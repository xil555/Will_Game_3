using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class DialogueTest : MonoBehaviour
{
    [Header("Dialogue Settings")]
    [SerializeField] private string npcID = "TutorialNPC"; 

    [Header("Ranges")]
    [SerializeField] private SphereCollider outerRange; 
    [SerializeField] private SphereCollider innerRange; 

    PlayerStats playerStats; 

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
        }
    }

    private void Start()
    {
        
        playerStats = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerStats>();

        if (playerStats == null)
        {
            Debug.LogError("DialogueTest: Could not find PlayerStats on the Player object! Make sure the tag is set to 'Player'.");
        }
}

    private void Update()
    {
        if (playerInInnerRange && Input.GetKeyDown(KeyCode.E))
    {
        // Check if we meet the requirements for the 2nd dialogue
        if (playerStats.battery >= 60 && playerStats.keys >= 3)
        {
            // Start the special dialogue
            DialogueManager.Instance.StartDialogue("FinishTutorialNPC");
            
            StartCoroutine(WaitAndLoadLevel());
        }
        else
        {
            Interact();
        }
    }
    }

    private void Interact()
    {
        
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
        
        if (sphere == null) return false;
        return other.bounds.Intersects(sphere.bounds);
    } 

    IEnumerator WaitAndLoadLevel()
{
    Debug.Log("Second dialogue started. Loading Level1 in 10 seconds...");

    // Wait for 10 seconds
    yield return new WaitForSeconds(3f);

    SceneManager.LoadScene("Level1");
}
}