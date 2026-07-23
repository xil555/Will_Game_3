using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] public float health = 100f;

    [Header("Inventory")]
    [SerializeField] public int keys = 0;
    [SerializeField] public int battery = 0;
    [HideInInspector] public int batteriesCollected = 0;

    [Header("Stamina")]
    public float maxStamina = 100f;
    [HideInInspector] public float stamina;

    void Start()
    {
        stamina = maxStamina;
        Debug.Log("PlayerStats initialized.");
    }

    void Update()
    {
        if (battery > 60)
        {
            Debug.Log("Battery level good");
        }
    }
}
