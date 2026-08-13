using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerDeath : MonoBehaviour
{
    public static bool IsDead { get; private set; }
    public static string LastGameplayScene { get; private set; }

    [SerializeField] private string gameOverSceneName = "Game Over";
    [SerializeField] private float caughtHoldTime = 1.15f;
    [SerializeField] private AudioClip caughtClip;

    Transform lookTarget;
    Transform playerCamera;
    bool killing;

    void Awake()
    {
        IsDead = false;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Police"))
            Kill(other.transform);
    }

    public void Kill()
    {
        Kill(null);
    }

    public void Kill(Transform killer)
    {
        if (killing || IsDead)
            return;

        killing = true;
        IsDead = true;
        lookTarget = killer;
        LastGameplayScene = SceneManager.GetActiveScene().name;

        if (PauseMenu.IsPaused)
            Time.timeScale = 1f;

        ResolveCamera();
        FreezeBody();
        PlayCaughtAudio();

        if (PlayerStealth.Instance != null)
            PlayerStealth.Instance.ExitHide();

        StartCoroutine(CaughtThenGameOver());
    }

    void LateUpdate()
    {
        if (!IsDead || lookTarget == null || playerCamera == null)
            return;

        Vector3 target = lookTarget.position + Vector3.up * 1.5f;
        Vector3 toTarget = target - playerCamera.position;
        if (toTarget.sqrMagnitude < 0.001f)
            return;

        Quaternion desired = Quaternion.LookRotation(toTarget);
        Vector3 euler = desired.eulerAngles;

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            Quaternion.Euler(0f, euler.y, 0f),
            8f * Time.unscaledDeltaTime);

        playerCamera.localRotation = Quaternion.Slerp(
            playerCamera.localRotation,
            Quaternion.Euler(NormalizePitch(euler.x), 0f, 0f),
            8f * Time.unscaledDeltaTime);
    }

    IEnumerator CaughtThenGameOver()
    {
        yield return new WaitForSecondsRealtime(caughtHoldTime);

        PersistentPlayer.Release();
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SceneManager.LoadScene(gameOverSceneName);
    }

    void FreezeBody()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
            return;

#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = Vector3.zero;
#else
        rb.velocity = Vector3.zero;
#endif
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;
    }

    void ResolveCamera()
    {
        PlayerLook look = GetComponent<PlayerLook>();
        if (look != null && look.playerCamera != null)
        {
            playerCamera = look.playerCamera;
            return;
        }

        PlayerMovement move = GetComponent<PlayerMovement>();
        if (move != null && move.playerCamera != null)
            playerCamera = move.playerCamera;
        else if (Camera.main != null)
            playerCamera = Camera.main.transform;
    }

    void PlayCaughtAudio()
    {
        if (caughtClip == null)
            return;

        Vector3 pos = playerCamera != null ? playerCamera.position : transform.position;
        AudioSource.PlayClipAtPoint(caughtClip, pos);
    }

    static float NormalizePitch(float pitch)
    {
        if (pitch > 180f)
            pitch -= 360f;
        return Mathf.Clamp(pitch, -80f, 80f);
    }
}
