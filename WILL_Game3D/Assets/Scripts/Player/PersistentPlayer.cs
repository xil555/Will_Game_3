using UnityEngine;

/// <summary>
/// Add this to the root of the player prefab.
/// Keeps the player alive across scene loads (DontDestroyOnLoad) and prevents duplicates.
/// PlayerSpawner will teleport this object to the correct spawn point each scene.
/// </summary>
public class PersistentPlayer : MonoBehaviour
{
    public static PersistentPlayer Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // A player already persisted from a previous scene — destroy this duplicate
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
}
