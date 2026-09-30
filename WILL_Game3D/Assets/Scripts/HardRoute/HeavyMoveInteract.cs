using UnityEngine;

/// <summary>
/// Stand by the couch and press E.
/// The one by the door turns off, and the one already placed at the drop spot turns on.
/// </summary>
[RequireComponent(typeof(Collider))]
public class HeavyMoveInteract : MonoBehaviour
{
    [Tooltip("A copy of the couch, already sitting where it should end up. Leave it disabled.")]
    public GameObject couchAfterMove;
    public string triggerId = "HardFurnitureSpot";
    public string prompt = "Press E to move the couch";
    public string playerTag = "Player";

    bool playerInside;
    bool moved;

    void Reset()
    {
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void Awake()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
            col.isTrigger = true;
    }

    void Start()
    {
        if (couchAfterMove != null)
            couchAfterMove.SetActive(false);
    }

    void OnTriggerEnter(Collider other)
    {
        if (moved || !other.CompareTag(playerTag))
            return;

        playerInside = true;
        if (IsFurnitureObjective())
            InteractPromptUI.Show(this, prompt);
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
        if (!playerInside || moved || PauseMenu.IsPaused || PlayerDeath.IsDead)
            return;
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsActive())
            return;
        if (!IsFurnitureObjective())
            return;
        if (!Input.GetKeyDown(KeyCode.E))
            return;

        MoveCouch();
    }

    void MoveCouch()
    {
        if (couchAfterMove == null)
        {
            Debug.LogWarning("HeavyMoveInteract: assign Couch After Move, the couch sitting at the drop spot.");
            return;
        }

        moved = true;
        InteractPromptUI.Hide(this);
        couchAfterMove.SetActive(true);
        gameObject.SetActive(false);

        if (ObjectiveManager.Instance != null)
            ObjectiveManager.Instance.ReportTriggerReached(triggerId);
    }

    bool IsFurnitureObjective()
    {
        if (ObjectiveManager.Instance == null || !ObjectiveManager.Instance.HasActiveObjective)
            return true;

        if (ObjectiveManager.Instance.CurrentObjective.type != ObjectiveType.ReachTrigger)
            return false;

        return string.Equals(
            ObjectiveManager.Instance.CurrentObjective.triggerId,
            triggerId,
            System.StringComparison.OrdinalIgnoreCase);
    }

    void OnDestroy()
    {
        InteractPromptUI.Hide(this);
    }
}
