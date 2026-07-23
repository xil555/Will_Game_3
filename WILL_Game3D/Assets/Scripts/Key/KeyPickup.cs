using UnityEngine;

public class KeyPickup : MonoBehaviour
{
    [SerializeField] private int keyAmount = 1;

    PlayerStats playerStats;

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
            Debug.LogWarning("KeyPickup: Could not find PlayerStats on the Player!");
            return;
        }

        playerStats.keys += keyAmount;

        if (ObjectiveManager.Instance != null)
            ObjectiveManager.Instance.ReportKeyCollected(keyAmount);

        if (EventDebugManager.Instance != null)
            EventDebugManager.Instance.TriggerEvent("Key pickup event triggered! +" + keyAmount);

        Destroy(gameObject);
    }
}