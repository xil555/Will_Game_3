using UnityEngine;

/// <summary>
/// Tracks whether the player is hidden. Hide spots and the enemy AI both read this.
/// </summary>
public class PlayerStealth : MonoBehaviour
{
    public static PlayerStealth Instance { get; private set; }

    public bool IsHidden { get; private set; }
    public HideSpot CurrentSpot { get; private set; }

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void EnterHide(HideSpot spot)
    {
        IsHidden = true;
        CurrentSpot = spot;
        if (EventDebugManager.Instance != null)
            EventDebugManager.Instance.TriggerEvent("Hidden. Stay still.");
    }

    public void ExitHide()
    {
        if (!IsHidden)
            return;

        IsHidden = false;
        CurrentSpot = null;
        if (EventDebugManager.Instance != null)
            EventDebugManager.Instance.TriggerEvent("Left hiding spot.");
    }
}
