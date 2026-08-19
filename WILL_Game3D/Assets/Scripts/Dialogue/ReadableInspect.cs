using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Click a newspaper or photo to zoom it in on a large (not full-screen) panel.
/// No rotation. Close with the Close button, E, or Esc.
/// </summary>
[RequireComponent(typeof(Collider))]
public class ReadableInspect : MonoBehaviour
{
    [Header("Near player")]
    public float interactRange = 2.5f;
    public string playerTag = "Player";

    [Header("UI")]
    public GameObject inspectPanel;
    public Image photoImage;
    public TextMeshProUGUI bodyText;
    public Button closeButton;

    [Header("Content")]
    public Sprite inspectSprite;
    [Tooltip("Leave empty if the sprite already has all the text (newspaper / photo).")]
    [TextArea(2, 8)]
    public string extraCaption;

    [Header("Zoom")]
    public float zoomTime = 0.22f;
    public float startScale = 0.82f;

    [Header("Optional dialogue after closing")]
    public string thoughtDialogueId;

    [Header("Prompt")]
    public string openPrompt = "Press E to pick up newspaper";
    public string closePrompt = "Press Esc or Close";
    public KeyCode openKey = KeyCode.E;

    bool playerNear;
    bool isOpen;
    bool closing;
    Camera mainCam;
    RectTransform panelRect;
    CanvasGroup panelGroup;
    PlayerMovement playerMovement;
    Behaviour lookControl;
    bool pausedMovement;
    bool pausedLook;
    Coroutine zoomRoutine;

    static ReadableInspect currentlyOpen;

    void Start()
    {
        mainCam = Camera.main;

        if (inspectPanel != null)
        {
            inspectPanel.SetActive(false);
            panelRect = inspectPanel.GetComponent<RectTransform>();
            panelGroup = inspectPanel.GetComponent<CanvasGroup>();
            if (panelGroup == null)
                panelGroup = inspectPanel.AddComponent<CanvasGroup>();
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(Close);
        }

        if (photoImage != null)
        {
            photoImage.preserveAspect = true;
            if (inspectSprite != null)
                photoImage.sprite = inspectSprite;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag))
            return;

        playerNear = true;
        playerMovement = other.GetComponent<PlayerMovement>();
        if (playerMovement == null)
            playerMovement = other.GetComponentInParent<PlayerMovement>();

        ResolveLookControl(other.gameObject);

        if (!isOpen)
            InteractPromptUI.Show(this, openPrompt);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag))
            return;

        playerNear = false;
        if (!isOpen)
            InteractPromptUI.Hide(this);
    }

    void Update()
    {
        if (PauseMenu.IsPaused || PlayerDeath.IsDead)
            return;

        if (isOpen)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
                Close();
            return;
        }

        if (!playerNear || closing)
            return;

        if (DialogueManager.Instance != null && DialogueManager.Instance.IsActive())
            return;

        if (currentlyOpen != null)
            return;

        InteractPromptUI.Show(this, openPrompt);

        if (Input.GetKeyDown(openKey))
            Open();
    }

    bool IsMouseOverThis()
    {
        if (mainCam == null)
            mainCam = Camera.main;
        if (mainCam == null)
            return false;

        Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (!Physics.Raycast(ray, out hit, interactRange, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            return false;

        return hit.collider != null && (hit.collider.transform == transform || hit.collider.transform.IsChildOf(transform));
    }

    void Open()
    {
        if (inspectPanel == null)
        {
            Debug.LogWarning(name + ": Assign Inspect Panel.");
            return;
        }

        if (currentlyOpen != null && currentlyOpen != this)
            currentlyOpen.CloseImmediate();

        isOpen = true;
        closing = false;
        currentlyOpen = this;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        PausePlayer();

        if (photoImage != null)
        {
            photoImage.gameObject.SetActive(inspectSprite != null);
            if (inspectSprite != null)
            {
                photoImage.sprite = inspectSprite;
                photoImage.preserveAspect = true;
            }
        }

        if (bodyText != null)
        {
            bool showCaption = !string.IsNullOrEmpty(extraCaption);
            bodyText.gameObject.SetActive(showCaption);
            if (showCaption)
                bodyText.text = extraCaption;
        }

        inspectPanel.SetActive(true);
        InteractPromptUI.Show(this, closePrompt);

        if (zoomRoutine != null)
            StopCoroutine(zoomRoutine);
        zoomRoutine = StartCoroutine(ZoomRoutine(true));
    }

    public void Close()
    {
        if (!isOpen || closing)
            return;

        closing = true;
        if (zoomRoutine != null)
            StopCoroutine(zoomRoutine);
        zoomRoutine = StartCoroutine(CloseRoutine());
    }

    IEnumerator CloseRoutine()
    {
        yield return ZoomRoutine(false);

        CloseImmediate();

        if (!string.IsNullOrEmpty(thoughtDialogueId) && DialogueManager.Instance != null)
            DialogueManager.Instance.StartDialogue(thoughtDialogueId);
    }

    void CloseImmediate()
    {
        isOpen = false;
        closing = false;

        if (currentlyOpen == this)
            currentlyOpen = null;

        if (inspectPanel != null)
            inspectPanel.SetActive(false);

        InteractPromptUI.Hide(this);
        RestorePlayer();
    }

    IEnumerator ZoomRoutine(bool zoomIn)
    {
        if (panelRect == null && inspectPanel != null)
            panelRect = inspectPanel.GetComponent<RectTransform>();

        float from = zoomIn ? startScale : 1f;
        float to = zoomIn ? 1f : startScale;
        float fromAlpha = zoomIn ? 0f : 1f;
        float toAlpha = zoomIn ? 1f : 0f;
        float t = 0f;

        if (panelRect != null)
            panelRect.localScale = Vector3.one * from;
        if (panelGroup != null)
            panelGroup.alpha = fromAlpha;

        while (t < zoomTime)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / zoomTime);
            p = p * p * (3f - 2f * p);

            if (panelRect != null)
                panelRect.localScale = Vector3.one * Mathf.Lerp(from, to, p);
            if (panelGroup != null)
                panelGroup.alpha = Mathf.Lerp(fromAlpha, toAlpha, p);

            yield return null;
        }

        if (panelRect != null)
            panelRect.localScale = Vector3.one * to;
        if (panelGroup != null)
            panelGroup.alpha = toAlpha;

        zoomRoutine = null;
    }

    void PausePlayer()
    {
        if (playerMovement != null && playerMovement.enabled)
        {
            playerMovement.enabled = false;
            pausedMovement = true;
        }

        if (lookControl != null && lookControl.enabled)
        {
            lookControl.enabled = false;
            pausedLook = true;
        }
    }

    void RestorePlayer()
    {
        if (pausedMovement && playerMovement != null)
        {
            playerMovement.enabled = true;
            pausedMovement = false;
        }

        if (pausedLook && lookControl != null)
        {
            lookControl.enabled = true;
            pausedLook = false;
        }
    }

    void ResolveLookControl(GameObject playerObject)
    {
        if (lookControl != null)
            return;

        Behaviour[] behaviours = playerObject.GetComponentsInChildren<Behaviour>(true);
        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] == null)
                continue;
            string typeName = behaviours[i].GetType().Name;
            if (typeName == "PlayerLook" || typeName == "MouseLook" || typeName == "CameraLook")
            {
                lookControl = behaviours[i];
                return;
            }
        }
    }

    void OnDestroy()
    {
        if (currentlyOpen == this)
            currentlyOpen = null;
        InteractPromptUI.Hide(this);
    }
}
