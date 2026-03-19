using UnityEngine;

public class DialogueTest : MonoBehaviour
{
    [Header("Dialogue Settings")]
    [SerializeField] private string npcID = "TutorialNPC"; // Change this in Inspector for each NPC

    [Header("Ranges")]
    [SerializeField] private SphereCollider outerRange; 
    [SerializeField] private SphereCollider innerRange; 

    private bool playerInInnerRange = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        // Using your IsFromCollider helper
        if (IsFromCollider(outerRange, other))
        {
            EventDebugManager.Instance.TriggerEvent("NPC: 'Hey! Come here!'");
        }

        if (IsFromCollider(innerRange, other))
        {
            playerInInnerRange = true;
            EventDebugManager.Instance.TriggerEvent("Press E to Talk");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (IsFromCollider(innerRange, other))
        {
            playerInInnerRange = false;
            // Optional: Close dialogue if player walks away
            // DialogueSystem.Instance.EndDialogue(); 
        }
    }

    private void Update()
    {
        // Player presses E in inner range
        if (playerInInnerRange && Input.GetKeyDown(KeyCode.E))
        {
            Interact();
        }
    }

    private void Interact()
    {
        // Check if the UI is already showing
        if (DialogueManager.Instance.IsActive())
        {
            DialogueManager.Instance.DisplayNextSentence();
        }
        else
        {
            DialogueManager.Instance.StartDialogue(npcID);
        }
    }

    private bool IsFromCollider(SphereCollider sphere, Collider other)
    {
        // Null check to prevent errors if you forget to assign one in Inspector
        if (sphere == null) return false;
        return other.bounds.Intersects(sphere.bounds);
    } 
}