using UnityEngine;

/// <summary>
/// Place this on an empty GameObject in any game scene (Easy, Medium, Hard).
/// Position/rotate that GameObject where you want the player to appear.
/// Assign the player prefab in the Inspector for scenes that may be loaded directly
/// (e.g. Hard chosen from the lobby without playing Easy first).
/// </summary>
public class PlayerSpawner : MonoBehaviour
{
    [Header("Player Prefab")]
    [Tooltip("The player prefab to spawn if no persistent player exists yet.")]
    public GameObject playerPrefab;

    void Start()
    {
        GameObject player = GameObject.FindWithTag("Player");

        if (player != null)
        {
            // A player persisted from a previous scene — teleport it here
            TeleportPlayer(player);
        }
        else
        {
            // No player yet (e.g. jumped straight to this scene from the lobby)
            if (playerPrefab != null)
            {
                Instantiate(playerPrefab, transform.position, transform.rotation);
            }
            else
            {
                Debug.LogWarning($"[PlayerSpawner] No player found in scene and no prefab assigned on {gameObject.name}!");
            }
        }
    }

    void TeleportPlayer(GameObject player)
    {
        // Disable the Rigidbody briefly so it doesn't fight the teleport
        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb != null)
        {
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = Vector3.zero;
#else
            rb.velocity = Vector3.zero;
#endif
            rb.angularVelocity = Vector3.zero;
        }

        player.transform.position = transform.position;
        player.transform.rotation = transform.rotation;
    }

    // Draw a visible gizmo in the editor so you can see where the spawn point is
    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, transform.forward * 1.5f);
    }
}
