using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Put this on the home doorway trigger.
/// Screen fades to black, then the main menu loads.
/// </summary>
[RequireComponent(typeof(Collider))]
public class GoHomeTrigger : MonoBehaviour
{
    public string menuSceneName = "MainMenu";
    public float fadeTime = 1.2f;
    public string playerTag = "Player";

    bool started;
    Image fadeImage;

    void Reset()
    {
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void Awake()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
            col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (started || !other.CompareTag(playerTag))
            return;

        started = true;
        StartCoroutine(FadeAndLoad());
    }

    IEnumerator FadeAndLoad()
    {
        fadeImage = CreateFade();
        Color color = Color.black;
        color.a = 0f;
        fadeImage.color = color;

        float t = 0f;
        while (t < fadeTime)
        {
            t += Time.unscaledDeltaTime;
            color.a = Mathf.Clamp01(t / fadeTime);
            fadeImage.color = color;
            yield return null;
        }

        color.a = 1f;
        fadeImage.color = color;

        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SceneManager.LoadScene(menuSceneName);
    }

    Image CreateFade()
    {
        GameObject canvasObject = new GameObject("GoHomeFade");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;
        canvasObject.AddComponent<CanvasScaler>();

        GameObject imageObject = new GameObject("Black");
        imageObject.transform.SetParent(canvasObject.transform, false);

        Image image = imageObject.AddComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0f);
        image.raycastTarget = true;

        RectTransform rect = image.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        return image;
    }
}
