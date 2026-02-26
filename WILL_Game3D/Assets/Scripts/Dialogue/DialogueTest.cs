using UnityEngine;


/*TODO
    triggers player dialogue if enters range 
    will display UI display calling player over 
 */
public class DialogueTest : MonoBehaviour
{
    [SerializeField] private SphereCollider outerRange; 
    [SerializeField] private SphereCollider innerRange; 

    private bool playerInOuterRange = false;
    private bool playerInInnerRange = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        // Player enters outer range
        if (IsFromCollider(outerRange, other))
        {
            playerInOuterRange = true;
            EventDebugManager.Instance.TriggerEvent("Player entered outer range: Come here");
        }

        // Player enters inner range
        if (IsFromCollider(innerRange, other))
        {
            playerInInnerRange = true;
            EventDebugManager.Instance.TriggerEvent("Player entered inner range: Press E to interact");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (IsFromCollider(outerRange, other))
            playerInOuterRange = false;

        if (IsFromCollider(innerRange, other))
            playerInInnerRange = false;
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
        EventDebugManager.Instance.TriggerEvent("Player pressed E: UI Dialogue Tutorial");
    }

    // Helper method to check which collider was triggered
    private bool IsFromCollider(SphereCollider sphere, Collider other)
    {
        // Checks if the other collider is touching the sphere bounds
        return other.bounds.Intersects(sphere.bounds);
    } 
}
