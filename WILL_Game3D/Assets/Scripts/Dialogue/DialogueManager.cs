using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.IO;
using TMPro;
using System.Collections;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private float typingSpeed = 0.05f;

    private Dictionary<string, List<string>> dialogueDatabase = new Dictionary<string, List<string>>();
    private Queue<string> sentenceQueue = new Queue<string>();
    private bool isDialogueActive = false;
    private bool isTyping = false;
    private string currentFullSentence;

    void Awake()
    {
        if (Instance != null && Instance != this)
            return;

        Instance = this;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        LoadDialogueData();
        RebindUiIfNeeded();
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (Instance == this)
            Instance = null;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (Instance != this)
            return;

        StopAllCoroutines();
        isDialogueActive = false;
        isTyping = false;
        sentenceQueue.Clear();
        RebindUiIfNeeded();
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
    }

    void LoadDialogueData()
    {
        string path = Path.Combine(Application.streamingAssetsPath, "Dialogue.json");
        if (!File.Exists(path))
            return;

        string json = File.ReadAllText(path);
        DialogueData data = JsonUtility.FromJson<DialogueData>(json);

        foreach (var entry in data.conversations)
            dialogueDatabase[entry.id] = entry.lines;
    }

    public void StartDialogue(string dialogueId)
    {
        RebindUiIfNeeded();

        if (!dialogueDatabase.ContainsKey(dialogueId) || isDialogueActive)
            return;

        if (dialoguePanel == null || dialogueText == null)
        {
            Debug.LogWarning("DialogueManager: Dialogue UI is missing in this scene.");
            return;
        }

        sentenceQueue.Clear();
        foreach (string line in dialogueDatabase[dialogueId])
            sentenceQueue.Enqueue(line);

        isDialogueActive = true;
        dialoguePanel.SetActive(true);
        DisplayNextSentence();
    }

    public void DisplayNextSentence()
    {
        if (isTyping)
        {
            StopAllCoroutines();
            if (dialogueText != null)
                dialogueText.text = currentFullSentence;
            isTyping = false;
            return;
        }

        if (sentenceQueue.Count == 0)
        {
            EndDialogue();
            return;
        }

        currentFullSentence = sentenceQueue.Dequeue();
        StartCoroutine(TypeSentence(currentFullSentence));
    }

    IEnumerator TypeSentence(string sentence)
    {
        isTyping = true;
        if (dialogueText != null)
            dialogueText.text = "";

        foreach (char letter in sentence.ToCharArray())
        {
            if (dialogueText != null)
                dialogueText.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }
        isTyping = false;
    }

    public void EndDialogue()
    {
        isDialogueActive = false;
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
        if (EventDebugManager.Instance != null)
            EventDebugManager.Instance.TriggerEvent("--- Dialogue Ended ---");
    }

    public bool IsActive() => isDialogueActive;

    void RebindUiIfNeeded()
    {
        if (dialoguePanel != null && dialogueText != null)
            return;

        GameObject panel = GameObject.Find("DialoguePanel");
        if (panel == null)
            return;

        dialoguePanel = panel;
        if (dialogueText == null)
            dialogueText = panel.GetComponentInChildren<TextMeshProUGUI>(true);
    }
}
