using UnityEngine;

/* 
    Battery pickup:
    - Adds battery amount (currently just logs)
    - Uses centralized EventDebugManager
    - Destroys pickup object after collection
*/
public class BatteryPickup : MonoBehaviour
{
    public int batteryAmount = 30;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        // Log the pickup through centralized event system
        EventDebugManager.Instance.TriggerEvent("Picked up battery! +" + batteryAmount);

        // TODO: later add to player's flashlight battery

        Destroy(gameObject);
    }
}