using UnityEngine;
/// <summary>
/// World pickup. Stand in the trigger to see a HUD prompt, then press E to drink.
/// </summary>
[RequireComponent(typeof(Collider))]
public class EnergyDrink : MonoBehaviour
{
    [Header("Pickup")]
    [Tooltip("If true, E drinks it immediately. If false, E stores it (use later with Q).")]
    public bool consumeOnContact = true;
    public bool destroyOnUse = true;
    public AudioClip drinkClip;
    [Header("Stamina Restore")]
    public float staminaRestore = 50f;
    [Header("Temporary Boost")]
    public float boostDuration = 8f;
    [Tooltip("1 = normal drain. 0.5 = sprint costs half stamina.")]
    public float drainMultiplier = 0.5f;
    [Tooltip("1 = normal regen. 2 = refill twice as fast.")]
    public float regenMultiplier = 1.8f;
    [Tooltip("1 = normal sprint speed.")]
    public float sprintSpeedMultiplier = 1.12f;
    [Tooltip("Shown on the player HUD while standing in the trigger.")]
    public string pickupPrompt = "Press E to pick up energy drink";
    [Header("Pickup Indicator")]
    public bool showPickupIndicator = true;
    public Vector3 indicatorOffset = new Vector3(0f, 1.2f, 0f);
    public Color indicatorColor = new Color(0.25f, 0.95f, 1f, 0.95f);
    [Tooltip("Optional custom arrow prefab. Leave empty to use the default down-arrow.")]
    public GameObject indicatorPrefab;
    [Tooltip("How far away the player can be and still see the arrow.")]
    public float indicatorVisibleRange = 40f;
    PickupIndicator indicator;
    PlayerStats cachedStats;
    bool playerInside;
    void Reset()
    {
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }
    void Start()
    {
        cachedStats = Object.FindFirstObjectByType<PlayerStats>();
        if (showPickupIndicator)
            indicator = PickupIndicator.Ensure(transform, indicatorOffset, indicatorColor, indicatorPrefab, indicatorVisibleRange);
    }
    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;
        playerInside = true;
        cachedStats = ResolveStats(other);
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
        PlayerStats stats = cachedStats != null ? cachedStats : Object.FindFirstObjectByType<PlayerStats>();
        if (stats == null)
        {
            Debug.LogWarning("EnergyDrink: Could not find PlayerStats on the Player!");
            return;
        }
        if (consumeOnContact)
            Consume(stats);
        else
        {
            stats.AddEnergyDrink(1);
            if (EventDebugManager.Instance != null)
                EventDebugManager.Instance.TriggerEvent("Energy drink picked up.");
            FinishPickup();
        }
    }
    void OnDestroy()
    {
        InteractPromptUI.Hide(this);
    }
    public bool Consume(PlayerStats stats)
    {
        if (stats == null)
            return false;
        stats.ApplyEnergyDrink(
            staminaRestore,
            boostDuration,
            drainMultiplier,
            regenMultiplier,
            sprintSpeedMultiplier);
        if (drinkClip != null)
        {
            Vector3 pos = stats.transform.position;
            AudioSource.PlayClipAtPoint(drinkClip, pos);
        }
        if (EventDebugManager.Instance != null)
            EventDebugManager.Instance.TriggerEvent("Energy drink used. Stamina boosted.");
        FinishPickup();
        return true;
    }
    void FinishPickup()
    {
        if (ObjectiveManager.Instance != null)
            ObjectiveManager.Instance.ReportEnergyDrinkCollected(1);
        InteractPromptUI.Hide(this);
        if (indicator != null)
            indicator.Hide();
        if (destroyOnUse)
            Destroy(gameObject);
    }
    PlayerStats ResolveStats(Collider other)
    {
        if (cachedStats != null)
            return cachedStats;
        cachedStats = other.GetComponent<PlayerStats>();
        if (cachedStats == null)
            cachedStats = other.GetComponentInParent<PlayerStats>();
        if (cachedStats == null)
            cachedStats = Object.FindFirstObjectByType<PlayerStats>();
        return cachedStats;
    }
}