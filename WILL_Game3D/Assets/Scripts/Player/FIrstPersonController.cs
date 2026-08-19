using System;
using System.Collections;
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
    [Tooltip("How fast the player turns when facing a dialogue target.")]
    public float faceTargetSpeed = 110f;

    [Header("References")]
    public Transform playerCamera;

    private Rigidbody rb;
    private Animator animator;
    private Vector3 currentVelocity;
    private Vector3 inputDirection;
    private float verticalRotation = 0f;
    private bool isSmoothFacing;

    public bool IsSmoothFacing => isSmoothFacing;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();

        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        SetCursorState(true);
    }

    void Update()
    {
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsActive())
        {
            SetCursorState(false);

            inputDirection = Vector3.zero;
            if (animator) animator.SetFloat("Speed", 0f);

            return;
        }

        if (isSmoothFacing)
        {
            inputDirection = Vector3.zero;
            if (animator) animator.SetFloat("Speed", 0f);
            return;
        }

        // Near a cabin drawer: keep mouse visible and stop look so clicking works.
        if (MoveObjectController.NearInteractableCount > 0)
        {
            SetCursorState(false);
            HandleInput();
            return;
        }

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
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        transform.Rotate(Vector3.up * mouseX);

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
        Vector3 moveDir = (transform.forward * inputDirection.z) + (transform.right * inputDirection.x);

        if (moveDir.magnitude > 1f) moveDir.Normalize();

        Vector3 targetVelocity = moveDir * moveSpeed;

        float currentStep = inputDirection.magnitude > 0 ? acceleration : deceleration;

        currentVelocity = Vector3.MoveTowards(
            currentVelocity,
            targetVelocity,
            currentStep * Time.fixedDeltaTime
        );

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

    public void FaceTarget(Transform target)
    {
        FaceTargetSmooth(target, null);
    }

    public void FaceTargetSmooth(Transform target, Action onComplete)
    {
        if (target == null)
        {
            onComplete?.Invoke();
            return;
        }

        StartCoroutine(FaceTargetSmoothRoutine(target, onComplete));
    }

    IEnumerator FaceTargetSmoothRoutine(Transform target, Action onComplete)
    {
        Vector3 direction = target.position - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f)
        {
            onComplete?.Invoke();
            yield break;
        }

        isSmoothFacing = true;
        SetCursorState(false);

        Quaternion targetBodyRotation = Quaternion.LookRotation(direction.normalized);
        float startVertical = verticalRotation;

        while (true)
        {
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetBodyRotation,
                faceTargetSpeed * Time.deltaTime
            );

            verticalRotation = Mathf.MoveTowards(verticalRotation, 0f, faceTargetSpeed * Time.deltaTime);
            if (playerCamera != null)
                playerCamera.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);

            bool bodyDone = Quaternion.Angle(transform.rotation, targetBodyRotation) < 0.5f;
            bool cameraDone = Mathf.Abs(verticalRotation) < 0.5f;

            if (bodyDone && cameraDone)
                break;

            yield return null;
        }

        transform.rotation = targetBodyRotation;
        verticalRotation = 0f;
        if (playerCamera != null)
            playerCamera.localRotation = Quaternion.Euler(0f, 0f, 0f);

        isSmoothFacing = false;
        onComplete?.Invoke();
    }
}
