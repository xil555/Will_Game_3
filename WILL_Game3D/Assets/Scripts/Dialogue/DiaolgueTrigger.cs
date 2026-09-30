using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Environmental spots (roadblock, grave): dialogue starts on enter.
/// NPCs: show Press E prompt, start dialogue only when E is pressed.
/// </summary>
[RequireComponent(typeof(Collider))]
public class DialogueTrigger : MonoBehaviour
{
    [Header("Dialogue")]
    [Tooltip("Must match an id in StreamingAssets/Dialogue.json")]
    public string dialogueId;

    [Header("Objectives")]
    [Tooltip("If this is an NPC talk objective, match ObjectiveManager Trigger Id (e.g. EasyStartNPC).")]
    public string npcId;

    [Header("Behaviour")]
    public bool playOnce = true;
    public string playerTag = "Player";

    public enum StartMode
    {
        PressE,
        AutoOnEnter
    }

    [Header("How it starts")]
    [Tooltip("NPCs = Press E. Roadblock / grave = Auto On Enter.")]
    public StartMode startMode = StartMode.PressE;
    public string talkPrompt = "Press E to talk";
    public KeyCode talkKey = KeyCode.E;

    [Header("Follow-up talk (optional)")]
    [Tooltip("Second conversation on the same NPC, e.g. granny after the grave.")]
    public string followUpDialogueId;
    public string followUpNpcId;
    public string followUpTalkPrompt = "Press E to talk";

    [Header("After this talk ends")]
    [Tooltip("These objects turn on only after the conversation closes. Use this for the men in the woman's house.")]
    public GameObject[] enableAfterDialogue;

    public bool ShouldDelayReach
    {
        get { return !string.IsNullOrEmpty(dialogueId) && !hasPlayed; }
    }

    public bool HasFinishedDialogue
    {
        get
        {
            if (!hasPlayed || isStarting)
                return false;

            return DialogueManager.Instance == null || !DialogueManager.Instance.IsActive();
        }
    }

    bool UsesPressE
    {
        get { return startMode == StartMode.PressE || !string.IsNullOrEmpty(npcId); }
    }

    [Header("Optional - face roadblock / object")]
    [Tooltip("Drag the roadblock here. Player turns to face it when dialogue starts.")]
    public Transform lookAtTarget;
    public bool rotatePlayerToTarget = true;
    [Tooltip("Wait for the slow turn to finish before dialogue text appears.")]
    public bool waitForTurnBeforeDialogue = true;

    [Header("Marker")]
    public bool showMarker = true;
    [Tooltip("How high above the collider the arrow sits.")]
    public float markerHeight = 2.15f;
    public float markerVisibleRange = 22f;
    public Color markerColor = new Color(0.85f, 0.15f, 0.15f, 0.95f);
    [Tooltip("Hide the arrow after this dialogue has played.")]
    public bool hideMarkerAfterPlayed = true;

    bool hasPlayed;
    bool followUpPlayed;
    bool startingFollowUp;
    bool isStarting;
    bool playerInside;
    Transform playerTransform;

    Transform markerRoot;
    TextMeshPro arrowText;

    void Awake()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
            col.isTrigger = true;

