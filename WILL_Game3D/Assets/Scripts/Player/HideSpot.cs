using UnityEngine;

/// <summary>
/// Place on a trigger collider (closet, dumpster, bushes). Player presses E to hide.
/// Hidden players are invisible to the enemy's vision cone.
/// </summary>
[RequireComponent(typeof(Collider))]
public class HideSpot : MonoBehaviour
{
    [Tooltip("Optional point the camera/player snaps toward while hidden.")]
    public Transform hidePoint;
    public string enterPrompt = "Press E to hide";
    public string exitPrompt = "Press E to leave";

    bool playerInside;
    Transform playerTransform;
    Vector3 enterPosition;
    Quaternion enterRotation;

    void Reset()
    {
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInside = true;
        playerTransform = other.transform;
        if (EventDebugManager.Instance != null)
            EventDebugManager.Instance.TriggerEvent(enterPrompt);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInside = false;
        if (PlayerStealth.Instance != null && PlayerStealth.Instance.CurrentSpot == this)
            Leave();
    }

    void Update()
    {
        if (PauseMenu.IsPaused)
            return;

        PlayerStealth stealth = PlayerStealth.Instance;
        if (stealth == null)
            return;

        bool hidingHere = stealth.IsHidden && stealth.CurrentSpot == this;

        if (hidingHere && Input.GetKeyDown(KeyCode.E))
        {
            Leave();
            return;
        }

        if (playerInside && !stealth.IsHidden && Input.GetKeyDown(KeyCode.E))
        {
            if (DialogueManager.Instance != null && DialogueManager.Instance.IsActive())
                return;
            Hide();
        }
    }

    void Hide()
    {
        if (playerTransform == null)
            return;

        EnsureStealth(playerTransform.gameObject);

        enterPosition = playerTransform.position;
        enterRotation = playerTransform.rotation;

        if (hidePoint != null)
        {
            playerTransform.position = hidePoint.position;
            playerTransform.rotation = hidePoint.rotation;
        }

        PlayerStealth.Instance.EnterHide(this);
    }

    void Leave()
    {
        if (playerTransform != null)
        {
            playerTransform.position = enterPosition;
            playerTransform.rotation = enterRotation;
        }

        if (PlayerStealth.Instance != null)
            PlayerStealth.Instance.ExitHide();
    }

    static void EnsureStealth(GameObject player)
    {
        if (player.GetComponent<PlayerStealth>() == null)
            player.AddComponent<PlayerStealth>();
    }
}
