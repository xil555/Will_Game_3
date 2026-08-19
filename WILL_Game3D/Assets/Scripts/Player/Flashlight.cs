using UnityEngine;

public class Flashlight : MonoBehaviour
{
    public GameObject ON;
    public GameObject OFF;

    [Header("Battery")]
    public PlayerStats playerStats;
    public int maxBattery = 100;
    [Tooltip("Battery lost per second while the flashlight is ON.")]
    public float drainPerSecond = 1.5f;

    bool isON;
    float drainRemainder;

    public bool IsOn => isON;

    public int CurrentBattery
    {
        get { return playerStats != null ? playerStats.battery : 0; }
    }

    void Awake()
    {
        FixInvalidColliders();
    }

    void Start()
    {
        FixInvalidColliders();

        if (playerStats == null)
            playerStats = Object.FindAnyObjectByType<PlayerStats>();

        if (playerStats != null && playerStats.battery <= 0)
            playerStats.battery = maxBattery;

        SetFlashlight(false);
    }

    void Update()
    {
        if (PauseMenu.IsPaused || PlayerDeath.IsDead)
            return;

        DrainBatteryWhileOn();

        if (Input.GetKeyDown(KeyCode.F))
            TryToggle();
    }

    void DrainBatteryWhileOn()
    {
        if (!isON || playerStats == null)
            return;

        drainRemainder += drainPerSecond * Time.deltaTime;
        int drainNow = Mathf.FloorToInt(drainRemainder);
        if (drainNow <= 0)
            return;

        drainRemainder -= drainNow;
        playerStats.battery = Mathf.Max(0, playerStats.battery - drainNow);

        if (playerStats.battery <= 0)
        {
            playerStats.battery = 0;
            drainRemainder = 0f;
            SetFlashlight(false);
        }
    }

    void TryToggle()
    {
        if (isON)
        {
            SetFlashlight(false);
            return;
        }

        if (playerStats == null)
        {
            SetFlashlight(true);
            return;
        }

        if (playerStats.battery <= 0)
            return;

        SetFlashlight(true);
    }

    void SetFlashlight(bool on)
    {
        isON = on;

        if (ON != null)
            ON.SetActive(on);

        if (OFF != null)
            OFF.SetActive(!on);
    }

    void FixInvalidColliders()
    {
        MeshCollider[] meshColliders = GetComponentsInChildren<MeshCollider>(true);
        for (int i = 0; i < meshColliders.Length; i++)
        {
            MeshCollider meshCol = meshColliders[i];
            Rigidbody rb = meshCol.GetComponent<Rigidbody>();
            if (rb == null)
                rb = meshCol.GetComponentInParent<Rigidbody>();

            if (rb != null && !rb.isKinematic && !meshCol.convex)
            {
                meshCol.enabled = false;
                Destroy(meshCol);
            }
        }
    }
}