        if (showMarker)
            CreateMarker();
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag))
            return;

        playerInside = true;
        playerTransform = other.transform;

        if (!IsCurrentStep())
            return;

        if (playOnce && hasPlayed && !FollowUpAvailable())
            return;

        if (UsesPressE)
        {
            if (DialogueManager.Instance == null || !DialogueManager.Instance.IsActive())
                InteractPromptUI.Show(this, GetTalkPrompt());
            return;
        }

        TryStartDialogue(other);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag))
            return;

        playerInside = false;
        InteractPromptUI.Hide(this);
    }

    void Update()
    {
        if (!IsCurrentStep())
        {
            InteractPromptUI.Hide(this);
            return;
        }

        if (!UsesPressE)
            return;

        if (!playerInside || isStarting)
            return;

        if (playOnce && hasPlayed && !FollowUpAvailable())
        {
            InteractPromptUI.Hide(this);
            return;
        }

        if (PauseMenu.IsPaused || PlayerDeath.IsDead)
            return;

        if (DialogueManager.Instance != null && DialogueManager.Instance.IsActive())
        {
            InteractPromptUI.Hide(this);
            return;
        }

        InteractPromptUI.Show(this, GetTalkPrompt());

        if (!Input.GetKeyDown(talkKey))
            return;

        if (playerTransform == null)
            return;

        TryStartDialogue(playerTransform);
    }

    void OnDestroy()
    {
        InteractPromptUI.Hide(this);
    }

    void TryStartDialogue(Component source)
    {
        if (playOnce && hasPlayed && !FollowUpAvailable())
            return;

        if (isStarting)
            return;

        if (!IsCurrentStep())
            return;

        string idToPlay = FollowUpAvailable() ? followUpDialogueId : dialogueId;
        if (string.IsNullOrEmpty(idToPlay))
            return;

        if (DialogueManager.Instance == null)
        {
            Debug.LogWarning("DialogueTrigger: No DialogueManager in scene.");
            return;
        }

        if (DialogueManager.Instance.IsActive())
            return;

        Transform sourceTransform = source.transform;
        PlayerDialogueLook dialogueLook = sourceTransform.GetComponent<PlayerDialogueLook>();
        if (dialogueLook == null)
            dialogueLook = sourceTransform.GetComponentInParent<PlayerDialogueLook>();

        startingFollowUp = FollowUpAvailable();
        isStarting = true;
        InteractPromptUI.Hide(this);

        if (rotatePlayerToTarget && lookAtTarget != null && dialogueLook != null)
        {
            if (waitForTurnBeforeDialogue)
            {
                dialogueLook.FaceTargetSmooth(lookAtTarget, () =>
                {
                    isStarting = false;
                    StartDialogueNow();
                });
            }
            else
            {
                dialogueLook.FaceTargetSmooth(lookAtTarget, null);
                isStarting = false;
                StartDialogueNow();
            }

            return;
        }

        if (rotatePlayerToTarget && lookAtTarget != null)
        {
            StartCoroutine(InstantTurnThenDialogue(sourceTransform));
            return;
        }

        isStarting = false;
        StartDialogueNow();
    }

    void StartDialogueNow()
    {
        InteractPromptUI.Hide(this);

        string idToPlay = startingFollowUp ? followUpDialogueId : dialogueId;
        string reportId = startingFollowUp ? followUpNpcId : npcId;

        if (startingFollowUp)
            followUpPlayed = true;
        else
            hasPlayed = true;

        DialogueManager.Instance.StartDialogue(idToPlay);

        if (!DialogueManager.Instance.IsActive())
        {
            hasPlayed = false;
            followUpPlayed = false;
            Debug.LogWarning("DialogueTrigger: '" + idToPlay + "' did not start. Check the Dialogue Id matches Dialogue.json.");
            return;
        }

        if (ObjectiveManager.Instance != null && !string.IsNullOrEmpty(reportId))
            ObjectiveManager.Instance.ReportNpcTalked(reportId);

        if (enableAfterDialogue != null && enableAfterDialogue.Length > 0)
            StartCoroutine(EnableAfterDialogueEnds());
    }

    IEnumerator EnableAfterDialogueEnds()
    {
        yield return null;

        float waitForStart = 1f;
        while (waitForStart > 0f && (DialogueManager.Instance == null || !DialogueManager.Instance.IsActive()))
        {
            waitForStart -= Time.deltaTime;
            yield return null;
        }

        if (DialogueManager.Instance == null || !DialogueManager.Instance.IsActive())
            yield break;

        while (DialogueManager.Instance.IsActive())
            yield return null;

        for (int i = 0; i < enableAfterDialogue.Length; i++)
        {
            if (enableAfterDialogue[i] != null)
                enableAfterDialogue[i].SetActive(true);
        }
    }

    bool FollowUpAvailable()
    {
        if (string.IsNullOrEmpty(followUpDialogueId) || !hasPlayed || followUpPlayed)
            return false;

        if (ObjectiveManager.Instance == null || !ObjectiveManager.Instance.HasActiveObjective)
            return true;

        if (ObjectiveManager.Instance.CurrentObjective.type != ObjectiveType.TalkToNpc)
            return false;

        if (string.IsNullOrEmpty(followUpNpcId))
            return true;

        return string.Equals(
            ObjectiveManager.Instance.CurrentObjective.triggerId,
            followUpNpcId,
            System.StringComparison.OrdinalIgnoreCase);
    }

    bool IsCurrentStep()
    {
        if (ObjectiveManager.Instance == null || !ObjectiveManager.Instance.HasActiveObjective)
            return true;

        string wanted = ObjectiveManager.Instance.CurrentObjective.triggerId;
        if (string.IsNullOrEmpty(wanted))
            return true;

        if (FollowUpAvailable()
            && string.Equals(followUpNpcId, wanted, System.StringComparison.OrdinalIgnoreCase))
            return true;

        if (!string.IsNullOrEmpty(npcId)
            && string.Equals(npcId, wanted, System.StringComparison.OrdinalIgnoreCase))
            return true;

        ObjectiveReach reach = GetComponent<ObjectiveReach>();
        if (reach != null
            && !string.IsNullOrEmpty(reach.triggerId)
            && string.Equals(reach.triggerId, wanted, System.StringComparison.OrdinalIgnoreCase))
            return true;

        bool tiedToObjective = !string.IsNullOrEmpty(npcId)
            || !string.IsNullOrEmpty(followUpNpcId)
            || reach != null;

        return !tiedToObjective;
    }

    string GetTalkPrompt()
    {
        if (FollowUpAvailable() && !string.IsNullOrEmpty(followUpTalkPrompt))
            return followUpTalkPrompt;

        return talkPrompt;
    }

    IEnumerator InstantTurnThenDialogue(Transform lookPlayer)
    {
        Vector3 direction = lookAtTarget.position - lookPlayer.position;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.001f)
            lookPlayer.rotation = Quaternion.LookRotation(direction.normalized);

        yield return null;

        isStarting = false;
        StartDialogueNow();
    }

    void LateUpdate()
    {
        UpdateMarker();
    }

    void CreateMarker()
    {
        GameObject rootGo = new GameObject("DialogueMarker");
        rootGo.transform.SetParent(transform, false);
        markerRoot = rootGo.transform;
        PlaceMarker();

        arrowText = CreateWorldText(rootGo.transform, "Arrow", "▼", 8f, Vector3.zero);
        arrowText.color = markerColor;
    }

    void PlaceMarker()
    {
        if (markerRoot == null)
            return;

        markerRoot.position = transform.position + Vector3.up * markerHeight;
    }

    static TextMeshPro CreateWorldText(Transform parent, string name, string text, float fontSize, Vector3 localPos)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;

        TextMeshPro tmp = go.AddComponent<TextMeshPro>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.raycastTarget = false;
        tmp.sortingOrder = 20;
        tmp.outlineWidth = 0.25f;
        tmp.outlineColor = new Color(0f, 0f, 0f, 0.85f);
        RectTransform rt = tmp.rectTransform;
        rt.sizeDelta = new Vector2(6f, 1.4f);
        return tmp;
    }

    void UpdateMarker()
    {
        if (markerRoot == null)
            return;

        PlaceMarker();

        Camera cam = Camera.main;
        float dist = float.MaxValue;
        if (cam != null)
        {
            dist = Vector3.Distance(cam.transform.position, markerRoot.position);
            markerRoot.rotation = Quaternion.LookRotation(markerRoot.position - cam.transform.position);
        }

        bool dialogueOpen = DialogueManager.Instance != null && DialogueManager.Instance.IsActive();
        bool hideBecausePlayed = hideMarkerAfterPlayed && hasPlayed && !FollowUpAvailable() && !isStarting && !dialogueOpen;
        bool inViewRange = dist <= markerVisibleRange;
        bool isThisStep = IsCurrentStep();

        if (arrowText != null)
        {
            bool showArrow = showMarker && isThisStep && inViewRange && !dialogueOpen && !isStarting && !hideBecausePlayed;
            arrowText.enabled = showArrow;
            if (showArrow)
            {
                arrowText.color = markerColor;
                float bob = Mathf.Sin(Time.time * 3.2f) * 0.12f;
                arrowText.transform.localPosition = new Vector3(0f, 0.15f + bob, 0f);
            }
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.85f, 0.15f, 0.15f, 0.35f);
        Collider col = GetComponent<Collider>();
        if (col != null)
            Gizmos.DrawWireCube(transform.position, Vector3.one);
    }
}
