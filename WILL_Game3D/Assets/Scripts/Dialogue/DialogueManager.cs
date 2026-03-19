using UnityEngine;
using System.Collections.Generic;
using System.IO;
using TMPro; // Required for TextMeshPro
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

    private void Awake()
    {
        Instance = this;
        LoadDialogueData();
        dialoguePanel.SetActive(false); // Hide UI on start
    }

    void LoadDialogueData()
    {
        string path = Path.Combine(Application.streamingAssetsPath, "Dialogue.json");
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            DialogueData data = JsonUtility.FromJson<DialogueData>(json);

            foreach (var entry in data.conversations)
            {
                dialogueDatabase[entry.id] = entry.lines;
            }
        }
    }

    public void StartDialogue(string dialogueId)
    {
        if (!dialogueDatabase.ContainsKey(dialogueId) || isDialogueActive) return;

        sentenceQueue.Clear();
        foreach (string line in dialogueDatabase[dialogueId])
        {
            sentenceQueue.Enqueue(line);
        }

        isDialogueActive = true;
        dialoguePanel.SetActive(true);
        DisplayNextSentence();
    }

    public void DisplayNextSentence()
    {
        // If still typing, finish the sentence instantly
        if (isTyping)
        {
            StopAllCoroutines();
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
        dialogueText.text = "";
        foreach (char letter in sentence.ToCharArray())
        {
            dialogueText.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }
        isTyping = false;
    }

    public void EndDialogue()
    {
        isDialogueActive = false;
        dialoguePanel.SetActive(false);
        EventDebugManager.Instance.TriggerEvent("--- Dialogue Ended ---");
    }

    public bool IsActive() => isDialogueActive;
}