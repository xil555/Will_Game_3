using UnityEngine;
using System.Collections.Generic;
using System.IO;

// 1. DATA MODELS (The blueprints for your JSON)
[System.Serializable]
public class DialogueEntry
{
    public string id;
    public List<string> lines;
}

[System.Serializable]
public class DialogueData
{
    public List<DialogueEntry> conversations;
}

// 2. THE MAIN SYSTEM
public class DialogueSystem : MonoBehaviour
{
    public static DialogueSystem Instance { get; private set; }

    private Dictionary<string, List<string>> _dialogueDatabase = new Dictionary<string, List<string>>();
    private Queue<string> _currentLines = new Queue<string>();
    private bool _isConversationActive = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        LoadDialogueFromJson();
    }

    private void LoadDialogueFromJson()
    {
        // This looks for a file named Dialogue.json in your StreamingAssets folder
        string path = Path.Combine(Application.streamingAssetsPath, "Dialogue.json");

        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            DialogueData data = JsonUtility.FromJson<DialogueData>(json);

            foreach (var entry in data.conversations)
            {
                _dialogueDatabase[entry.id] = entry.lines;
            }
            Debug.Log("Dialogue System: Loaded " + _dialogueDatabase.Count + " conversations.");
        }
        else
        {
            Debug.LogError("Dialogue System: JSON file missing at " + path);
        }
    }

    public void StartDialogue(string conversationId)
    {
        if (!_dialogueDatabase.ContainsKey(conversationId)) return;

        _currentLines.Clear();
        foreach (string line in _dialogueDatabase[conversationId])
        {
            _currentLines.Enqueue(line);
        }

        _isConversationActive = true;
        AdvanceDialogue();
    }

    public void AdvanceDialogue()
    {
        if (_currentLines.Count > 0)
        {
            string nextLine = _currentLines.Dequeue();
            // Hooking into your existing EventDebugManager
            EventDebugManager.Instance.TriggerEvent(nextLine);
        }
        else
        {
            _isConversationActive = false;
            EventDebugManager.Instance.TriggerEvent("--- End of Dialogue ---");
        }
    }

    public bool IsInConversation() => _isConversationActive;
}