using System;
using System.Collections.Generic;
using UnityEngine;

public enum ObjectiveType
{
    CollectBatteries,
    CollectKeys,
    ReachTrigger
}

[Serializable]
public class ObjectiveData
{
    [Tooltip("Shown on screen. For collect types use {current} and {target}, e.g. Collect batteries: {current}/{target}")]
    public string displayText = "New Objective";

    public ObjectiveType type = ObjectiveType.ReachTrigger;

    [Tooltip("How many pickups are needed. Ignored for ReachTrigger.")]
    public int targetAmount = 1;

    [Tooltip("Must match the Trigger Id on an ObjectiveTrigger in the scene. Only used for ReachTrigger.")]
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
    public string allCompleteText = "All objectives complete!";

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
        NotifyChanged();
    }

    public string GetDisplayText()
    {
        if (allComplete || objectives == null || objectives.Count == 0)
            return allCompleteText;

        ObjectiveData objective = objectives[currentIndex];
        string text = objective.displayText;

        if (objective.type == ObjectiveType.CollectBatteries || objective.type == ObjectiveType.CollectKeys)
        {
            string progress = currentProgress + "/" + objective.targetAmount;

            if (text.Contains("{current}") || text.Contains("{target}"))
            {
                text = text
                    .Replace("{current}", currentProgress.ToString())
                    .Replace("{target}", objective.targetAmount.ToString());
            }
            else
            {
                // Always show progress even if the designer didn't add placeholders
                text = text + " (" + progress + ")";
            }
        }

        return text;
    }

    public void ReportBatteryCollected(int amount = 1)
    {
        if (!HasActiveObjective) return;
        if (CurrentObjective.type != ObjectiveType.CollectBatteries) return;

        AddProgress(amount);
    }

    public void ReportKeyCollected(int amount = 1)
    {
        if (!HasActiveObjective) return;
        if (CurrentObjective.type != ObjectiveType.CollectKeys) return;

        AddProgress(amount);
    }

    public void ReportTriggerReached(string triggerId)
    {
        if (!HasActiveObjective) return;
        if (CurrentObjective.type != ObjectiveType.ReachTrigger) return;
        if (string.IsNullOrEmpty(triggerId)) return;
        if (!string.Equals(CurrentObjective.triggerId, triggerId, StringComparison.OrdinalIgnoreCase))
            return;

        CompleteCurrentObjective();
    }

    void AddProgress(int amount)
    {
        currentProgress += amount;
        NotifyChanged();

        if (currentProgress >= CurrentObjective.targetAmount)
            CompleteCurrentObjective();
    }

    void CompleteCurrentObjective()
    {
        if (EventDebugManager.Instance != null)
            EventDebugManager.Instance.TriggerEvent("Objective complete: " + CurrentObjective.displayText);

        currentIndex++;
        currentProgress = 0;

        if (currentIndex >= objectives.Count)
            allComplete = true;

        NotifyChanged();
    }

    void NotifyChanged()
    {
        OnObjectiveChanged?.Invoke();
    }
}
