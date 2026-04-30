using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 8f;
    public float acceleration = 50f;
    public float deceleration = 60f;

    private Rigidbody rb;
    private Animator animator;
    private Vector3 currentVelocity;
    private Vector3 inputDirection;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();

        // Physics Setup
        rb.freezeRotation = true; 
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    void Update()
    {
        // Dialogue Lock
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsActive())
        {
            inputDirection = Vector3.zero;
            if (animator) animator.SetFloat("Speed", 0f);
            return;
        }

        // Get raw input
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        
        // This is the "Local Input"
        inputDirection = new Vector3(h, 0f, v).normalized;

        if (animator) animator.SetFloat("Speed", inputDirection.magnitude);
    }

    void FixedUpdate()
    {
        MoveRelative();
    }

    void MoveRelative()
    {
        // KEY FIX: Convert input to the direction the player is facing
        // transform.forward is where the player looks
        // transform.right is the player's side-to-side
        Vector3 moveDir = (transform.forward * inputDirection.z) + (transform.right * inputDirection.x);
        
        // Ensure we don't move faster diagonally
        if (moveDir.magnitude > 1f) moveDir.Normalize();

        Vector3 targetVelocity = moveDir * moveSpeed;

        // Determine if we are trying to move or trying to stop
        float currentStep = inputDirection.magnitude > 0 ? acceleration : deceleration;

        currentVelocity = Vector3.MoveTowards(
            currentVelocity,
            targetVelocity,
            currentStep * Time.fixedDeltaTime
        );

        // Apply movement
        rb.MovePosition(rb.position + currentVelocity * Time.fixedDeltaTime);
    }
}