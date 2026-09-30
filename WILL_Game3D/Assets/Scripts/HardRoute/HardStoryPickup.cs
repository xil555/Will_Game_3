using UnityEngine;

/// <summary>
/// Press E to pick up the wallet or keys. The object is removed. Nothing goes into an inventory.
/// Completes a ReachTrigger objective and can play a thought line.
/// On the keys, drag the enemy in so they only wake up after the keys are taken.
/// </summary>
[RequireComponent(typeof(Collider))]
public class HardStoryPickup : MonoBehaviour
{
    public string triggerId = "HardWallet";
    public string thoughtDialogueId;
    public string prompt = "Press E to pick up";
    public string playerTag = "Player";
    [Tooltip("Optional. Turned on when this is picked up. Use this on the keys for whoever is inside the house.")]
    public GameObject enableOnPickup;

    bool playerInside;
    bool taken;

    void Reset()
    {
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void Start()
    {
        if (enableOnPickup != null)
            enableOnPickup.SetActive(false);
    }

    void OnTriggerEnter(Collider other)
    {
        if (taken || !other.CompareTag(playerTag))
            return;

        playerInside = true;
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
        if (!playerInside || taken || PauseMenu.IsPaused || PlayerDeath.IsDead)
            return;
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsActive())
            return;
        if (!Input.GetKeyDown(KeyCode.E))
            return;

        taken = true;
        InteractPromptUI.Hide(this);

        if (enableOnPickup != null)
            enableOnPickup.SetActive(true);

        if (ObjectiveManager.Instance != null)
            ObjectiveManager.Instance.ReportTriggerReached(triggerId);

        if (!string.IsNullOrEmpty(thoughtDialogueId) && DialogueManager.Instance != null)
            DialogueManager.Instance.StartDialogue(thoughtDialogueId);

        gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        InteractPromptUI.Hide(this);
    }
}
