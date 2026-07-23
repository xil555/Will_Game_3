using UnityEngine;

public class GameManager : MonoBehaviour
{
    PlayerStats playerStats;

    void Start()
    {
        playerStats = FindObjectOfType<PlayerStats>();
    }

    void Update()
    {
        // Player may be spawned at runtime — keep trying until found
        if (playerStats == null)
        {
            playerStats = FindObjectOfType<PlayerStats>();
            return;
        }

        if (playerStats.keys >= 3)
        {
            EventDebugManager.Instance.TriggerEvent("Player has more than 3 keys!");
        }

        if (playerStats.battery >= 60)
        {
            EventDebugManager.Instance.TriggerEvent("Player has about 60% battery!");
            EventDebugManager.Instance.TriggerEvent("go Back to the stranger");
        }
    }
}
