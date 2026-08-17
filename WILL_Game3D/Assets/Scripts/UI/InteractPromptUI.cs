using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Screen-space "Press E" prompt on the player's Canvas.
/// Hide spots and pickups call Show/Hide when the player enters their trigger.
/// </summary>
public class InteractPromptUI : MonoBehaviour
{
    public static InteractPromptUI Instance { get; private set; }

    [Header("Pop")]
    public float popSpeed = 10f;

    TextMeshProUGUI label;
    CanvasGroup group;
    RectTransform root;
    object currentSource;
    float shown;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Hook()
    {
        SceneReady();
    }

    public static InteractPromptUI Ensure()
    {
        if (Instance != null)
            return Instance;

        return CreateOnPlayerCanvas();
    }

    public static void Show(object source, string text)
    {
        InteractPromptUI ui = Ensure();
        if (ui == null || source == null || string.IsNullOrEmpty(text))
            return;
        ui.ShowInternal(source, text);
    }

    public static void Hide(object source)
    {
        if (Instance == null || source == null)
            return;
        Instance.HideInternal(source);
    }

    void Awake()
    {
        Instance = this;
        if (group == null)
            Build();
        SetShown(0f);
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Update()
    {
        if (PauseMenu.IsPaused || PlayerDeath.IsDead)
        {
            SetShown(0f);
            return;
        }

        float target = currentSource != null ? 1f : 0f;
        shown = Mathf.MoveTowards(shown, target, Time.unscaledDeltaTime * popSpeed);
        SetShown(shown);
    }

    void ShowInternal(object source, string text)
    {
        currentSource = source;
        if (label != null)
            label.text = text;
    }

    void HideInternal(object source)
    {
        if (currentSource == source)
            currentSource = null;
    }

    void SetShown(float t)
    {
        if (group != null)
            group.alpha = t;
        if (root != null)
            root.localScale = Vector3.one * (0.85f + t * 0.15f);
    }

    void Build()
    {
        root = GetComponent<RectTransform>();
        group = gameObject.GetComponent<CanvasGroup>();
        if (group == null)
            group = gameObject.AddComponent<CanvasGroup>();
        group.interactable = false;
        group.blocksRaycasts = false;

        label = GetComponentInChildren<TextMeshProUGUI>(true);
        if (label != null)
            return;

        GameObject textGo = new GameObject("Label");
        textGo.transform.SetParent(transform, false);
        label = textGo.AddComponent<TextMeshProUGUI>();
        label.alignment = TextAlignmentOptions.Center;
        label.fontStyle = FontStyles.Bold;
        label.fontSize = 36f;
        label.color = Color.white;
        label.enableWordWrapping = false;
        label.raycastTarget = false;
        label.outlineWidth = 0.22f;
        label.outlineColor = new Color(0f, 0f, 0f, 0.9f);
        RectTransform textRt = label.rectTransform;
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;
    }

    static void SceneReady()
    {
        CreateOnPlayerCanvas();
    }

    static InteractPromptUI CreateOnPlayerCanvas()
    {
        if (Instance != null)
            return Instance;

        Canvas canvas = FindPlayerCanvas();
        if (canvas == null)
            return null;

        Transform existing = canvas.transform.Find("InteractPrompt");
        GameObject go = existing != null ? existing.gameObject : new GameObject("InteractPrompt");
        if (existing == null)
            go.transform.SetParent(canvas.transform, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        if (rt == null)
            rt = go.AddComponent<RectTransform>();

        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 90f);
        rt.sizeDelta = new Vector2(720f, 64f);

        InteractPromptUI ui = go.GetComponent<InteractPromptUI>();
        if (ui == null)
            ui = go.AddComponent<InteractPromptUI>();

        return ui;
    }

    static Canvas FindPlayerCanvas()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Canvas[] onPlayer = player.GetComponentsInChildren<Canvas>(true);
            for (int i = 0; i < onPlayer.Length; i++)
            {
                if (onPlayer[i] != null && onPlayer[i].renderMode != RenderMode.WorldSpace)
                    return onPlayer[i];
            }
        }

#if UNITY_6000_0_OR_NEWER
        Canvas[] all = FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
#else
        Canvas[] all = FindObjectsOfType<Canvas>();
#endif
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != null && all[i].renderMode == RenderMode.ScreenSpaceOverlay)
                return all[i];
        }

        return null;
    }
}
