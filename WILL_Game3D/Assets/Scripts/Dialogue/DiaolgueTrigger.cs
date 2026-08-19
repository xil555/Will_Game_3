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

        if (playOnce && hasPlayed && !FollowUpAvailable())
            return;

        playerInside = true;
        playerTransform = other.transform;

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
        if (startingFollowUp)
            followUpPlayed = true;
        else
            hasPlayed = true;
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

        DialogueManager.Instance.StartDialogue(idToPlay);

        if (ObjectiveManager.Instance != null && !string.IsNullOrEmpty(reportId))
            ObjectiveManager.Instance.ReportNpcTalked(reportId);
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
        Collider col = GetComponent<Collider>();
        float top = col != null ? col.bounds.max.y : transform.position.y;
        Vector3 worldPos = col != null
            ? new Vector3(col.bounds.center.x, top + 0.55f, col.bounds.center.z)
            : transform.position + Vector3.up * markerHeight;

        GameObject rootGo = new GameObject("DialogueMarker");
        rootGo.transform.SetParent(transform, false);
        rootGo.transform.position = worldPos;
        markerRoot = rootGo.transform;

        arrowText = CreateWorldText(rootGo.transform, "Arrow", "▼", 8f, new Vector3(0f, 0.15f, 0f));
        arrowText.color = markerColor;
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

        if (arrowText != null)
        {
            bool showArrow = showMarker && inViewRange && !dialogueOpen && !isStarting && !hideBecausePlayed;
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
