using UnityEngine;

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
        if (!other.CompareTag("Player")) return;

        // Fallback: find PlayerStats on the colliding object directly
        if (playerStats == null)
            playerStats = other.GetComponent<PlayerStats>();

        if (playerStats == null)
        {
            Debug.LogWarning("BatteryPickup: Could not find PlayerStats on the Player!");
            return;
        }

        playerStats.battery += batteryAmount;
        playerStats.batteriesCollected++;

        if (ObjectiveManager.Instance != null)
            ObjectiveManager.Instance.ReportBatteryCollected(1);

        if (EventDebugManager.Instance != null)
            EventDebugManager.Instance.TriggerEvent("Battery pickup event triggered! +" + batteryAmount);

        Destroy(gameObject);
    }
}