using TMPro;
using UnityEngine;

/// <summary>
/// Place on a trigger collider (closet, dumpster, bushes). Player presses E to hide.
/// Hidden players are invisible to the enemy's vision cone.
/// Shows a down arrow over the spot and a Press E prompt when the player can hide.
/// </summary>
[RequireComponent(typeof(Collider))]
public class HideSpot : MonoBehaviour
{
    [Tooltip("Optional point the camera/player snaps toward while hidden.")]
    public Transform hidePoint;
    public string enterPrompt = "Press E to hide";
    public string exitPrompt = "Press E to leave";

    [Header("Marker")]
    public bool showMarker = true;
    [Tooltip("How high above the collider the arrow sits.")]
    public float markerHeight = 2.15f;
    public float markerVisibleRange = 22f;
    public Color markerColor = new Color(1f, 0.86f, 0.2f, 0.95f);

    bool playerInside;
    Transform playerTransform;
    Vector3 enterPosition;
    Quaternion enterRotation;

    Transform markerRoot;
    TextMeshPro arrowText;

    void Reset()
    {
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void Awake()
    {
        if (showMarker)
            CreateMarker();
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInside = true;
        playerTransform = other.transform;
        InteractPromptUI.Show(this, enterPrompt);
        if (EventDebugManager.Instance != null)
            EventDebugManager.Instance.TriggerEvent(enterPrompt);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        // Enemy collision can shove the player out of the trigger. Stay hidden until they press E.
        if (PlayerStealth.Instance != null && PlayerStealth.Instance.IsHidden && PlayerStealth.Instance.CurrentSpot == this)
            return;

        playerInside = false;
        InteractPromptUI.Hide(this);
    }

    void Update()
    {
        if (PauseMenu.IsPaused || PlayerDeath.IsDead)
            return;

        PlayerStealth stealth = PlayerStealth.Instance;
        if (stealth == null)
            return;

        bool hidingHere = stealth.IsHidden && stealth.CurrentSpot == this;

        if (playerInside)
            InteractPromptUI.Show(this, hidingHere ? exitPrompt : enterPrompt);

        if (hidingHere && Input.GetKeyDown(KeyCode.E))
        {
            Leave();
            return;
        }

        if (playerInside && !stealth.IsHidden && Input.GetKeyDown(KeyCode.E))
        {
            if (DialogueManager.Instance != null && DialogueManager.Instance.IsActive())
                return;
            Hide();
        }
    }

    void LateUpdate()
    {
        UpdateMarker();
    }

    void Hide()
    {
        if (playerTransform == null)
            return;

        EnsureStealth(playerTransform.gameObject);

        enterPosition = playerTransform.position;
        enterRotation = playerTransform.rotation;

        if (hidePoint != null)
        {
            playerTransform.position = hidePoint.position;
            playerTransform.rotation = hidePoint.rotation;
        }

        PlayerStealth.Instance.EnterHide(this);
    }

    void Leave()
    {
        if (playerTransform != null)
        {
            playerTransform.position = enterPosition;
            playerTransform.rotation = enterRotation;
        }

        if (PlayerStealth.Instance != null)
            PlayerStealth.Instance.ExitHide();
    }

    void OnDestroy()
    {
        InteractPromptUI.Hide(this);
    }

    static void EnsureStealth(GameObject player)
    {
        if (player.GetComponent<PlayerStealth>() == null)
            player.AddComponent<PlayerStealth>();
    }

    void CreateMarker()
    {
        Collider col = GetComponent<Collider>();
        float top = col != null ? col.bounds.max.y : transform.position.y;
        Vector3 worldPos = col != null
            ? new Vector3(col.bounds.center.x, top + 0.55f, col.bounds.center.z)
            : transform.position + Vector3.up * markerHeight;

        GameObject rootGo = new GameObject("HideSpotMarker");
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
        tmp.enableWordWrapping = false;
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
        bool hidingHere = PlayerStealth.Instance != null
            && PlayerStealth.Instance.IsHidden
            && PlayerStealth.Instance.CurrentSpot == this;

        float dist = float.MaxValue;
        if (cam != null)
        {
            dist = Vector3.Distance(cam.transform.position, markerRoot.position);
            markerRoot.rotation = Quaternion.LookRotation(markerRoot.position - cam.transform.position);
        }

        bool inViewRange = dist <= markerVisibleRange;
        if (arrowText != null)
        {
            bool showArrow = showMarker && inViewRange && !hidingHere;
            arrowText.enabled = showArrow;
            if (showArrow)
            {
                float bob = Mathf.Sin(Time.time * 3.2f) * 0.12f;
                arrowText.transform.localPosition = new Vector3(0f, 0.15f + bob, 0f);
            }
        }
    }
}
