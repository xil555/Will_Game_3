using UnityEngine;

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

        // Lock the cursor to the middle of the screen and hide it
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Ensure the Rigidbody doesn't fight the rotation logic
        if (rb != null)
        {
            rb.freezeRotation = true;
        }
    }

    void Update()
    {
        // 1. Dialogue Lock - Stop looking around if talking
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsActive())
        {
            // Optional: Unlock cursor during dialogue
            // Cursor.lockState = CursorLockMode.None;
            // Cursor.visible = true;
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