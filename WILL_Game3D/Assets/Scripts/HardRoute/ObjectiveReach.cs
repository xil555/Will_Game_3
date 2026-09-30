using UnityEngine;

/// <summary>
/// Walk into this trigger to complete a ReachTrigger objective.
/// If a DialogueTrigger is on the same object, the lines play first, then the objective moves on.
/// </summary>
[RequireComponent(typeof(Collider))]
public class ObjectiveReach : MonoBehaviour
{
    public string triggerId = "HardHome";
    public bool playOnce = true;
    public string playerTag = "Player";

    bool done;
    bool waitingForDialogue;
    DialogueTrigger talk;

    void Reset()
    {
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (done && playOnce)
            return;
        if (!other.CompareTag(playerTag))
            return;
        if (ObjectiveManager.Instance == null)
            return;

        talk = GetComponent<DialogueTrigger>();
        if (talk != null && talk.ShouldDelayReach)
        {
            waitingForDialogue = true;
            return;
        }

        Complete();
    }

    void Update()
    {
        if (done || !waitingForDialogue)
            return;

        if (talk != null && talk.ShouldDelayReach)
            return;

        if (talk != null && !talk.HasFinishedDialogue)
            return;

        Complete();
    }

    void Complete()
    {
        if (done)
            return;

        if (!IsCurrentReach())
        {
            waitingForDialogue = false;
            return;
        }

        done = true;
        waitingForDialogue = false;

        if (ObjectiveManager.Instance != null)
            ObjectiveManager.Instance.ReportTriggerReached(triggerId);
    }

    bool IsCurrentReach()
    {
        if (ObjectiveManager.Instance == null || !ObjectiveManager.Instance.HasActiveObjective)
            return false;

        ObjectiveData current = ObjectiveManager.Instance.CurrentObjective;
        if (current.type != ObjectiveType.ReachTrigger)
            return false;

        return string.Equals(current.triggerId, triggerId, System.StringComparison.OrdinalIgnoreCase);
    }
}
