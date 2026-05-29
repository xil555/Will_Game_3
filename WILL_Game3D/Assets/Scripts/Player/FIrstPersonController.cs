using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class FirstPersonController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 8f;
    public float acceleration = 50f;
    public float deceleration = 60f;

    [Header("Look Settings")]
    public float mouseSensitivity = 2f;
    public float upDownRange = 80f;

    [Header("References")]
    public Transform playerCamera;

    private Rigidbody rb;
    private Animator animator;
    private Vector3 currentVelocity;
    private Vector3 inputDirection;
    private float verticalRotation = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();

        // Physics Setup: stop external forces from tipping the player over
        rb.freezeRotation = true; 
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        // Start with a hidden and locked cursor
        SetCursorState(true);
    }

    void Update()
    {
        // Dialogue Lock Check
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsActive())
        {
            // Free the cursor so the player can interact with UI elements/buttons
            SetCursorState(false);

            // Halt input processing and stop the animator
            inputDirection = Vector3.zero;
            if (animator) animator.SetFloat("Speed", 0f);
            
            return; 
        }

        // Re-lock the cursor automatically if dialogue just ended
        if (Cursor.lockState != CursorLockMode.Locked)
        {
            SetCursorState(true);
        }

        HandleLook();
        HandleInput();
    }

    void FixedUpdate()
    {
        MoveRelative();
    }

    void HandleLook()
    {
        // 1. Horizontal Rotation (Turns the entire player body left/right)
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        transform.Rotate(Vector3.up * mouseX);

        // 2. Vertical Rotation (Tilts only the child camera up/down)
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, -upDownRange, upDownRange);
        
        if (playerCamera != null)
        {
            playerCamera.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
        }
    }

    void HandleInput()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        
        inputDirection = new Vector3(h, 0f, v).normalized;

        if (animator) animator.SetFloat("Speed", inputDirection.magnitude);
    }

    void MoveRelative()
    {
        // Translate local WASD vector to world space directions based on character orientation
        Vector3 moveDir = (transform.forward * inputDirection.z) + (transform.right * inputDirection.x);
        
        if (moveDir.magnitude > 1f) moveDir.Normalize();

        Vector3 targetVelocity = moveDir * moveSpeed;

        // Decide whether to apply acceleration or braking deceleration
        float currentStep = inputDirection.magnitude > 0 ? acceleration : deceleration;

        // Interpolate horizontal velocity layers
        currentVelocity = Vector3.MoveTowards(
            currentVelocity,
            targetVelocity,
            currentStep * Time.fixedDeltaTime
        );

        // Keep the Rigidbody's current vertical velocity so gravity still functions naturally
        Vector3 finalVelocity = currentVelocity;
        
        #if UNITY_6000_0_OR_NEWER
        finalVelocity.y = rb.linearVelocity.y; 
        rb.linearVelocity = finalVelocity;
        #else
        finalVelocity.y = rb.velocity.y; 
        rb.velocity = finalVelocity;
        #endif
    }

    private void SetCursorState(bool lockCursor)
    {
        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}