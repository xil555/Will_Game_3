using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

/// <summary>
/// Trigger-based FP cutscene: talk to stray dog, dog runs away, player says "Wait!", then exit.
/// </summary>
[RequireComponent(typeof(Collider))]
public class CutsceneEnter : MonoBehaviour
{
    [Header("Cameras")]
    public GameObject playerCamera;
    public GameObject cutsceneCamera;

    [Header("Dog")]
    public Transform dog;
    public Animator dogAnimator;
    public Transform dogRunDestination;

    public float dogRunSpeed = 3f;
    public float dogStopDistance = 0.2f;

    [Header("Dialogue UI")]
    public GameObject dialoguePanel;
    public TMP_Text speakerName;
    public TMP_Text dialogueText;

    private GameObject player;

    private MonoBehaviour[] playerScripts;

    private Rigidbody playerRb;

    // Save player's original Rigidbody settings
    private RigidbodyConstraints originalConstraints;
    private bool originalUseGravity;

    // Save player's original camera position/rotation
    private Vector3 originalCameraPosition;
    private Quaternion originalCameraRotation;

    private bool cutsceneStarted = false;
    private bool waitingForE = false;

    private void OnTriggerEnter(Collider other)
    {
        if (cutsceneStarted)
            return;

        // Only trigger for the spawned Player
        if (!other.CompareTag("Player"))
            return;

        player = other.transform.root.gameObject;

        cutsceneStarted = true;

        StartCoroutine(PlayCutscene());
    }

    private IEnumerator PlayCutscene()
    {
        // =====================================
        // SAVE PLAYER STATE
        // =====================================

        playerScripts = player.GetComponents<MonoBehaviour>();

        playerRb = player.GetComponent<Rigidbody>();

        if (playerRb != null)
        {
            originalConstraints = playerRb.constraints;
            originalUseGravity = playerRb.useGravity;

            // Stop existing movement
            playerRb.linearVelocity = Vector3.zero;
            playerRb.angularVelocity = Vector3.zero;

            // Completely freeze player
            playerRb.constraints = RigidbodyConstraints.FreezeAll;
        }

        // =====================================
        // SAVE PLAYER CAMERA STATE
        // =====================================

        if (playerCamera != null)
        {
            originalCameraPosition =
                playerCamera.transform.position;

            originalCameraRotation =
                playerCamera.transform.rotation;
        }

        // =====================================
        // DISABLE PLAYER SCRIPTS
        // =====================================

        foreach (MonoBehaviour script in playerScripts)
        {
            if (script != this)
            {
                script.enabled = false;
            }
        }

        // =====================================
        // SWITCH TO CUTSCENE CAMERA
        // =====================================

        if (playerCamera != null)
            playerCamera.SetActive(false);

        if (cutsceneCamera != null)
            cutsceneCamera.SetActive(true);

        // =====================================
        // SHOW DIALOGUE
        // =====================================

        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);

        // Make sure dog starts idle
        if (dogAnimator != null)
            dogAnimator.SetBool("IsRunning", false);

        // =====================================
        // DIALOGUE 1
        // =====================================

        ShowDialogue(
            "PLAYER",
            "Hey... where did you come from?"
        );

        yield return StartCoroutine(WaitForE());

        // =====================================
        // DIALOGUE 2
        // =====================================

        ShowDialogue(
            "PLAYER",
            "Who do you belong to?"
        );

        yield return StartCoroutine(WaitForE());

        // =====================================
        // DOG RUNS AWAY
        // =====================================

        if (dogAnimator != null)
            dogAnimator.SetBool("IsRunning", true);

        // Start dog movement without waiting
        StartCoroutine(MoveDog());

        // =====================================
        // PLAYER SAYS WAIT
        // =====================================

        ShowDialogue(
            "PLAYER",
            "Wait!"
        );

        yield return StartCoroutine(WaitForE());

        // =====================================
        // END CUTSCENE
        // =====================================

        EndCutscene();
    }

    // =========================================
    // END CUTSCENE
    // =========================================

    private void EndCutscene()
    {
        // Hide dialogue
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        // Turn off cutscene camera
        if (cutsceneCamera != null)
            cutsceneCamera.SetActive(false);

        // =====================================
        // RESTORE PLAYER CAMERA
        // =====================================

        if (playerCamera != null)
        {
            // Restore original camera transform
            playerCamera.transform.position =
                originalCameraPosition;

            playerCamera.transform.rotation =
                originalCameraRotation;

            playerCamera.SetActive(true);
        }

        // =====================================
        // RESTORE PLAYER RIGIDBODY
        // =====================================

        if (playerRb != null)
        {
            // Restore original Rigidbody settings
            playerRb.constraints = originalConstraints;
            playerRb.useGravity = originalUseGravity;

            // Remove any movement created during cutscene
            playerRb.linearVelocity = Vector3.zero;
            playerRb.angularVelocity = Vector3.zero;
        }

        // =====================================
        // RESTORE PLAYER SCRIPTS
        // =====================================

        if (playerScripts != null)
        {
            foreach (MonoBehaviour script in playerScripts)
            {
                if (script != this)
                {
                    script.enabled = true;
                }
            }
        }

        // Prevent the cutscene from triggering again
        cutsceneStarted = true;
    }

    // =========================================
    // SHOW DIALOGUE
    // =========================================

    private void ShowDialogue(string speaker, string message)
    {
        if (speakerName != null)
            speakerName.text = speaker;

        if (dialogueText != null)
            dialogueText.text = message;

        waitingForE = true;
    }

    // =========================================
    // WAIT FOR E
    // =========================================

    private IEnumerator WaitForE()
    {
        waitingForE = true;

        while (waitingForE)
        {
            if (Keyboard.current != null &&
                Keyboard.current.eKey.wasPressedThisFrame)
            {
                waitingForE = false;
            }

            yield return null;
        }
    }

    // =========================================
    // MOVE DOG
    // =========================================

    private IEnumerator MoveDog()
    {
        if (dog == null || dogRunDestination == null)
            yield break;

        while (Vector3.Distance(
            dog.position,
            dogRunDestination.position
        ) > dogStopDistance)
        {
            Vector3 direction =
                dogRunDestination.position - dog.position;

            // Keep dog on ground
            direction.y = 0f;

            if (direction != Vector3.zero)
            {
                direction.Normalize();

                Quaternion targetRotation =
                    Quaternion.LookRotation(direction);

                dog.rotation = Quaternion.Slerp(
                    dog.rotation,
                    targetRotation,
                    10f * Time.deltaTime
                );
            }

            dog.position +=
                direction * dogRunSpeed * Time.deltaTime;

            yield return null;
        }

        // Place dog exactly at destination
        dog.position =
            dogRunDestination.position;

        // Return dog to idle
        if (dogAnimator != null)
            dogAnimator.SetBool("IsRunning", false);
    }
}
