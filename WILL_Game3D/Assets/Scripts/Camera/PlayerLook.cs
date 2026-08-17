using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(100)]
public class PlayerLook : MonoBehaviour
{
    [Header("Settings")]
    public float mouseSensitivity = 2f;
    public float upDownRange = 80f;

    [Header("References")]
    public Transform playerCamera;

    Rigidbody rb;
    float yaw;
    float pitch;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null)
            rb.freezeRotation = true;

        CaptureFacing();
        LockCursor();
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        string sceneName = scene.name.Replace('\u00A0', ' ');
        if (sceneName == "Game Over" || sceneName == "MainMenu" || sceneName == "LobbySystem" || sceneName == "CreditMenu")
            return;

        LockCursor();
        CaptureFacing();
    }

    void CaptureFacing()
    {
        yaw = transform.eulerAngles.y;
        pitch = 0f;
        if (playerCamera == null)
            return;

        float camPitch = playerCamera.localEulerAngles.x;
        if (camPitch > 180f)
            camPitch -= 360f;
        pitch = Mathf.Clamp(camPitch, -upDownRange, upDownRange);
    }

    void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void LateUpdate()
    {
        if (PauseMenu.IsPaused || PlayerDeath.IsDead)
            return;

        if (DialogueManager.Instance != null && DialogueManager.Instance.IsActive())
            return;

        if (playerCamera == null)
            return;

        float mouseX = Input.GetAxisRaw("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxisRaw("Mouse Y") * mouseSensitivity;

        yaw += mouseX;
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, -upDownRange, upDownRange);

        Quaternion bodyRot = Quaternion.Euler(0f, yaw, 0f);
        transform.rotation = bodyRot;
        if (rb != null)
            rb.rotation = bodyRot;

        playerCamera.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }
}
