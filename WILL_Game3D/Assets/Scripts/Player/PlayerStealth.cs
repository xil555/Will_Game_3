using UnityEngine;

/// <summary>
/// Tracks whether the player is hidden. Hide spots and the enemy AI both read this.
/// While hidden, enemy colliders cannot shove the player out of the spot.
/// </summary>
public class PlayerStealth : MonoBehaviour
{
    public static PlayerStealth Instance { get; private set; }

    public bool IsHidden { get; private set; }
    public bool IsHiding => IsHidden;
    public HideSpot CurrentSpot { get; private set; }

    public static event System.Action<bool> OnHiddenChanged;

    Rigidbody rb;
    Collider[] playerColliders;
    bool storedKinematic;
    Vector3 hiddenPosition;

    void Awake()
    {
        Instance = this;
        rb = GetComponent<Rigidbody>();
        playerColliders = GetComponentsInChildren<Collider>(true);
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void LateUpdate()
    {
        if (!IsHidden || rb == null)
            return;

        transform.position = hiddenPosition;
#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = Vector3.zero;
#else
        rb.velocity = Vector3.zero;
#endif
        rb.angularVelocity = Vector3.zero;
        rb.position = hiddenPosition;
    }

    public void EnterHide(HideSpot spot)
    {
        IsHidden = true;
        CurrentSpot = spot;
        hiddenPosition = transform.position;
        FreezeAgainstEnemies(true);
        OnHiddenChanged?.Invoke(true);
        if (EventDebugManager.Instance != null)
            EventDebugManager.Instance.TriggerEvent("Hidden. Stay still.");
    }

    public void ExitHide()
    {
        if (!IsHidden)
            return;

        IsHidden = false;
        CurrentSpot = null;
        FreezeAgainstEnemies(false);
        OnHiddenChanged?.Invoke(false);
        if (EventDebugManager.Instance != null)
            EventDebugManager.Instance.TriggerEvent("Left hiding spot.");
    }

    void FreezeAgainstEnemies(bool hidden)
    {
        if (rb != null)
        {
            if (hidden)
            {
                storedKinematic = rb.isKinematic;
#if UNITY_6000_0_OR_NEWER
                rb.linearVelocity = Vector3.zero;
#else
                rb.velocity = Vector3.zero;
#endif
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
            }
            else
            {
                rb.isKinematic = storedKinematic;
            }
        }

        SetIgnoreEnemyCollision(hidden);
    }

    void SetIgnoreEnemyCollision(bool ignore)
    {
        if (playerColliders == null)
            playerColliders = GetComponentsInChildren<Collider>(true);

#if UNITY_6000_0_OR_NEWER
        EnemyAI[] enemies = FindObjectsByType<EnemyAI>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
#else
        EnemyAI[] enemies = FindObjectsOfType<EnemyAI>();
#endif
        for (int e = 0; e < enemies.Length; e++)
        {
            if (enemies[e] == null)
                continue;

            Collider[] enemyCols = enemies[e].GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < enemyCols.Length; i++)
            {
                if (enemyCols[i] == null || enemyCols[i].isTrigger)
                    continue;

                for (int p = 0; p < playerColliders.Length; p++)
                {
                    if (playerColliders[p] == null || playerColliders[p].isTrigger)
                        continue;
                    Physics.IgnoreCollision(playerColliders[p], enemyCols[i], ignore);
                }
            }
        }
    }
}
