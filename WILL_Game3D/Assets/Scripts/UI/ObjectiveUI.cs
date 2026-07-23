using UnityEngine;
using TMPro;

/// <summary>
/// Display-only. Put on the player Canvas TextMeshPro.
/// Reads the current objective from the scene's ObjectiveManager.
/// </summary>
public class ObjectiveUI : MonoBehaviour
{
    [Header("References")]
    public TextMeshProUGUI objectiveText;

    [Header("Settings")]
    [Tooltip("Text shown when the scene has no ObjectiveManager.")]
    public string noObjectiveText = "";

    private bool isSubscribed;

    void OnEnable()
    {
        TrySubscribe();
        Refresh();
    }

    void OnDisable()
    {
        Unsubscribe();
    }

    void Update()
    {
        // Scene changed / manager destroyed — clear stale subscription
        if (isSubscribed && ObjectiveManager.Instance == null)
        {
            isSubscribed = false;
            Refresh();
            return;
        }

        if (!isSubscribed)
            TrySubscribe();
    }

    void TrySubscribe()
    {
        if (ObjectiveManager.Instance == null || isSubscribed) return;

        ObjectiveManager.Instance.OnObjectiveChanged += Refresh;
        isSubscribed = true;
        Refresh();
    }

    void Unsubscribe()
    {
        if (!isSubscribed) return;

        if (ObjectiveManager.Instance != null)
            ObjectiveManager.Instance.OnObjectiveChanged -= Refresh;

        isSubscribed = false;
    }

    void Refresh()
    {
        if (objectiveText == null) return;

        if (ObjectiveManager.Instance == null)
        {
            objectiveText.text = noObjectiveText;
            return;
        }

        objectiveText.text = ObjectiveManager.Instance.GetDisplayText();
    }
}
