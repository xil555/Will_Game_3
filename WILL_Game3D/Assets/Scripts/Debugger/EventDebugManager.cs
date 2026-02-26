using UnityEngine;
using System;

public class EventDebugManager : MonoBehaviour
{
    // Singleton instance
    public static EventDebugManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); 
    }

    // General log method
    public void LogEvent(string message)
    {
        Debug.Log("[EVENT] " + message);
    }

    public event Action<string> OnEventLogged;

    public void TriggerEvent(string message)
    {
        LogEvent(message);
        OnEventLogged?.Invoke(message);
    }
}
