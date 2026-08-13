using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Fullscreen flash + optional audio when the enemy fully spots the player.
/// Add to the enemy or any scene object. EnemyAI will find it automatically.
/// </summary>
public class SpottedFeedback : MonoBehaviour
{
    public static SpottedFeedback Instance { get; private set; }

    public AudioClip stingClip;
    public Color flashColor = new Color(0.7f, 0.05f, 0.05f, 0.55f);
    public float flashDuration = 0.45f;

    AudioSource audioSource;
    Image flashImage;
    float flashTimer;

    void Awake()
    {
        Instance = this;
        audioSource = GetComponent<AudioSource>();
        EnsureFlashImage();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Update()
    {
        if (flashImage == null || flashTimer <= 0f)
            return;

        flashTimer -= Time.unscaledDeltaTime;
        Color c = flashImage.color;
        c.a = Mathf.Clamp01(flashTimer / flashDuration) * flashColor.a;
        flashImage.color = c;
    }

    public void Play()
    {
        if (stingClip != null)
        {
            if (audioSource != null)
                audioSource.PlayOneShot(stingClip);
            else
                AudioSource.PlayClipAtPoint(stingClip, Camera.main != null ? Camera.main.transform.position : transform.position);
        }

        if (flashImage != null)
        {
            flashImage.color = flashColor;
            flashTimer = flashDuration;
        }
    }

    void EnsureFlashImage()
    {
        Canvas canvas = GetComponentInChildren<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGo = new GameObject("SpottedFlashCanvas");
            canvasGo.transform.SetParent(transform, false);
            canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 250;
            canvasGo.AddComponent<CanvasScaler>();
        }

        Transform existing = canvas.transform.Find("Flash");
        GameObject flashGo = existing != null ? existing.gameObject : new GameObject("Flash");
        if (existing == null)
            flashGo.transform.SetParent(canvas.transform, false);

        flashImage = flashGo.GetComponent<Image>();
        if (flashImage == null)
            flashImage = flashGo.AddComponent<Image>();

        RectTransform rt = flashImage.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        Color c = flashColor;
        c.a = 0f;
        flashImage.color = c;
        flashImage.raycastTarget = false;
    }
}
