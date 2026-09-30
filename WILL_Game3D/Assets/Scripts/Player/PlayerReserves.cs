using UnityEngine;
/// <summary>
/// Spare batteries and energy drinks. The bars stay as they are until they hit empty,
/// then one reserve is used automatically.
/// </summary>
public class PlayerReserves : MonoBehaviour
{
    public const int MaxSlots = 3;
    public int batteryCount;
    public int drinkCount;
    float staminaRestore = 50f;
    float boostDuration = 8f;
    float drainMultiplier = 0.5f;
    float regenMultiplier = 1.8f;
    float sprintSpeedMultiplier = 1.12f;
    AudioClip drinkClip;
    PlayerStats stats;
    float previousStamina = -1f;
    public static PlayerReserves Get(PlayerStats playerStats)
    {
        if (playerStats == null)
            return null;
        PlayerReserves reserves = playerStats.GetComponent<PlayerReserves>();
        if (reserves == null)
            reserves = playerStats.gameObject.AddComponent<PlayerReserves>();
        reserves.stats = playerStats;
        return reserves;
    }
    void Awake()
    {
        if (stats == null)
            stats = GetComponent<PlayerStats>();
    }
    void Update()
    {
        if (stats == null)
            stats = GetComponent<PlayerStats>();
        if (stats == null || PauseMenu.IsPaused || PlayerDeath.IsDead)
            return;
        float stamina = stats.stamina;
        if (previousStamina < 0f)
            previousStamina = stamina;
        if (previousStamina > 0.01f && stamina <= 0.01f)
            TryAutoUseDrink();
        previousStamina = stats.stamina;
    }
    public bool TryAddBattery()
    {
        if (batteryCount >= MaxSlots)
            return false;
        batteryCount++;
        return true;
    }
    public bool TryConsumeBattery()
    {
        if (batteryCount <= 0)
            return false;
        batteryCount--;
        return true;
    }
    public bool TryAddDrink(float restore, float duration, float drain, float regen, float sprint, AudioClip clip)
    {
        if (drinkCount >= MaxSlots)
            return false;
        drinkCount++;
        staminaRestore = restore;
        boostDuration = duration;
        drainMultiplier = drain;
        regenMultiplier = regen;
        sprintSpeedMultiplier = sprint;
        drinkClip = clip;
        return true;
    }
    public bool TryAutoUseDrink()
    {
        if (stats == null || drinkCount <= 0)
            return false;
        drinkCount--;
        stats.ApplyEnergyDrink(
            staminaRestore,
            boostDuration,
            drainMultiplier,
            regenMultiplier,
            sprintSpeedMultiplier);
        if (drinkClip != null)
            AudioSource.PlayClipAtPoint(drinkClip, stats.transform.position);
        if (EventDebugManager.Instance != null)
            EventDebugManager.Instance.TriggerEvent("Energy drink used from reserve.");
        previousStamina = stats.stamina;
        return true;
    }
}