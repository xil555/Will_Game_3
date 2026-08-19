using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Add this to the Player (same object as PlayerMovement).
/// Handles slow turn toward dialogue targets without editing PlayerMovement.
/// </summary>
public class PlayerDialogueLook : MonoBehaviour
{
    [Header("Camera")]
    [Tooltip("Leave empty to use the first Camera on the player.")]
    public Transform playerCamera;

    [Header("Turn")]
    public float faceTargetSpeed = 110f;
    public float maxTurnTime = 1.6f;

    [Header("Optional")]
    [Tooltip("Assign PlayerLook here so mouse look pauses. If empty, it is found automatically.")]
    public Behaviour lookControl;

    public static bool IsSmoothFacing { get; private set; }

    Rigidbody rb;
    PlayerMovement playerMovement;
    bool isRunning;
    bool movementPaused;
    bool lookPaused;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        playerMovement = GetComponent<PlayerMovement>();
        ResolveCamera();
        ResolveLookControl();
    }

    void Update()
    {
        if (IsSmoothFacing)
            StopMotion();

        if (!movementPaused && !lookPaused)
            return;

        bool dialogueActive = DialogueManager.Instance != null && DialogueManager.Instance.IsActive();
        if (dialogueActive || IsSmoothFacing)
            return;

        RestorePlayerControls();
    }

    void ResolveCamera()
    {
        if (playerCamera != null)
            return;

        Camera cam = GetComponentInChildren<Camera>(true);
        if (cam != null)
            playerCamera = cam.transform;
        else if (Camera.main != null)
            playerCamera = Camera.main.transform;
    }

    void ResolveLookControl()
    {
        if (lookControl != null)
            return;

        Behaviour[] behaviours = GetComponents<Behaviour>();
        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] == null)
                continue;

            string typeName = behaviours[i].GetType().Name;
            if (typeName == "PlayerLook" || typeName == "MouseLook" || typeName == "CameraLook")
            {
                lookControl = behaviours[i];
                return;
            }
        }

        behaviours = GetComponentsInChildren<Behaviour>(true);
        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] == null)
                continue;

            string typeName = behaviours[i].GetType().Name;
            if (typeName == "PlayerLook" || typeName == "MouseLook" || typeName == "CameraLook")
            {
                lookControl = behaviours[i];
                return;
            }
        }
    }

    public void FaceTargetSmooth(Transform target, Action onComplete)
    {
        if (target == null)
        {
            onComplete?.Invoke();
            return;
        }

        if (isRunning)
            return;

        StartCoroutine(FaceTargetSmoothRoutine(target, onComplete));
    }

    IEnumerator FaceTargetSmoothRoutine(Transform target, Action onComplete)
    {
        ResolveCamera();
        ResolveLookControl();

        isRunning = true;
        IsSmoothFacing = true;

        PausePlayerControls();
        StopMotion();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Vector3 direction = target.position - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f)
            direction = transform.forward;

        Quaternion targetBodyRotation = Quaternion.LookRotation(direction.normalized);

        float currentPitch = 0f;
        if (playerCamera != null)
        {
            currentPitch = playerCamera.localEulerAngles.x;
            if (currentPitch > 180f)
                currentPitch -= 360f;
        }

        float elapsed = 0f;

        while (elapsed < maxTurnTime)
        {
            StopMotion();

            Quaternion currentBody = rb != null ? rb.rotation : transform.rotation;
            Quaternion nextBody = Quaternion.RotateTowards(
                currentBody,
                targetBodyRotation,
                faceTargetSpeed * Time.deltaTime
            );
            SetBodyRotation(nextBody);

            currentPitch = Mathf.MoveTowards(currentPitch, 0f, faceTargetSpeed * Time.deltaTime);
            if (playerCamera != null)
                playerCamera.localRotation = Quaternion.Euler(currentPitch, 0f, 0f);

            elapsed += Time.deltaTime;

            bool bodyDone = Quaternion.Angle(nextBody, targetBodyRotation) < 1f;
            bool cameraDone = Mathf.Abs(currentPitch) < 1f;
            if (bodyDone && cameraDone)
                break;

            yield return null;
        }

        SetBodyRotation(targetBodyRotation);
        if (playerCamera != null)
            playerCamera.localRotation = Quaternion.Euler(0f, 0f, 0f);

        StopMotion();

        IsSmoothFacing = false;
        isRunning = false;
        onComplete?.Invoke();
    }

    void SetBodyRotation(Quaternion rotation)
    {
        if (rb != null)
        {
            rb.MoveRotation(rotation);
            rb.rotation = rotation;
        }
        else
        {
            transform.rotation = rotation;
        }
    }

    void PausePlayerControls()
    {
        StopMotion();

        if (playerMovement != null && playerMovement.enabled)
        {
            playerMovement.enabled = false;
            movementPaused = true;
        }

        if (lookControl != null && lookControl.enabled)
        {
            lookControl.enabled = false;
            lookPaused = true;
        }
    }

    void RestorePlayerControls()
    {
        StopMotion();

        if (movementPaused && playerMovement != null)
        {
            playerMovement.enabled = true;
            movementPaused = false;
        }

        if (lookPaused && lookControl != null)
        {
            lookControl.enabled = true;
            lookPaused = false;
        }
    }

    void StopMotion()
    {
        if (rb == null)
            return;

#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = Vector3.zero;
#else
        rb.velocity = Vector3.zero;
#endif
        rb.angularVelocity = Vector3.zero;
    }
}
