using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attach to a UI Slider in your Canvas to display the player's stamina.
/// Set the Slider's Min = 0, Max = 1, and disable the Handle Slide Area if you don't want a drag handle.
/// Assign the Fill Area > Fill Image to get the colour transition.
/// </summary>
public class StaminaBar : MonoBehaviour
{
    [Header("References")]
    public Slider staminaSlider;

    [Header("Colour Transition")]
    public Image fillImage;
    public Color fullColor = new Color(0.2f, 0.85f, 0.2f);
    public Color emptyColor = new Color(0.85f, 0.15f, 0.15f);

    [Header("Settings")]
    [Tooltip("How quickly the bar visually catches up to the real value.")]
    public float smoothSpeed = 8f;
    [Tooltip("Hide the bar when stamina is full.")]
    public bool hideWhenFull = true;
    public float hideDelay = 2f;

    private PlayerStats playerStats;
    private CanvasGroup canvasGroup;
    private float hideTimer;

    void Start()
    {
        playerStats = FindObjectOfType<PlayerStats>();
        canvasGroup = GetComponent<CanvasGroup>();

        if (staminaSlider != null)
        {
            staminaSlider.minValue = 0f;
            staminaSlider.maxValue = 1f;
            staminaSlider.value = 1f;
        }
    }

    void Update()
    {
        if (playerStats == null)
        {
            playerStats = FindObjectOfType<PlayerStats>();
            return;
        }

        float ratio = playerStats.stamina / playerStats.maxStamina;

        if (staminaSlider != null)
            staminaSlider.value = Mathf.Lerp(staminaSlider.value, ratio, smoothSpeed * Time.deltaTime);

        if (fillImage != null)
            fillImage.color = Color.Lerp(emptyColor, fullColor, ratio);

        HandleVisibility(ratio);
    }

    void HandleVisibility(float ratio)
    {
        if (!hideWhenFull || canvasGroup == null) return;

        if (ratio < 0.999f)
        {
            hideTimer = hideDelay;
            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, 1f, Time.deltaTime * 5f);
        }
        else
        {
            hideTimer -= Time.deltaTime;
            if (hideTimer <= 0f)
                canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, 0f, Time.deltaTime * 2f);
        }
    }
}
