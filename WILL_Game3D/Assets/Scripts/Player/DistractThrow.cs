using UnityEngine;

/// <summary>
/// Press G to toss a distraction noise in the look direction.
/// Use this when you do not have a physical can placed in the scene.
/// </summary>
public class DistractThrow : MonoBehaviour
{
    public KeyCode throwKey = KeyCode.G;
    public float throwDistance = 10f;
    public float loudness = 0.9f;
    public float cooldown = 4f;
    public LayerMask hitMask = ~0;
    public AudioClip throwClip;

    float cooldownTimer;

    void Update()
    {
        if (PauseMenu.IsPaused)
            return;
        if (PlayerStealth.Instance != null && PlayerStealth.Instance.IsHidden)
            return;
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsActive())
            return;

        cooldownTimer -= Time.deltaTime;
        if (!Input.GetKeyDown(throwKey) || cooldownTimer > 0f)
            return;

        Vector3 origin = transform.position + Vector3.up * 1.2f;
        Vector3 dir = transform.forward;
        Camera cam = Camera.main;
        if (cam != null)
            dir = cam.transform.forward;

        Vector3 point = origin + dir * throwDistance;
        if (Physics.Raycast(origin, dir, out RaycastHit hit, throwDistance, hitMask, QueryTriggerInteraction.Ignore))
            point = hit.point;

        NoiseEvent.Emit(point, loudness);
        cooldownTimer = cooldown;

        if (throwClip != null)
            AudioSource.PlayClipAtPoint(throwClip, point);

        if (EventDebugManager.Instance != null)
            EventDebugManager.Instance.TriggerEvent("Distraction thrown");
    }
}
