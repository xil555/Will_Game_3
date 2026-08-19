using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shows phone flashlight battery on a UI Slider.
/// Attach to a Canvas object. Assign the Slider and PlayerStats.
/// </summary>
public class BatteryUI : MonoBehaviour
{
    [Header("References")]
    public Slider batterySlider;
    public PlayerStats playerStats;
    public Flashlight flashlight;

    [Header("Battery")]
    public int maxBattery = 100;

    [Header("Optional warning")]
    public Text warningText;
    [Tooltip("Show warning when battery is at or below this amount.")]
    public int lowBatteryThreshold = 20;
    public string lowBatteryMessage = "Battery low — find a power bank";
    public string emptyBatteryMessage = "Battery dead — find a power bank";

    [Header("Optional fill colour")]
    public Image fillImage;
    public Color normalColor = new Color(0.3f, 0.85f, 0.4f);
    public Color lowColor = new Color(1f, 0.75f, 0.2f);
    public Color emptyColor = new Color(0.9f, 0.25f, 0.25f);

    void Start()
    {
        if (playerStats == null)
            playerStats = Object.FindAnyObjectByType<PlayerStats>();

        if (flashlight == null)
            flashlight = Object.FindAnyObjectByType<Flashlight>();

        if (batterySlider == null)
            batterySlider = GetComponentInChildren<Slider>();

        if (batterySlider != null)
        {
            batterySlider.minValue = 0;
            batterySlider.maxValue = maxBattery;
            batterySlider.interactable = false;
        }

        RefreshUI();
    }

    void Update()
    {
        RefreshUI();
    }

    void RefreshUI()
    {
        if (playerStats == null || batterySlider == null)
            return;

        int battery = Mathf.Clamp(playerStats.battery, 0, maxBattery);
        batterySlider.value = battery;

        if (fillImage != null)
        {
            if (battery <= 0)
                fillImage.color = emptyColor;
            else if (battery <= lowBatteryThreshold)
                fillImage.color = lowColor;
            else
                fillImage.color = normalColor;
        }

        if (warningText == null)
            return;

        if (battery <= 0)
        {
            warningText.gameObject.SetActive(true);
            warningText.text = emptyBatteryMessage;
        }
        else if (battery <= lowBatteryThreshold)
        {
            warningText.gameObject.SetActive(true);
            warningText.text = lowBatteryMessage;
        }
        else
        {
            warningText.gameObject.SetActive(false);
        }
    }
}
