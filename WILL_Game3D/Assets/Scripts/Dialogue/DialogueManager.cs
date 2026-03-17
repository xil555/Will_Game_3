using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    private Dictionary<string, List<string>> dialogueDatabase = new Dictionary<string, List<string>>();
    private Queue<string> sentenceQueue = new Queue<string>();
    private bool isDialogueActive = false;

    private void Awake()
    {
        Instance = this;
        LoadDialogueData();
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
        if (!dialogueDatabase.ContainsKey(dialogueId)) return;

        sentenceQueue.Clear();
        foreach (string line in dialogueDatabase[dialogueId])
        {
            sentenceQueue.Enqueue(line);
        }

        isDialogueActive = true;
        DisplayNextSentence();
    }

    public void DisplayNextSentence()
    {
        if (sentenceQueue.Count == 0)
        {
            EndDialogue();
            return;
        }

        string sentence = sentenceQueue.Dequeue();
        EventDebugManager.Instance.TriggerEvent("Dialogue: " + sentence);
    }

    void EndDialogue()
    {
        isDialogueActive = false;
        EventDebugManager.Instance.TriggerEvent("--- Dialogue Ended ---");
    }

    public bool IsActive() => isDialogueActive;
}
