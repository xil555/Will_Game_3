using System;
using System.Collections.Generic;
using UnityEngine;

public enum ObjectiveType
{
    CollectBatteries,
    CollectKeys,
    ReachTrigger,
    TalkToNpc
}

[Serializable]
public class ObjectiveData
{
    [Tooltip("Shown on screen. Use {current} and {target} for collect types, e.g. Objective: Collect Batteries ({current}/{target})")]
    public string displayText = "Objective: Collect Batteries ({current}/{target})";

    public ObjectiveType type = ObjectiveType.CollectBatteries;

    [Tooltip("How many pickups are needed. Ignored for ReachTrigger.")]
    [Min(1)]
    public int targetAmount = 3;

    [Tooltip("For ReachTrigger: match ObjectiveTrigger. For TalkToNpc: match the NPC's npcID (e.g. TutorialNPC).")]
    public string triggerId = "";
}

/// <summary>
/// Put this in each game scene (Tutorial, Easy, Hard, etc.) and fill the Objectives list
/// in order. Completing one advances to the next automatically.
/// </summary>
public class ObjectiveManager : MonoBehaviour
{
    public static ObjectiveManager Instance { get; private set; }

    [Header("Objective Chain")]
    [Tooltip("Objectives run in order from top to bottom.")]
    public List<ObjectiveData> objectives = new List<ObjectiveData>();

    [Header("Completion")]
    public string allCompleteText = "Objective complete!";
    public string batteryObjectiveFormat = "Objective: Collect Batteries ({current}/{target})";
    public string keyObjectiveFormat = "Objective: Collect Keys ({current}/{target})";

    [Header("Debug")]
    public bool debugLogs = true;

    public event Action OnObjectiveChanged;

    private int currentIndex;
    private int currentProgress;
    private bool allComplete;

    public bool HasActiveObjective => !allComplete && currentIndex < objectives.Count;
    public ObjectiveData CurrentObjective => HasActiveObjective ? objectives[currentIndex] : null;
    public int CurrentProgress => currentProgress;
    public bool AllComplete => allComplete;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Log("ObjectiveManager is running on " + gameObject.name);
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Start()
    {
        currentIndex = 0;
        currentProgress = 0;
        allComplete = objectives == null || objectives.Count == 0;

        if (allComplete)
            Log("No objectives in the list — UI will show the complete text.");
        else
            Log("Started. Objective 0/" + objectives.Count + " -> " + GetDisplayText());

        NotifyChanged();
    }

    public string GetDisplayText()
    {
        if (allComplete || objectives == null || objectives.Count == 0)
            return allCompleteText;

        ObjectiveData objective = objectives[currentIndex];
        return FormatObjectiveText(objective, currentProgress);
    }

    string FormatObjectiveText(ObjectiveData objective, int progress)
    {
        int target = Mathf.Max(1, objective.targetAmount);
        int current = Mathf.Clamp(progress, 0, target);
        string template = objective.displayText;

        bool isCollect = objective.type == ObjectiveType.CollectBatteries
                      || objective.type == ObjectiveType.CollectKeys;

        if (isCollect && (string.IsNullOrWhiteSpace(template) || !HasProgressPlaceholders(template)))
        {
            string fallback = objective.type == ObjectiveType.CollectKeys
                ? keyObjectiveFormat
                : batteryObjectiveFormat;

            if (string.IsNullOrWhiteSpace(fallback) || !HasProgressPlaceholders(fallback))
            {
                fallback = objective.type == ObjectiveType.CollectKeys
                    ? "Objective: Collect Keys ({current}/{target})"
                    : "Objective: Collect Batteries ({current}/{target})";
            }

            template = fallback;
        }

        if (isCollect)
        {
            return template
                .Replace("{current}", current.ToString())
                .Replace("{target}", target.ToString());
        }

        return template;
    }

    static bool HasProgressPlaceholders(string text)
    {
        return text.Contains("{current}") || text.Contains("{target}");
    }

    public void ReportBatteryCollected(int amount = 1)
    {
        Log("Battery collected reported.");

        if (!HasActiveObjective)
        {
            Log("Ignored battery — no active objective.");
            return;
        }

        if (CurrentObjective.type != ObjectiveType.CollectBatteries)
        {
            Log("Ignored battery — current objective is " + CurrentObjective.type + ", not CollectBatteries.");
            return;
        }

        AddProgress(amount);
    }

    public void ReportKeyCollected(int amount = 1)
    {
        Log("Key collected reported.");

        if (!HasActiveObjective)
        {
            Log("Ignored key — no active objective.");
            return;
        }

        if (CurrentObjective.type != ObjectiveType.CollectKeys)
        {
            Log("Ignored key — current objective is " + CurrentObjective.type + ", not CollectKeys.");
            return;
        }

        AddProgress(amount);
    }

    public void ReportTriggerReached(string triggerId)
    {
        Log("Trigger reached: " + triggerId);

        if (!HasActiveObjective) return;
        if (CurrentObjective.type != ObjectiveType.ReachTrigger) return;
        if (string.IsNullOrEmpty(triggerId)) return;
        if (!string.Equals(CurrentObjective.triggerId, triggerId, StringComparison.OrdinalIgnoreCase))
            return;

        CompleteCurrentObjective();
    }

    public void ReportNpcTalked(string npcId)
    {
        Log("NPC talked reported: " + npcId);

        if (!HasActiveObjective)
        {
            Log("Ignored NPC talk — no active objective.");
            return;
        }

        if (CurrentObjective.type != ObjectiveType.TalkToNpc)
        {
            Log("Ignored NPC talk — current objective is " + CurrentObjective.type + ", not TalkToNpc.");
            return;
        }

        if (string.IsNullOrEmpty(npcId)) return;

        if (!string.IsNullOrEmpty(CurrentObjective.triggerId)
            && !string.Equals(CurrentObjective.triggerId, npcId, StringComparison.OrdinalIgnoreCase))
        {
            Log("Ignored NPC talk — expected Trigger Id '" + CurrentObjective.triggerId + "' but got '" + npcId + "'.");
            return;
        }

        CompleteCurrentObjective();
    }

    void AddProgress(int amount)
    {
        currentProgress += amount;
        Log("Progress " + currentProgress + "/" + CurrentObjective.targetAmount + " on '" + GetDisplayText() + "'");
        NotifyChanged();

        if (currentProgress >= CurrentObjective.targetAmount)
            CompleteCurrentObjective();
    }

    void CompleteCurrentObjective()
    {
        Log("Completed: " + CurrentObjective.displayText);

        if (EventDebugManager.Instance != null)
            EventDebugManager.Instance.TriggerEvent("Objective complete: " + CurrentObjective.displayText);

        currentIndex++;
        currentProgress = 0;

        if (currentIndex >= objectives.Count)
        {
            allComplete = true;
            Log("All objectives complete.");
        }
        else
        {
            Log("Next objective " + currentIndex + "/" + objectives.Count + " -> " + GetDisplayText());
        }

        NotifyChanged();
    }

    void NotifyChanged()
    {
        OnObjectiveChanged?.Invoke();
    }

    void Log(string message)
    {
        if (!debugLogs) return;
        Debug.Log("[Objective] " + message, this);
    }
}
