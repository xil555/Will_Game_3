using UnityEngine;

public class GameManager : MonoBehaviour
{

    PlayerStats playerStats;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        playerStats = FindObjectOfType<PlayerStats>();
    }

    // Update is called once per frame
    void Update()
    {
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
