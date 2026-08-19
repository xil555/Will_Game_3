using UnityEngine;
using TMPro;

/// <summary>
/// Display-only. Put on the player Canvas TextMeshPro.
/// Reads the current objective from the scene's ObjectiveManager.
/// Do not type the battery/key objective into No Objective Text — that field
/// is only used when a scene has no ObjectiveManager at all.
/// </summary>
public class ObjectiveUI : MonoBehaviour
{
    [Header("References")]
    public TextMeshProUGUI objectiveText;

    [Header("Settings")]
    [Tooltip("Fallback only. Leave blank. Real objective text comes from the scene ObjectiveManager.")]
    public string noObjectiveText = "";

    private ObjectiveManager boundManager;
    private string lastLoggedText;

    void Awake()
    {
        if (objectiveText == null)
            objectiveText = GetComponent<TextMeshProUGUI>();

        Debug.Log("[ObjectiveUI] Running on " + gameObject.name
            + ". Text assigned: " + (objectiveText != null), this);
    }

    void OnEnable()
    {
        BindToManager(true);
        Refresh();
    }

    void OnDisable()
    {
        Unbind();
    }

    void Update()
    {
        BindToManager(false);
        Refresh();
    }

    void BindToManager(bool force)
    {
        ObjectiveManager current = ObjectiveManager.Instance;
        if (current == null)
            current = Object.FindAnyObjectByType<ObjectiveManager>();

        if (current == boundManager && !force)
            return;

        Unbind();

        if (current == null)
            return;

        boundManager = current;
        boundManager.OnObjectiveChanged += Refresh;
        Debug.Log("[ObjectiveUI] Bound to ObjectiveManager '" + current.gameObject.name
            + "'. Showing: " + current.GetDisplayText(), this);
    }

    void Unbind()
    {
        if (boundManager != null)
            boundManager.OnObjectiveChanged -= Refresh;

        boundManager = null;
    }

    void Refresh()
    {
        if (objectiveText == null)
            objectiveText = GetComponent<TextMeshProUGUI>();

        if (objectiveText == null)
            return;

        ObjectiveManager manager = boundManager != null ? boundManager : ObjectiveManager.Instance;
        if (manager == null)
            manager = Object.FindAnyObjectByType<ObjectiveManager>();

        if (manager == null)
        {
            if (lastLoggedText != "__none__")
            {
                lastLoggedText = "__none__";
                Debug.LogWarning("[ObjectiveUI] No ObjectiveManager found in this scene. UI will stay empty unless No Objective Text is set.", this);
            }

            objectiveText.text = noObjectiveText;
            return;
        }

        string display = manager.GetDisplayText();
        objectiveText.text = display;

        if (display != lastLoggedText)
        {
            lastLoggedText = display;
            Debug.Log("[ObjectiveUI] Text updated -> " + display, this);
        }
    }
}
