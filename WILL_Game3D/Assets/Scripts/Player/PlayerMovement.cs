using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Animator))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 8f;
    public float acceleration = 40f;

    private Rigidbody rb;
    private Animator animator;

    private Vector3 currentVelocity;
    private Vector3 inputDirection;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();

        // Prevent tipping over
        rb.constraints = RigidbodyConstraints.FreezeRotationX | 
                         RigidbodyConstraints.FreezeRotationZ;
    }

    void Update()
    {
        // 1. Check if the Dialogue System is currently running
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsActive())
        {
            // Reset input so the player doesn't keep sliding
            inputDirection = Vector3.zero;
        
            // Ensure the animator returns to Idle
            animator.SetFloat("Speed", 0f);
        
            return; // Exit Update early so movement logic doesn't run
        }

        // 2. Normal movement input (Existing code)
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        inputDirection = new Vector3(h, 0f, v).normalized;

        float speed = inputDirection.magnitude;
        animator.SetFloat("Speed", speed);
    }

    void FixedUpdate()
    {
        HandleMovement();
    }

    void HandleMovement()
    {
        if (inputDirection.magnitude > 0.01f)
        {
            // Player-relative movement
            Vector3 moveDirection =
                transform.right * inputDirection.x +
                transform.forward * inputDirection.z;

            Vector3 targetVelocity = moveDirection * moveSpeed;

            currentVelocity = Vector3.MoveTowards(
                currentVelocity,
                targetVelocity,
                acceleration * Time.fixedDeltaTime
            );

            // Optional: rotate player toward movement direction
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                10f * Time.fixedDeltaTime
            );
        }
        else
        {
            currentVelocity = Vector3.zero;
        }

        rb.MovePosition(rb.position + currentVelocity * Time.fixedDeltaTime);
    }
}