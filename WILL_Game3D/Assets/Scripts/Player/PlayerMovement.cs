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
    private Vector3 horizontalVelocity;
    private Vector3 inputDirection;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();

        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    void Update()
    {
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsActive())
        {
            inputDirection = Vector3.zero;
            if (animator) animator.SetFloat("Speed", 0f);
            return;
        }

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        // Normalize so all 8 directions travel at equal speed
        inputDirection = new Vector3(h, 0f, v).normalized;

        if (animator) animator.SetFloat("Speed", inputDirection.magnitude);
    }

    void FixedUpdate()
    {
        MoveRelative();
    }

    void MoveRelative()
    {
        // Build world-space move direction from player-relative input
        Vector3 moveDir = (transform.forward * inputDirection.z) + (transform.right * inputDirection.x);

        Vector3 targetVelocity = moveDir * moveSpeed;

        float currentStep = inputDirection.magnitude > 0 ? acceleration : deceleration;

        // Smoothly accelerate / decelerate horizontal velocity only
        horizontalVelocity = Vector3.MoveTowards(
            horizontalVelocity,
            targetVelocity,
            currentStep * Time.fixedDeltaTime
        );

        // Preserve the Rigidbody's existing Y velocity so gravity & jumping still work
#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = new Vector3(horizontalVelocity.x, rb.linearVelocity.y, horizontalVelocity.z);
#else
        rb.velocity = new Vector3(horizontalVelocity.x, rb.velocity.y, horizontalVelocity.z);
#endif
    }
}