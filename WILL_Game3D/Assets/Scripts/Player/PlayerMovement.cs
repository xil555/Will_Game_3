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
        // Read input ONLY here
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        inputDirection = new Vector3(h, 0f, v).normalized;

        // 🎬 Update animation parameter
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