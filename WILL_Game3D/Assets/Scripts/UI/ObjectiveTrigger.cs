using UnityEngine;

/// <summary>
/// Place on an empty GameObject with a trigger Collider.
/// When the player enters, it completes a ReachTrigger objective whose Trigger Id matches.
/// </summary>
[RequireComponent(typeof(Collider))]
public class ObjectiveTrigger : MonoBehaviour
{
    [Tooltip("Must match the Trigger Id on a ReachTrigger objective in ObjectiveManager.")]
    public string triggerId = "enter_house";

    [Tooltip("If true, this trigger can only fire once.")]
    public bool triggerOnce = true;

    private bool hasTriggered;

    void Reset()
    {
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (hasTriggered && triggerOnce) return;
        if (!other.CompareTag("Player")) return;
        if (ObjectiveManager.Instance == null) return;

        ObjectiveManager.Instance.ReportTriggerReached(triggerId);
        hasTriggered = true;
    }
}
