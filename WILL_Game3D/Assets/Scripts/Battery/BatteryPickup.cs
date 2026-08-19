using UnityEngine;

public class BatteryPickup : MonoBehaviour
{
    PlayerStats playerStats;
    public int batteryAmount = 30;
    public string pickupPrompt = "Press E to pick up battery";

    [Header("Battery cap")]
    [Tooltip("Max battery the player can hold. Should match Flashlight maxBattery.")]
    public int maxBattery = 100;

    [Header("Pickup Indicator")]
    public bool showPickupIndicator = true;
    public Vector3 indicatorOffset = new Vector3(0f, 1.2f, 0f);
    public Color indicatorColor = new Color(1f, 0.86f, 0.2f, 0.95f);
    [Tooltip("Optional custom arrow prefab. Leave empty to use the default down-arrow.")]
    public GameObject indicatorPrefab;
    [Tooltip("How far away the player can be and still see the arrow.")]
    public float indicatorVisibleRange = 40f;

    PickupIndicator indicator;
    bool playerInside;

    void Start()
    {
        playerStats = Object.FindAnyObjectByType<PlayerStats>();
        if (showPickupIndicator)
            indicator = PickupIndicator.Ensure(transform, indicatorOffset, indicatorColor, indicatorPrefab, indicatorVisibleRange);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInside = true;
        if (playerStats == null)
            playerStats = other.GetComponent<PlayerStats>() ?? other.GetComponentInParent<PlayerStats>();

        InteractPromptUI.Show(this, pickupPrompt);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInside = false;
        InteractPromptUI.Hide(this);
    }

    void Update()
    {
        if (!playerInside || PauseMenu.IsPaused || PlayerDeath.IsDead)
            return;
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsActive())
            return;
        if (!Input.GetKeyDown(KeyCode.E))
            return;

        Collect();
    }

    void OnDestroy()
    {
        InteractPromptUI.Hide(this);
    }

    void Collect()
    {
        if (playerStats == null)
            playerStats = Object.FindAnyObjectByType<PlayerStats>();

        if (playerStats == null)
        {
            Debug.LogWarning("BatteryPickup: Could not find PlayerStats on the Player!");
            return;
        }

        playerStats.battery = Mathf.Min(playerStats.battery + batteryAmount, maxBattery);
        playerStats.batteriesCollected++;

        if (ObjectiveManager.Instance != null)
            ObjectiveManager.Instance.ReportBatteryCollected(1);
        else
            Debug.LogWarning("[Objective] Battery picked up but no ObjectiveManager exists in this scene.");

        if (EventDebugManager.Instance != null)
            EventDebugManager.Instance.TriggerEvent("Battery pickup event triggered! +" + batteryAmount);

        InteractPromptUI.Hide(this);
        if (indicator != null)
            indicator.Hide();

        Destroy(gameObject);
    }
}
