using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 8f;
    public float acceleration = 50f;
    public float deceleration = 60f;

    [Header("Sprint Settings")]
    public float sprintSpeed = 14f;
    public float staminaDrainRate = 25f;
    public float staminaRegenRate = 12f;
    public float staminaRegenDelay = 1.5f;

    private Rigidbody rb;
    private Animator animator;
    private PlayerStats playerStats;

    private Vector3 horizontalVelocity;
    private Vector3 inputDirection;

    private bool isSprinting;
    private bool canSprint = true;
    private float regenDelayTimer;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        playerStats = GetComponent<PlayerStats>();

        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    void Update()
    {
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsActive())
        {
            inputDirection = Vector3.zero;
            isSprinting = false;
            if (animator) animator.SetFloat("Speed", 0f);
            return;
        }

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        // Normalize so all 8 directions travel at equal speed
        inputDirection = new Vector3(h, 0f, v).normalized;

        HandleSprint();

        if (animator)
        {
            animator.SetFloat("Speed", inputDirection.magnitude);
            animator.SetBool("IsSprinting", isSprinting);
        }
    }

    void FixedUpdate()
    {
        MoveRelative();
    }

    void HandleSprint()
    {
        if (playerStats == null) return;

        bool wantsToSprint = Input.GetKey(KeyCode.LeftShift) && inputDirection.magnitude > 0;

        if (wantsToSprint && canSprint && playerStats.stamina > 0f)
        {
            isSprinting = true;
            regenDelayTimer = staminaRegenDelay;

            playerStats.stamina -= staminaDrainRate * Time.deltaTime;
            playerStats.stamina = Mathf.Max(playerStats.stamina, 0f);

            // Fully exhausted — stop sprint and lock it out until recovered
            if (playerStats.stamina <= 0f)
            {
                canSprint = false;
                isSprinting = false;
            }
        }
        else
        {
            isSprinting = false;

            if (regenDelayTimer > 0f)
            {
                regenDelayTimer -= Time.deltaTime;
            }
            else
            {
                playerStats.stamina += staminaRegenRate * Time.deltaTime;
                playerStats.stamina = Mathf.Min(playerStats.stamina, playerStats.maxStamina);

                // Only allow sprinting again once 25% stamina is recovered
                if (!canSprint && playerStats.stamina >= playerStats.maxStamina * 0.25f)
                    canSprint = true;
            }
        }
    }

    void MoveRelative()
    {
        // Build world-space move direction from player-relative input
        Vector3 moveDir = (transform.forward * inputDirection.z) + (transform.right * inputDirection.x);

        float activeSpeed = isSprinting ? sprintSpeed : moveSpeed;
        Vector3 targetVelocity = moveDir * activeSpeed;

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