using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

public class KidDialogueTrigger : MonoBehaviour
{
    [Header("UI")]
    public GameObject dialoguePanel;
    public TMP_Text nameText;
    public TMP_Text dialogueText;
    public GameObject objectivePanel;

    [Header("Objective")]
    [Tooltip("Completed when this talk ends. Does not stop the kid from talking.")]
    public string objectiveId = "MediumKid";

    [Header("Cameras")]
    public Camera cutsceneCamera;

    [Header("Dialogue")]
    public string kidName = "Kid";
    [TextArea]
    public string[] lines =
    {
        "H-hi... I'm scared.",
        "The old man just took my dog into the house along the road.",
        "Please... can you rescue it for me?",
        "I'll give you batteries in return."
    };

    Camera mainCamera;
    MonoBehaviour playerMovement;
    MonoBehaviour playerCam;
    bool inCutscene;
    int lineIndex;

    void Start()
    {
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
        if (objectivePanel != null)
            objectivePanel.SetActive(false);
        if (cutsceneCamera != null)
            cutsceneCamera.enabled = false;

        // Never leave the game frozen if play mode stopped mid-cutscene last time
        if (Time.timeScale < 1f)
            Time.timeScale = 1f;
    }

    void OnTriggerEnter(Collider other)
    {
        if (inCutscene) return;
        if (!other.CompareTag("Player")) return;
        StartCutscene(other.gameObject);
    }

    void Update()
    {
        if (!inCutscene) return;

        bool pressedE =
            (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            || Input.GetKeyDown(KeyCode.E);

        if (pressedE)
            AdvanceDialogue();
    }

    void StartCutscene(GameObject player)
    {
        inCutscene = true;
        lineIndex = 0;

        CachePlayerControls(player);
        CacheMainCamera();

        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (playerMovement != null)
            playerMovement.enabled = false;
        if (playerCam != null)
            playerCam.enabled = false;

        if (mainCamera != null)
            mainCamera.enabled = false;
        if (cutsceneCamera != null)
            cutsceneCamera.enabled = true;

        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);
        if (nameText != null)
            nameText.text = kidName;

        ShowCurrentLine();
    }

    void AdvanceDialogue()
    {
        lineIndex++;
        if (lineIndex >= lines.Length)
        {
            EndCutscene();
            return;
        }
        ShowCurrentLine();
    }

    void ShowCurrentLine()
    {
        if (dialogueText != null)
            dialogueText.text = lines[lineIndex];
    }

    void EndCutscene()
    {
        inCutscene = false;

        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        if (cutsceneCamera != null)
            cutsceneCamera.enabled = false;

        CacheMainCamera();
        if (mainCamera != null)
            mainCamera.enabled = true;

        if (playerMovement != null)
            playerMovement.enabled = true;
        if (playerCam != null)
            playerCam.enabled = true;

        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (objectivePanel != null)
            objectivePanel.SetActive(true);

        Collider col = GetComponent<Collider>();
        if (col != null)
            col.enabled = false;

        if (ObjectiveManager.Instance != null && !string.IsNullOrEmpty(objectiveId))
            ObjectiveManager.Instance.ReportNpcTalked(objectiveId);
    }

    void CachePlayerControls(GameObject player)
    {
        if (player == null)
            return;

        playerMovement = player.GetComponent<PlayerMovement>();
        if (playerMovement == null)
            playerMovement = player.GetComponentInParent<PlayerMovement>();

        playerCam = player.GetComponent<PlayerLook>();
        if (playerCam == null)
            playerCam = player.GetComponentInChildren<PlayerLook>();
    }

    void CacheMainCamera()
    {
        if (mainCamera != null) return;

        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            GameObject camObj = GameObject.FindGameObjectWithTag("MainCamera");
            if (camObj != null)
                mainCamera = camObj.GetComponent<Camera>();
        }
    }
}
