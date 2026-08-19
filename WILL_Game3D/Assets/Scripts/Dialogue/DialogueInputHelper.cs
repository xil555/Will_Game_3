using UnityEngine;

/// <summary>
/// Optional helper — does not change DialogueManager.
/// Wire UI buttons to DialogueManager.DisplayNextSentence() instead if you prefer.
/// </summary>
public class DialogueInputHelper : MonoBehaviour
{
    void Update()
    {
        if (DialogueManager.Instance == null || !DialogueManager.Instance.IsActive())
            return;

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
            DialogueManager.Instance.DisplayNextSentence();
    }
}
