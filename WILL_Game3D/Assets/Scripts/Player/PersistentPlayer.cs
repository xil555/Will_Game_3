using UnityEngine;
using UnityEngine.SceneManagement;

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

    /// <summary>
    /// Drop persistence so the player is destroyed with the current scene
    /// (Game Over / Main Menu) instead of leaking into UI scenes.
    /// </summary>
    public static void Release()
    {
        if (Instance == null)
            return;

        GameObject go = Instance.gameObject;
        Instance = null;

        Scene scene = SceneManager.GetActiveScene();
        if (go.scene != scene)
            SceneManager.MoveGameObjectToScene(go, scene);
    }
}
