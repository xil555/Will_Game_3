using UnityEngine;

/* 
    Key Pickup:
    - Increases key amount (currently just logs)
    - Uses centralized EventDebugManager
    - Destroys pickup object after collection
    - Ready for UI popup later
*/
public class KeyPickup : MonoBehaviour
{
    [SerializeField] private int keyAmount = 1;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        EventDebugManager.Instance.TriggerEvent("Picked up Key! +" + keyAmount);

        // TODO: later actually add to players key UI inventory

        Destroy(gameObject);
    }
}