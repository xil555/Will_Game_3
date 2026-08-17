using System.Collections;
using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] public float health = 100f;

    [Header("Inventory")]
    [SerializeField] public int keys = 0;
    [SerializeField] public int battery = 0;
    [HideInInspector] public int batteriesCollected = 0;
    [Tooltip("Stored energy drinks. Press the drink key to consume one.")]
    public int energyDrinks = 0;
    public KeyCode drinkKey = KeyCode.Q;

    [Header("Stamina")]
    public float maxStamina = 100f;
    [HideInInspector] public float stamina;

    [Header("Stored Drink Effect")]
    public float storedDrinkRestore = 50f;
    public float storedDrinkDuration = 8f;
    public float storedDrinkDrainMultiplier = 0.5f;
    public float storedDrinkRegenMultiplier = 1.8f;
    public float storedDrinkSprintMultiplier = 1.12f;

    public float StaminaDrainMultiplier { get; private set; } = 1f;
    public float StaminaRegenMultiplier { get; private set; } = 1f;
    public float SprintSpeedMultiplier { get; private set; } = 1f;
    public bool HasStaminaBoost { get; private set; }
    public bool StaminaRestoredFlag { get; set; }

    Coroutine boostRoutine;

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

        if (PauseMenu.IsPaused || PlayerDeath.IsDead)
            return;

        if (Input.GetKeyDown(drinkKey))
            TryUseStoredEnergyDrink();
    }

    void OnDisable()
    {
        ClearBoost();
    }

    public void RestoreStamina(float amount)
    {
        if (amount <= 0f)
            return;

        stamina = Mathf.Clamp(stamina + amount, 0f, maxStamina);
        StaminaRestoredFlag = true;
    }

    public bool TryUseStoredEnergyDrink()
    {
        if (energyDrinks <= 0)
            return false;

        energyDrinks--;
        ApplyEnergyDrink(
            storedDrinkRestore,
            storedDrinkDuration,
            storedDrinkDrainMultiplier,
            storedDrinkRegenMultiplier,
            storedDrinkSprintMultiplier);
        return true;
    }

    public void ApplyEnergyDrink(
        float restoreAmount,
        float boostDuration,
        float drainMultiplier,
        float regenMultiplier,
        float sprintMultiplier)
    {
        RestoreStamina(restoreAmount);

        if (boostDuration <= 0f)
            return;

        ClearBoost();
        boostRoutine = StartCoroutine(BoostRoutine(
            boostDuration,
            Mathf.Max(0.05f, drainMultiplier),
            Mathf.Max(0.05f, regenMultiplier),
            Mathf.Max(0.05f, sprintMultiplier)));
    }

    public void AddEnergyDrink(int amount = 1)
    {
        energyDrinks = Mathf.Max(0, energyDrinks + amount);
    }

    IEnumerator BoostRoutine(float duration, float drain, float regen, float sprint)
    {
        HasStaminaBoost = true;
        StaminaDrainMultiplier = drain;
        StaminaRegenMultiplier = regen;
        SprintSpeedMultiplier = sprint;

        yield return new WaitForSeconds(duration);

        StaminaDrainMultiplier = 1f;
        StaminaRegenMultiplier = 1f;
        SprintSpeedMultiplier = 1f;
        HasStaminaBoost = false;
        boostRoutine = null;
    }

    void ClearBoost()
    {
        if (boostRoutine != null)
        {
            StopCoroutine(boostRoutine);
            boostRoutine = null;
        }

        HasStaminaBoost = false;
        StaminaDrainMultiplier = 1f;
        StaminaRegenMultiplier = 1f;
        SprintSpeedMultiplier = 1f;
    }
}
