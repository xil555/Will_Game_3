using UnityEngine;

/// <summary>
/// Knockable bait. Player presses E nearby to throw/knock it, which emits a loud noise
/// the enemy will investigate.
/// </summary>
[RequireComponent(typeof(Collider))]
public class NoiseBait : MonoBehaviour
{
    public float loudness = 1f;
    public float interactRange = 2.5f;
    public bool destroyOnUse = true;
    public AudioClip knockClip;
    public string prompt = "Press E to throw / make noise";

    bool used;
    AudioSource audioSource;
    Transform player;

    void Reset()
    {
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        if (used || PauseMenu.IsPaused)
            return;

        if (player == null)
        {
            GameObject found = GameObject.FindGameObjectWithTag("Player");
            if (found != null)
                player = found.transform;
            return;
        }

        if (PlayerStealth.Instance != null && PlayerStealth.Instance.IsHidden)
            return;

        if (DialogueManager.Instance != null && DialogueManager.Instance.IsActive())
            return;

        float dist = Vector3.Distance(player.position, transform.position);
        if (dist > interactRange)
            return;

        if (Input.GetKeyDown(KeyCode.E))
            Use();
    }

    void Use()
    {
        used = true;
        NoiseEvent.Emit(transform.position, loudness);

        if (EventDebugManager.Instance != null)
            EventDebugManager.Instance.TriggerEvent("Noise bait thrown!");

        if (knockClip != null)
        {
            if (audioSource != null)
                audioSource.PlayOneShot(knockClip);
            else
                AudioSource.PlayClipAtPoint(knockClip, transform.position);
        }

        if (destroyOnUse)
            Destroy(gameObject, knockClip != null ? knockClip.length : 0.05f);
        else
            enabled = false;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}
