using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerLook : MonoBehaviour
{
    [Header("Settings")]
    public float mouseSensitivity = 2f;
    public float upDownRange = 80f;

    [Header("References")]
    public Transform playerCamera;

    private Rigidbody rb;
    private float verticalRotation = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        if (rb != null)
            rb.freezeRotation = true;

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
        // Re-lock cursor whenever the player lands in a new scene
        LockCursor();
        verticalRotation = 0f;
    }

    void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if (PauseMenu.IsPaused)
            return;

        // 1. Dialogue Lock - Stop looking around if talking
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsActive())
        {
            return;
        }

        // 2. Horizontal Rotation (Turning the whole body Left/Right)
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        transform.Rotate(Vector3.up * mouseX);

        // 3. Vertical Rotation (Looking Up/Down with just the Camera)
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, -upDownRange, upDownRange);
        
        playerCamera.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
    }
}