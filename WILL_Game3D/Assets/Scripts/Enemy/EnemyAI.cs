using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

public enum EnemyState
{
    Idle,
    Patrol,
    Investigate,
    Pursue,
    Search,
    Attack
}

/// <summary>
/// Horror NPC brain. Existing inspector refs (agent, player, waypoints, detectionRange, killDistance) still work.
/// Perception fills an alert meter; states only change when thresholds are crossed, which stops jitter.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    public enum VisionBand
    {
        None,
        Peripheral,
        Focus
    }

    [Header("References")]
    public NavMeshAgent agent;
    public Transform player;
    public Transform[] waypoints;

    [Header("Detection Settings")]
    [Tooltip("Fallback sight range used if Sight Radius is 0 (keeps old scenes working).")]
    public float detectionRange = 10f;
    public float killDistance = 1.5f;

    [Header("Perception")]
    public float sightRadius = 18f;
    [Tooltip("Wide cone. Slow, creeping suspicion.")]
    [Range(10f, 180f)] public float viewAngle = 120f;
    [Tooltip("Narrow center cone. Fast / near-instant detection.")]
    [Range(5f, 90f)] public float focusViewAngle = 32f;
    public float detectionSpeed = 0.55f;
    public float alertDecay = 0.22f;
    public float noiseSensitivity = 1f;
    public float hearRadius = 22f;
    public float pointBlankRange = 4.5f;
    public float eyeHeight = 1.6f;
    [Tooltip("If the player is this close, the enemy 'feels' them even outside the cone.")]
    public float proximitySenseRange = 2.8f;
    [Tooltip("Extra sight range while the player's flashlight is on.")]
    public float flashlightSightBonus = 7f;
    public LayerMask sightMask = ~0;

    [Header("Alert Thresholds")]
    [Range(0f, 1f)] public float investigateThreshold = 0.28f;
    [Range(0f, 1f)] public float pursueThreshold = 0.78f;

    [Header("Movement")]
    public float baseSpeed = 2.4f;
    public float chaseSpeed = 5.8f;
    public float investigateSpeed = 3.2f;
    public float searchSpeed = 3.6f;
    public float speedSmooth = 4f;
    [Tooltip("Extra burst while staring directly at the player.")]
    public float eyeContactSpeedBonus = 0.45f;
    [Tooltip("Seconds of predicted player travel used while chasing.")]
    public float interceptLookahead = 1.2f;

    [Header("Patrol")]
    public Vector2 patrolPauseRange = new Vector2(0.8f, 3.2f);
    [Range(0f, 1f)] public float glanceChance = 0.35f;
    public float glanceDuration = 0.7f;

    [Header("Search / Hunt")]
    public float searchRadius = 7f;
    public int searchSamples = 5;
    public float searchHoldTime = 0.9f;
    public float loseSightMemory = 0.65f;
    [Tooltip("After a sighting, keep hunting this long even without vision.")]
    public float huntDuration = 9f;
    public float destinationRefresh = 0.16f;

    [Header("Spotted Freeze")]
    public float spottedFreezeTime = 0.55f;
    public UnityEvent onSpottedSting;

    [Header("Animation")]
    public float animatorDampTime = 0.12f;

    [Header("Debug")]
    public bool debugLogs = false;
    public bool drawGizmos = true;

    public EnemyState CurrentState { get; private set; } = EnemyState.Idle;
    public float AlertLevel { get; private set; }

    Animator animator;
    Flashlight playerFlashlight;
    int waypointIndex;
    int waypointDirection = 1;
    int searchIndex;
    float stateTimer;
    float glanceTimer;
    float loseSightTimer;
    float freezeTimer;
    float desiredSpeed;
    float twitchTimer;
    float huntTimer;
    float destTimer;
    float stuckTimer;
    float lastRemaining = 999f;
    bool freezePendingPursue;
    bool hasLastKnown;
    bool killedPlayer;
    Vector3 lastKnownPosition;
    Vector3 lastKnownVelocity;
    Vector3 investigatePoint;
    Vector3 lastPlayerPos;
    Vector3 playerVelocity;
    Vector3[] searchPoints = new Vector3[8];
    VisionBand currentVision;

    static readonly int SpeedHash = Animator.StringToHash("Speed");
    static readonly int IsChasingHash = Animator.StringToHash("IsChasing");

    void Awake()
    {
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
    }

    void OnEnable()
    {
        NoiseEvent.OnEmitted += OnNoiseHeard;
    }

    void OnDisable()
    {
        NoiseEvent.OnEmitted -= OnNoiseHeard;
    }

    void Start()
    {
        if (agent != null)
        {
            agent.speed = baseSpeed;
            agent.acceleration = 14f;
            agent.angularSpeed = 240f;
            agent.stoppingDistance = 0.35f;
            agent.autoBraking = true;
            agent.updateRotation = true;
        }

        ResolvePlayer();
        if (GetComponent<SpottedFeedback>() == null)
            gameObject.AddComponent<SpottedFeedback>();
        EnterState(waypoints != null && waypoints.Length > 0 ? EnemyState.Patrol : EnemyState.Idle);
    }

    void Update()
    {
        if (agent == null || !agent.isOnNavMesh)
            return;

        if (killedPlayer || PlayerDeath.IsDead)
        {
            if (agent.isOnNavMesh)
                agent.isStopped = true;
            FacePoint(player != null ? player.position : lastKnownPosition);
            UpdateAnimator();
            return;
        }

        ResolvePlayer();
        SamplePlayerVelocity();
        UpdatePerception();
        UpdateState();
        RecoverIfStuck();
        ApplyMovementFeel();
        UpdateAnimator();
        TryCatchPlayer();
    }

    #region Perception

    void SamplePlayerVelocity()
    {
        if (player == null)
        {
            playerVelocity = Vector3.zero;
            return;
        }

        if (lastPlayerPos.sqrMagnitude > 0.01f)
        {
            Vector3 raw = (player.position - lastPlayerPos) / Mathf.Max(Time.deltaTime, 0.0001f);
            raw.y = 0f;
            playerVelocity = Vector3.Lerp(playerVelocity, raw, 8f * Time.deltaTime);
        }

        lastPlayerPos = player.position;
    }

    void UpdatePerception()
    {
        currentVision = EvaluateVision(out Vector3 seenPoint);
        bool flashlightOn = playerFlashlight != null && playerFlashlight.IsOn;

        if (currentVision != VisionBand.None)
        {
            loseSightTimer = 0f;
            huntTimer = huntDuration;
            RememberPlayer(seenPoint);

            float gain = detectionSpeed * Time.deltaTime;
            if (currentVision == VisionBand.Focus)
                gain *= 3.4f;
            else
                gain *= 0.55f;

            if (flashlightOn)
                gain *= 1.65f;

            float dist = Vector3.Distance(EyePosition(), player.position);
            float range = CurrentSightRange;
            gain *= Mathf.Clamp01(1.2f - dist / Mathf.Max(range, 0.01f));
            AlertLevel = Mathf.Clamp01(AlertLevel + gain);

            if (currentVision == VisionBand.Focus && dist <= pointBlankRange)
                AlertLevel = 1f;
        }
        else
        {
            loseSightTimer += Time.deltaTime;
            huntTimer = Mathf.Max(0f, huntTimer - Time.deltaTime);

            bool hunting = huntTimer > 0f || CurrentState == EnemyState.Pursue || CurrentState == EnemyState.Attack;
            if (!hunting)
                AlertLevel = Mathf.MoveTowards(AlertLevel, 0f, alertDecay * Time.deltaTime);
        }
    }

    VisionBand EvaluateVision(out Vector3 seenPoint)
    {
        seenPoint = hasLastKnown ? lastKnownPosition : transform.position;
        if (player == null)
            return VisionBand.None;

        if (PlayerStealth.Instance != null && PlayerStealth.Instance.IsHidden)
            return VisionBand.None;

        Vector3 eye = EyePosition();
        Vector3 target = player.position + Vector3.up * 1.1f;
        Vector3 toPlayer = target - eye;
        float distance = toPlayer.magnitude;

        // Close enough to feel breath on their neck — no cone required, LOS still required.
        if (distance <= proximitySenseRange && HasLineOfSight(eye, target, distance))
        {
            seenPoint = player.position;
            return VisionBand.Focus;
        }

        if (distance > CurrentSightRange)
            return VisionBand.None;

        float angle = Vector3.Angle(transform.forward, toPlayer);
        if (angle > viewAngle * 0.5f)
            return VisionBand.None;

        if (!HasLineOfSight(eye, target, distance))
            return VisionBand.None;

        seenPoint = player.position;
        return angle <= focusViewAngle * 0.5f ? VisionBand.Focus : VisionBand.Peripheral;
    }

    bool HasLineOfSight(Vector3 from, Vector3 to, float distance)
    {
        Vector3 dir = to - from;
        if (dir.sqrMagnitude < 0.0001f)
            return true;

        if (Physics.Raycast(from, dir.normalized, out RaycastHit hit, distance, sightMask, QueryTriggerInteraction.Ignore))
        {
            Transform root = hit.transform;
            if (player != null && (root == player || root.IsChildOf(player) || hit.transform.CompareTag("Player")))
                return true;
            return false;
        }

        return true;
    }

    void OnNoiseHeard(Vector3 worldPosition, float loudness)
    {
        float dist = Vector3.Distance(transform.position, worldPosition);
        if (dist > hearRadius)
            return;

        float falloff = 1f - Mathf.Clamp01(dist / hearRadius);
        float spike = loudness * noiseSensitivity * falloff * 0.55f;
        AlertLevel = Mathf.Clamp01(AlertLevel + spike);

        investigatePoint = worldPosition;
        lastKnownPosition = worldPosition;
        hasLastKnown = true;

        if (debugLogs)
            Debug.Log("[EnemyAI] Heard noise. Alert=" + AlertLevel.ToString("0.00"), this);

        if (CurrentState == EnemyState.Pursue || CurrentState == EnemyState.Attack)
        {
            SetDestinationThrottled(worldPosition, true);
            huntTimer = Mathf.Max(huntTimer, huntDuration * 0.45f);
            return;
        }

        if (AlertLevel >= pursueThreshold)
            BeginSpottedThenPursue();
        else if (AlertLevel >= investigateThreshold)
            EnterState(EnemyState.Investigate);
    }

    void RememberPlayer(Vector3 position)
    {
        lastKnownPosition = position;
        lastKnownVelocity = playerVelocity;
        hasLastKnown = true;
    }

    #endregion

    #region State machine

    void UpdateState()
    {
        switch (CurrentState)
        {
            case EnemyState.Idle:
                TickIdle();
                break;
            case EnemyState.Patrol:
                TickPatrol();
                break;
            case EnemyState.Investigate:
                TickInvestigate();
                break;
            case EnemyState.Pursue:
                TickPursue();
                break;
            case EnemyState.Search:
                TickSearch();
                break;
            case EnemyState.Attack:
                TickAttack();
                break;
        }
    }

    void EnterState(EnemyState next)
    {
        if (CurrentState == next && next != EnemyState.Patrol && next != EnemyState.Search)
            return;

        CurrentState = next;
        stateTimer = 0f;
        glanceTimer = 0f;
        stuckTimer = 0f;
        destTimer = 0f;
        freezePendingPursue = false;

        if (agent != null && agent.isOnNavMesh && next != EnemyState.Pursue)
            agent.isStopped = false;

        switch (next)
        {
            case EnemyState.Idle:
                desiredSpeed = baseSpeed * 0.35f;
                if (agent != null && agent.isOnNavMesh)
                    agent.ResetPath();
                stateTimer = Random.Range(1.2f, 3.5f);
                break;

            case EnemyState.Patrol:
                desiredSpeed = baseSpeed;
                GoToCurrentWaypoint();
                break;

            case EnemyState.Investigate:
                desiredSpeed = investigateSpeed;
                SetDestination(investigatePoint);
                break;

            case EnemyState.Pursue:
                desiredSpeed = chaseSpeed;
                huntTimer = huntDuration;
                break;

            case EnemyState.Search:
                desiredSpeed = searchSpeed;
                BuildSearchPoints();
                searchIndex = 0;
                SetDestination(searchPoints[0]);
                break;

            case EnemyState.Attack:
                desiredSpeed = chaseSpeed * 0.4f;
                if (agent != null)
                    agent.isStopped = true;
                KillPlayer();
                break;
        }

        if (debugLogs)
            Debug.Log("[EnemyAI] -> " + next, this);
    }

    void TickIdle()
    {
        TryEscalateFromAlert();
        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0f)
            EnterState(waypoints != null && waypoints.Length > 0 ? EnemyState.Patrol : EnemyState.Idle);
    }

    void TickPatrol()
    {
        TryEscalateFromAlert();

        if (waypoints == null || waypoints.Length == 0)
        {
            EnterState(EnemyState.Idle);
            return;
        }

        if (ReachedDestination())
        {
            if (agent != null)
                agent.isStopped = true;

            if (stateTimer <= 0f)
            {
                stateTimer = Random.Range(patrolPauseRange.x, patrolPauseRange.y);
                if (Random.value < glanceChance)
                    glanceTimer = glanceDuration;
            }

            stateTimer -= Time.deltaTime;

            if (glanceTimer > 0f)
            {
                glanceTimer -= Time.deltaTime;
                transform.Rotate(0f, 110f * Time.deltaTime, 0f);
            }

            if (stateTimer <= 0f)
            {
                if (agent != null)
                    agent.isStopped = false;
                AdvanceWaypoint();
            }
        }
    }

    void TickInvestigate()
    {
        if (AlertLevel >= pursueThreshold && currentVision != VisionBand.None)
        {
            BeginSpottedThenPursue();
            return;
        }

        if (currentVision != VisionBand.None && player != null)
        {
            RememberPlayer(player.position);
            SetDestinationThrottled(GetInterceptPoint(), false);
        }

        if (ReachedDestination())
        {
            stateTimer += Time.deltaTime;
            ScanInPlace();
            if (stateTimer > 2.2f && currentVision == VisionBand.None)
                EnterState(EnemyState.Search);
        }
    }

    void TickPursue()
    {
        if (freezePendingPursue)
        {
            freezeTimer -= Time.deltaTime;
            if (agent != null)
                agent.isStopped = true;
            FacePoint(player != null ? player.position : lastKnownPosition);

            if (freezeTimer <= 0f)
            {
                freezePendingPursue = false;
                if (agent != null)
                    agent.isStopped = false;
            }
            return;
        }

        if (player != null && currentVision != VisionBand.None)
        {
            RememberPlayer(player.position);
            loseSightTimer = 0f;
            huntTimer = huntDuration;
            SetDestinationThrottled(GetInterceptPoint(), false);

            bool staring = currentVision == VisionBand.Focus;
            twitchTimer -= Time.deltaTime;
            if (staring && twitchTimer <= 0f)
            {
                twitchTimer = Random.Range(0.16f, 0.5f);
                desiredSpeed = chaseSpeed * (1f + Random.Range(0.1f, eyeContactSpeedBonus));
            }
            else if (!staring)
            {
                desiredSpeed = chaseSpeed;
            }
        }
        else if (huntTimer > 0f || loseSightTimer < loseSightMemory)
        {
            Vector3 huntPoint = lastKnownPosition + lastKnownVelocity * interceptLookahead;
            SetDestinationThrottled(huntPoint, false);
            desiredSpeed = chaseSpeed * 0.92f;
        }
        else
        {
            EnterState(EnemyState.Search);
        }

        if (player != null && Vector3.Distance(transform.position, player.position) <= killDistance)
            EnterState(EnemyState.Attack);
    }

    void TickSearch()
    {
        if (currentVision != VisionBand.None && AlertLevel >= investigateThreshold)
        {
            if (AlertLevel >= pursueThreshold)
                BeginSpottedThenPursue();
            else
            {
                investigatePoint = player.position;
                EnterState(EnemyState.Investigate);
            }
            return;
        }

        if (ReachedDestination())
        {
            stateTimer += Time.deltaTime;
            ScanInPlace();

            if (stateTimer >= searchHoldTime)
            {
                searchIndex++;
                stateTimer = 0f;
                if (searchIndex >= searchSamples)
                {
                    AlertLevel = Mathf.MoveTowards(AlertLevel, 0f, 1f);
                    huntTimer = 0f;
                    EnterState(EnemyState.Patrol);
                }
                else
                {
                    SetDestination(searchPoints[searchIndex]);
                }
            }
        }
    }

    void TickAttack()
    {
        FacePoint(player != null ? player.position : lastKnownPosition);
        if (player != null && Vector3.Distance(transform.position, player.position) > killDistance * 1.6f)
            EnterState(EnemyState.Pursue);
    }

    void TryEscalateFromAlert()
    {
        if (CurrentState == EnemyState.Pursue || CurrentState == EnemyState.Attack)
            return;

        if (currentVision == VisionBand.Focus && AlertLevel >= pursueThreshold)
        {
            BeginSpottedThenPursue();
            return;
        }

        if (AlertLevel >= pursueThreshold && currentVision != VisionBand.None)
        {
            BeginSpottedThenPursue();
            return;
        }

        if (AlertLevel >= investigateThreshold && hasLastKnown && CurrentState != EnemyState.Investigate)
        {
            investigatePoint = lastKnownPosition;
            EnterState(EnemyState.Investigate);
        }
    }

    void BeginSpottedThenPursue()
    {
        if (CurrentState == EnemyState.Pursue && !freezePendingPursue)
            return;

        EnterState(EnemyState.Pursue);
        freezePendingPursue = true;
        freezeTimer = spottedFreezeTime;
        huntTimer = huntDuration;
        if (agent != null)
            agent.isStopped = true;

        onSpottedSting?.Invoke();
        if (SpottedFeedback.Instance != null)
            SpottedFeedback.Instance.Play();
        else
        {
            SpottedFeedback feedback = FindObjectOfType<SpottedFeedback>();
            if (feedback != null)
                feedback.Play();
        }
        if (EventDebugManager.Instance != null)
            EventDebugManager.Instance.TriggerEvent("Enemy spotted the player");
    }

    #endregion

    #region Navigation helpers

    Vector3 GetInterceptPoint()
    {
        if (player == null)
            return lastKnownPosition;

        Vector3 predicted = player.position + playerVelocity * interceptLookahead;
        predicted.y = player.position.y;
        return predicted;
    }

    void SetDestinationThrottled(Vector3 world, bool force)
    {
        destTimer -= Time.deltaTime;
        if (!force && destTimer > 0f)
            return;

        destTimer = destinationRefresh;
        SetDestination(world);
    }

    void RecoverIfStuck()
    {
        if (agent == null || !agent.isOnNavMesh || agent.isStopped || agent.pathPending)
        {
            stuckTimer = 0f;
            return;
        }

        float remaining = agent.remainingDistance;
        float speed = agent.velocity.magnitude;
        bool notMoving = speed < 0.12f && remaining > 1.1f && Mathf.Abs(remaining - lastRemaining) < 0.05f;

        stuckTimer = notMoving ? stuckTimer + Time.deltaTime : 0f;
        lastRemaining = remaining;

        if (stuckTimer > 1.1f)
        {
            stuckTimer = 0f;
            agent.ResetPath();
            Vector3 fallback = hasLastKnown ? lastKnownPosition : transform.position + transform.forward * 2f;
            SetDestination(fallback);
        }
    }

    void AdvanceWaypoint()
    {
        if (waypoints.Length == 1)
        {
            waypointIndex = 0;
        }
        else if (Random.value < 0.25f)
        {
            waypointDirection *= -1;
            waypointIndex = Mathf.Clamp(waypointIndex + waypointDirection, 0, waypoints.Length - 1);
        }
        else
        {
            waypointIndex += waypointDirection;
            if (waypointIndex >= waypoints.Length)
            {
                waypointIndex = waypoints.Length - 1;
                waypointDirection = -1;
            }
            else if (waypointIndex < 0)
            {
                waypointIndex = 0;
                waypointDirection = 1;
            }
        }

        GoToCurrentWaypoint();
    }

    void GoToCurrentWaypoint()
    {
        if (waypoints == null || waypoints.Length == 0)
            return;
        waypointIndex = Mathf.Clamp(waypointIndex, 0, waypoints.Length - 1);
        if (waypoints[waypointIndex] != null)
            SetDestination(waypoints[waypointIndex].position);
    }

    void BuildSearchPoints()
    {
        Vector3 origin = hasLastKnown ? lastKnownPosition : transform.position;
        Vector3 escapeDir = lastKnownVelocity.sqrMagnitude > 0.4f ? lastKnownVelocity.normalized : transform.forward;
        searchSamples = Mathf.Clamp(searchSamples, 2, searchPoints.Length);

        searchPoints[0] = origin;
        Vector3 alongEscape = origin + escapeDir * (searchRadius * 0.85f);
        searchPoints[1] = SampleNav(alongEscape, origin);

        for (int i = 2; i < searchSamples; i++)
        {
            Vector2 circle = Random.insideUnitCircle * searchRadius;
            Vector3 candidate = origin + new Vector3(circle.x, 0f, circle.y);
            searchPoints[i] = SampleNav(candidate, origin);
        }
    }

    Vector3 SampleNav(Vector3 candidate, Vector3 fallback)
    {
        if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, searchRadius, NavMesh.AllAreas))
            return hit.position;
        return fallback;
    }

    void SetDestination(Vector3 world)
    {
        if (agent == null || !agent.isOnNavMesh)
            return;
        if (NavMesh.SamplePosition(world, out NavMeshHit hit, 3f, NavMesh.AllAreas))
            agent.SetDestination(hit.position);
        else
            agent.SetDestination(world);
    }

    bool ReachedDestination()
    {
        if (agent == null || !agent.isOnNavMesh)
            return false;
        if (agent.pathPending)
            return false;
        return agent.remainingDistance <= Mathf.Max(0.45f, agent.stoppingDistance);
    }

    void ScanInPlace()
    {
        transform.Rotate(0f, Mathf.Sin(Time.time * 2.4f) * 90f * Time.deltaTime, 0f);
    }

    void FacePoint(Vector3 world)
    {
        Vector3 flat = world - transform.position;
        flat.y = 0f;
        if (flat.sqrMagnitude < 0.001f)
            return;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(flat), 8f * Time.deltaTime);
    }

    void ApplyMovementFeel()
    {
        if (agent == null)
            return;
        agent.speed = Mathf.MoveTowards(agent.speed, desiredSpeed, speedSmooth * Time.deltaTime);
    }

    void UpdateAnimator()
    {
        if (animator == null)
            return;

        float planar = agent != null ? new Vector3(agent.velocity.x, 0f, agent.velocity.z).magnitude : 0f;
        animator.SetFloat(SpeedHash, planar, animatorDampTime, Time.deltaTime);

        if (HasAnimatorBool(IsChasingHash))
            animator.SetBool(IsChasingHash, CurrentState == EnemyState.Pursue || CurrentState == EnemyState.Attack);
    }

    bool HasAnimatorBool(int hash)
    {
        if (animator.parameters == null)
            return false;
        for (int i = 0; i < animator.parameterCount; i++)
        {
            if (animator.parameters[i].nameHash == hash)
                return true;
        }
        return false;
    }

    #endregion

    #region Catch / setup

    void TryCatchPlayer()
    {
        if (killedPlayer || player == null)
            return;
        if (PlayerStealth.Instance != null && PlayerStealth.Instance.IsHidden)
            return;
        if (Vector3.Distance(transform.position, player.position) > killDistance)
            return;
        if (CurrentState != EnemyState.Pursue && CurrentState != EnemyState.Attack)
            return;
        EnterState(EnemyState.Attack);
    }

    void KillPlayer()
    {
        if (killedPlayer)
            return;
        killedPlayer = true;

        Debug.Log("Player is dead!");
        if (EventDebugManager.Instance != null)
            EventDebugManager.Instance.TriggerEvent("Enemy caught the player");

        PlayerDeath death = player != null ? player.GetComponent<PlayerDeath>() : null;
        if (death == null && player != null)
            death = player.gameObject.AddComponent<PlayerDeath>();
        if (death == null)
            death = FindObjectOfType<PlayerDeath>();
        if (death != null)
            death.Kill(transform);
    }

    void ResolvePlayer()
    {
        GameObject found = GameObject.FindGameObjectWithTag("Player");
        if (found == null)
            return;

        if (player != found.transform)
        {
            player = found.transform;
            playerFlashlight = found.GetComponent<Flashlight>() ?? found.GetComponentInChildren<Flashlight>();
        }
        else if (playerFlashlight == null)
        {
            playerFlashlight = found.GetComponent<Flashlight>() ?? found.GetComponentInChildren<Flashlight>();
        }
    }

    float CurrentSightRange
    {
        get
        {
            float range = sightRadius > 0.01f ? sightRadius : detectionRange;
            if (playerFlashlight != null && playerFlashlight.IsOn)
                range += flashlightSightBonus;
            return range;
        }
    }

    Vector3 EyePosition()
    {
        return transform.position + Vector3.up * eyeHeight;
    }

    #endregion

    void OnDrawGizmosSelected()
    {
        if (!drawGizmos)
            return;

        Vector3 eye = Application.isPlaying ? EyePosition() : transform.position + Vector3.up * eyeHeight;
        float range = sightRadius > 0.01f ? sightRadius : detectionRange;

        DrawCone(eye, viewAngle, range, new Color(1f, 0.85f, 0.2f, 0.25f));
        DrawCone(eye, focusViewAngle, range, new Color(1f, 0.15f, 0.1f, 0.35f));

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, hearRadius);
        Gizmos.color = new Color(1f, 0.4f, 0.9f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, proximitySenseRange);

        if (hasLastKnown)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(lastKnownPosition, 0.25f);
        }
    }

    void DrawCone(Vector3 origin, float angle, float range, Color color)
    {
        Gizmos.color = color;
        Quaternion left = Quaternion.Euler(0f, -angle * 0.5f, 0f);
        Quaternion right = Quaternion.Euler(0f, angle * 0.5f, 0f);
        Vector3 fwd = transform.forward * range;
        Gizmos.DrawRay(origin, left * fwd);
        Gizmos.DrawRay(origin, right * fwd);
        Gizmos.DrawRay(origin, fwd);
    }
}
