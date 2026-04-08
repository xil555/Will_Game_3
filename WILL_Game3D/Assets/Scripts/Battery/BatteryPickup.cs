using UnityEngine;

/* 
    Battery pickup:
    - Adds battery amount (currently just logs)
    - Uses centralized EventDebugManager
    - Destroys pickup object after collection
*/
public class BatteryPickup : MonoBehaviour
{

    PlayerStats playerStats;
    public int batteryAmount = 30;

    void Start()
    {
        playerStats = FindObjectOfType<PlayerStats>();

    }

    private void OnTriggerEnter(Collider other)
    {
        OnBatteryPickup();

        Destroy(gameObject);
    }

    public void OnBatteryPickup()
    {
        // This method can be called by PlayerStats when the battery is picked up
        // For now, it just logs the pickup, but it can be expanded to update player stats
        EventDebugManager.Instance.TriggerEvent("Battery pickup event triggered! +" + batteryAmount);

        playerStats.battery += batteryAmount; // Update the player's battery stat


    }
}