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

    PlayerStats playerStats;
    void Start()
    {
        playerStats = FindObjectOfType<PlayerStats>();
    }

    private void OnTriggerEnter(Collider other)
    {
        OnKeyPickup();

        Destroy(gameObject);
    }

    public void OnKeyPickup()
    {
        // This method can be called by PlayerStats when the key is picked up
        // For now, it just logs the pickup, but it can be expanded to update player stats
        EventDebugManager.Instance.TriggerEvent("Key pickup event triggered! +" + keyAmount);

        playerStats.keys += keyAmount; // Update the player's key stat
    }
}