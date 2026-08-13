using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 8f;
    public float acceleration = 50f;
    public float deceleration = 60f;

    [Header("Sprint Settings")]
    public float sprintSpeed = 14f;
    public float staminaDrainRate = 25f;
    public float staminaRegenRate = 12f;
    public float staminaRegenDelay = 1.5f;

    [Header("Ground Check")]
    [Tooltip("Extra distance below the collider used to detect floors.")]
    public float groundCheckDistance = 0.35f;
    [Tooltip("Slopes steeper than this are treated as walls.")]
    public float maxSlopeAngle = 50f;
    public LayerMask groundMask = ~0;

    [Header("Gravity")]
    [Tooltip("Downward acceleration while airborne.")]
    public float gravity = 30f;
    [Tooltip("Constant downward speed applied while grounded so bumps cannot launch the player.")]
    public float groundSnapSpeed = 6f;
    [Tooltip("Maximum fall speed.")]
    public float terminalVelocity = 55f;

    [Header("Head Bob")]
    [Tooltip("If empty, the camera is taken from PlayerLook / Camera.main.")]
    public Transform playerCamera;
    public float bobFrequency = 1.6f;
    public float bobAmount = 0.06f;
    public float smoothness = 12f;

    [Header("Head Bob States")]
    public float idleBobAmount = 0.012f;
    public float idleBobFrequency = 0.7f;
    public float walkBobMultiplier = 1f;
    public float sprintBobMultiplier = 1.75f;
    public float sprintFrequencyMultiplier = 1.45f;

    private Rigidbody rb;
    private Animator animator;
    private PlayerStats playerStats;
    private Collider bodyCollider;

    private Vector3 horizontalVelocity;
    private Vector3 inputDirection;
    private Vector3 groundNormal = Vector3.up;
    private float verticalVelocity;

    private bool isSprinting;
    private bool canSprint = true;
    private bool isGrounded;
    private float regenDelayTimer;

    private Vector3 cameraRestLocalPos;
    private Vector3 currentBobOffset;
    private float bobTimer;
    private bool cameraRestCaptured;

    public bool IsGrounded => isGrounded;
    public bool IsSprinting => isSprinting;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        playerStats = GetComponent<PlayerStats>();
        bodyCollider = GetComponent<Collider>();

        // Custom gravity is applied in MoveRelative so Unity gravity must stay off.
        // Leaving it on plus collision Y-impulse is what launched the player into the sky.
        rb.useGravity = false;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        ResolveCamera();
        CaptureCameraRest();
    }

    void Update()
    {
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsActive())
        {
            inputDirection = Vector3.zero;
            isSprinting = false;
            if (animator) animator.SetFloat("Speed", 0f);
            return;
        }

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        // Normalize so all 8 directions travel at equal speed
        inputDirection = new Vector3(h, 0f, v).normalized;

        HandleSprint();

        if (animator)
        {
            animator.SetFloat("Speed", inputDirection.magnitude);
            animator.SetBool("IsSprinting", isSprinting);
        }
    }

    void FixedUpdate()
    {
        CheckGrounded();
        MoveRelative();
    }

    void LateUpdate()
    {
        ApplyHeadBob();
    }

    void HandleSprint()
    {
        if (playerStats == null) return;

        bool wantsToSprint = Input.GetKey(KeyCode.LeftShift) && inputDirection.magnitude > 0;

        if (wantsToSprint && canSprint && playerStats.stamina > 0f)
        {
            isSprinting = true;
            regenDelayTimer = staminaRegenDelay;

            playerStats.stamina -= staminaDrainRate * Time.deltaTime;
            playerStats.stamina = Mathf.Max(playerStats.stamina, 0f);

            // Fully exhausted — stop sprint and lock it out until recovered
            if (playerStats.stamina <= 0f)
            {
                canSprint = false;
                isSprinting = false;
            }
        }
        else
        {
            isSprinting = false;

            if (regenDelayTimer > 0f)
            {
                regenDelayTimer -= Time.deltaTime;
            }
            else
            {
                playerStats.stamina += staminaRegenRate * Time.deltaTime;
                playerStats.stamina = Mathf.Min(playerStats.stamina, playerStats.maxStamina);

                // Only allow sprinting again once 25% stamina is recovered
                if (!canSprint && playerStats.stamina >= playerStats.maxStamina * 0.25f)
                    canSprint = true;
            }
        }
    }

    void CheckGrounded()
    {
        GetGroundCast(out Vector3 origin, out float radius, out float distance);

        if (Physics.SphereCast(
                origin,
                radius,
                Vector3.down,
                out RaycastHit hit,
                distance,
                groundMask,
                QueryTriggerInteraction.Ignore))
        {
            float slopeAngle = Vector3.Angle(hit.normal, Vector3.up);
            if (slopeAngle <= maxSlopeAngle)
            {
                isGrounded = true;
                groundNormal = hit.normal;
                return;
            }
        }

        isGrounded = false;
        groundNormal = Vector3.up;
    }

    void MoveRelative()
    {
        Vector3 wishDir = (transform.forward * inputDirection.z) + (transform.right * inputDirection.x);
        wishDir.y = 0f;
        if (wishDir.sqrMagnitude > 1f)
            wishDir.Normalize();

        // Slide along walls instead of digging in / riding their upward normals
        wishDir = SlideOffWalls(wishDir);

        float activeSpeed = isSprinting ? sprintSpeed : moveSpeed;
        Vector3 targetVelocity = wishDir * activeSpeed;

        float currentStep = inputDirection.sqrMagnitude > 0f ? acceleration : deceleration;

        Vector3 currentXZ = new Vector3(horizontalVelocity.x, 0f, horizontalVelocity.z);
        Vector3 targetXZ = new Vector3(targetVelocity.x, 0f, targetVelocity.z);

        currentXZ = Vector3.MoveTowards(currentXZ, targetXZ, currentStep * Time.fixedDeltaTime);
        horizontalVelocity = currentXZ;

        Vector3 finalVelocity;

        if (isGrounded)
        {
            // Walk along the slope instead of clipping into it
            Vector3 alongSlope = Vector3.ProjectOnPlane(currentXZ, groundNormal);

            // Snap downward along the ground normal so steps and debris cannot launch us
            alongSlope -= groundNormal * groundSnapSpeed;

            // Legitimate slope travel may have +Y. Obstacle bounce should not.
            float maxUp = Mathf.Max(0f, Vector3.ProjectOnPlane(targetXZ, groundNormal).y);
            if (alongSlope.y > maxUp)
                alongSlope.y = maxUp;

            verticalVelocity = alongSlope.y;
            finalVelocity = alongSlope;
        }
        else
        {
            verticalVelocity -= gravity * Time.fixedDeltaTime;
            verticalVelocity = Mathf.Max(verticalVelocity, -terminalVelocity);
            finalVelocity = new Vector3(currentXZ.x, verticalVelocity, currentXZ.z);
        }

        SetVelocity(finalVelocity);
    }

    /// <summary>
    /// If a wall is immediately ahead, project movement onto that wall's plane
    /// and strip any upward component so capsules cannot climb collision normals.
    /// </summary>
    Vector3 SlideOffWalls(Vector3 wishDir)
    {
        if (wishDir.sqrMagnitude < 0.0001f)
            return Vector3.zero;

        GetCapsuleCast(out Vector3 p1, out Vector3 p2, out float radius);

        if (!Physics.CapsuleCast(
                p1,
                p2,
                radius,
                wishDir.normalized,
                out RaycastHit hit,
                0.25f,
                groundMask,
                QueryTriggerInteraction.Ignore))
        {
            return wishDir;
        }

        float angle = Vector3.Angle(hit.normal, Vector3.up);
        if (angle <= maxSlopeAngle)
            return wishDir;

        Vector3 slid = Vector3.ProjectOnPlane(wishDir, hit.normal);
        slid.y = 0f;

        if (slid.sqrMagnitude < 0.0001f)
            return Vector3.zero;

        return slid.normalized * wishDir.magnitude;
    }

    void OnCollisionStay(Collision collision)
    {
        int contacts = collision.contactCount;
        for (int i = 0; i < contacts; i++)
        {
            Vector3 normal = collision.GetContact(i).normal;

            // Nearly-vertical faces are walls. Kill leftover upward speed from the solver.
            if (normal.y < 0.4f && verticalVelocity > 0f && !isGrounded)
                verticalVelocity = 0f;

            if (normal.y < 0.4f && isGrounded && GetVelocity().y > 0.1f)
            {
                Vector3 vel = GetVelocity();
                vel.y = Mathf.Min(vel.y, 0f);
                SetVelocity(vel);
            }
        }
    }

    void ApplyHeadBob()
    {
        ResolveCamera();
        if (playerCamera == null)
            return;

        if (!cameraRestCaptured)
            CaptureCameraRest();

        bool dialogueLock = DialogueManager.Instance != null && DialogueManager.Instance.IsActive();
        float planarSpeed = new Vector3(horizontalVelocity.x, 0f, horizontalVelocity.z).magnitude;
        bool walking = !dialogueLock && isGrounded && planarSpeed > 0.4f && inputDirection.sqrMagnitude > 0.01f;

        float freq;
        float amp;

        if (walking)
        {
            freq = bobFrequency * (isSprinting ? sprintFrequencyMultiplier : 1f);
            amp = bobAmount * (isSprinting ? sprintBobMultiplier : walkBobMultiplier);
        }
        else
        {
            freq = idleBobFrequency;
            amp = dialogueLock ? 0f : idleBobAmount;
        }

        bobTimer += Time.deltaTime * freq * Mathf.PI * 2f;

        Vector3 targetOffset;
        if (walking)
        {
            // Horizontal sway + double-frequency vertical step
            targetOffset = new Vector3(
                Mathf.Sin(bobTimer) * amp * 0.45f,
                Mathf.Abs(Mathf.Sin(bobTimer)) * amp,
                0f
            );
        }
        else
        {
            // Slow idle breathing
            targetOffset = new Vector3(0f, Mathf.Sin(bobTimer) * amp, 0f);
        }

        currentBobOffset = Vector3.Lerp(currentBobOffset, targetOffset, smoothness * Time.deltaTime);
        playerCamera.localPosition = cameraRestLocalPos + currentBobOffset;
    }

    void ResolveCamera()
    {
        if (playerCamera != null)
            return;

        PlayerLook look = GetComponent<PlayerLook>();
        if (look != null && look.playerCamera != null)
            playerCamera = look.playerCamera;
        else if (Camera.main != null)
            playerCamera = Camera.main.transform;
    }

    void CaptureCameraRest()
    {
        if (playerCamera == null)
            return;

        cameraRestLocalPos = playerCamera.localPosition;
        cameraRestCaptured = true;
    }

    void GetGroundCast(out Vector3 origin, out float radius, out float distance)
    {
        if (bodyCollider != null)
        {
            Bounds bounds = bodyCollider.bounds;
            radius = Mathf.Max(0.08f, Mathf.Min(bounds.extents.x, bounds.extents.z) * 0.85f);
            origin = new Vector3(bounds.center.x, bounds.min.y + radius + 0.05f, bounds.center.z);
            distance = groundCheckDistance + 0.1f;
            return;
        }

        radius = 0.25f;
        origin = transform.position + Vector3.up * (radius + 0.05f);
        distance = groundCheckDistance + 0.1f;
    }

    void GetCapsuleCast(out Vector3 p1, out Vector3 p2, out float radius)
    {
        CapsuleCollider capsule = bodyCollider as CapsuleCollider;
        if (capsule != null)
        {
            Vector3 scale = transform.lossyScale;
            radius = capsule.radius * Mathf.Max(scale.x, scale.z) * 0.9f;

            float height = Mathf.Max(capsule.height * scale.y, radius * 2f + 0.01f);
            Vector3 center = transform.TransformPoint(capsule.center);
            float half = height * 0.5f - radius;

            p1 = center + Vector3.up * half;
            p2 = center - Vector3.up * half;
            return;
        }

        if (bodyCollider != null)
        {
            Bounds bounds = bodyCollider.bounds;
            radius = Mathf.Min(bounds.extents.x, bounds.extents.z) * 0.85f;
            p1 = new Vector3(bounds.center.x, bounds.max.y - radius, bounds.center.z);
            p2 = new Vector3(bounds.center.x, bounds.min.y + radius, bounds.center.z);
            return;
        }

        radius = 0.3f;
        p1 = transform.position + Vector3.up * 1.5f;
        p2 = transform.position + Vector3.up * 0.3f;
    }

    Vector3 GetVelocity()
    {
#if UNITY_6000_0_OR_NEWER
        return rb.linearVelocity;
#else
        return rb.velocity;
#endif
    }

    void SetVelocity(Vector3 velocity)
    {
#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = velocity;
#else
        rb.velocity = velocity;
#endif
    }
}
