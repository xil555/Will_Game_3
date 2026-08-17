using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DialogueTest : MonoBehaviour
{
    [Header("Dialogue Settings")]
    [SerializeField] private string npcID = "TutorialNPC";
    [SerializeField] private string npcDisplayName = "";
    [SerializeField] private string talkPrompt = "Press E to speak to {name}";
    [SerializeField] private string finishDialogueId = "FinishTutorialNPC";
    [SerializeField] private string nextSceneName = "LobbySystem";

    [Header("Ranges")]
    [SerializeField] private SphereCollider outerRange;
    [SerializeField] private SphereCollider innerRange;

    PlayerStats playerStats;
    bool playerInInnerRange;
    bool finishing;

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (IsFromCollider(outerRange, other) && EventDebugManager.Instance != null)
            EventDebugManager.Instance.TriggerEvent("NPC: 'Hey! Come here!'");

        if (IsFromCollider(innerRange, other))
        {
            playerInInnerRange = true;
            InteractPromptUI.Show(this, GetTalkPrompt());
            if (EventDebugManager.Instance != null)
                EventDebugManager.Instance.TriggerEvent(GetTalkPrompt());
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (IsFromCollider(innerRange, other))
        {
            playerInInnerRange = false;
            InteractPromptUI.Hide(this);
        }
    }

    void Start()
    {
        ResolvePlayer();
        if (playerStats == null)
            Debug.LogWarning("DialogueTest: Could not find PlayerStats on the Player object. Will retry each frame.");
    }

    void Update()
    {
        if (PauseMenu.IsPaused || PlayerDeath.IsDead)
            return;

        if (playerStats == null)
            ResolvePlayer();

        RefreshInsideRange();
        UpdateTalkPrompt();

        if (!playerInInnerRange || !Input.GetKeyDown(KeyCode.E))
            return;

        if (finishing || IsReadyToFinishTutorial())
            ContinueFinishTalk();
        else
            Interact();
    }

    void Interact()
    {
        if (DialogueManager.Instance == null)
            return;

        if (DialogueManager.Instance.IsActive())
        {
            DialogueManager.Instance.DisplayNextSentence();
            return;
        }

        DialogueManager.Instance.StartDialogue(npcID);
        Debug.Log("[Objective] Started dialogue with NPC '" + npcID + "'");

        if (ObjectiveManager.Instance != null)
            ObjectiveManager.Instance.ReportNpcTalked(npcID);
        else
            Debug.LogWarning("[Objective] Talked to NPC but no ObjectiveManager exists in this scene.");
    }

    void ContinueFinishTalk()
    {
        if (DialogueManager.Instance == null)
            return;

        if (DialogueManager.Instance.IsActive())
        {
            DialogueManager.Instance.DisplayNextSentence();
            return;
        }

        if (finishing)
            return;

        finishing = true;
        DialogueManager.Instance.StartDialogue(finishDialogueId);

        if (ObjectiveManager.Instance != null)
            ObjectiveManager.Instance.ReportNpcTalked(npcID);

        StartCoroutine(WaitAndLoadLobby());
    }

    bool IsReadyToFinishTutorial()
    {
        ObjectiveManager objectives = ObjectiveManager.Instance;
        if (objectives == null)
            return playerStats != null && playerStats.batteriesCollected >= 3;

        if (objectives.AllComplete)
            return true;

        ObjectiveData current = objectives.CurrentObjective;
        return objectives.CurrentIndex > 0
            && current != null
            && current.type == ObjectiveType.TalkToNpc
            && (string.IsNullOrEmpty(current.triggerId)
                || string.Equals(current.triggerId, npcID, StringComparison.OrdinalIgnoreCase));
    }

    void RefreshInsideRange()
    {
        if (innerRange == null)
            return;

        if (playerStats == null)
        {
            playerInInnerRange = false;
            return;
        }

        Vector3 playerPos = playerStats.transform.position;
        playerInInnerRange = innerRange.bounds.Contains(playerPos)
            || (innerRange.ClosestPoint(playerPos) - playerPos).sqrMagnitude < 0.05f * 0.05f;
    }

    void UpdateTalkPrompt()
    {
        bool talking = DialogueManager.Instance != null && DialogueManager.Instance.IsActive();
        if (playerInInnerRange && !talking && !finishing)
            InteractPromptUI.Show(this, GetTalkPrompt());
        else
            InteractPromptUI.Hide(this);
    }

    string GetTalkPrompt()
    {
        string name = string.IsNullOrWhiteSpace(npcDisplayName) ? PrettyNpcName(npcID) : npcDisplayName;
        if (string.IsNullOrWhiteSpace(talkPrompt))
            return "Press E to speak to " + name;
        return talkPrompt.Replace("{name}", name);
    }

    static string PrettyNpcName(string id)
    {
        if (string.Equals(id, "TutorialNPC", StringComparison.OrdinalIgnoreCase))
            return "the strange man";
        if (string.Equals(id, "OldMan", StringComparison.OrdinalIgnoreCase))
            return "the old man";
        return string.IsNullOrEmpty(id) ? "NPC" : id;
    }

    void OnDestroy()
    {
        InteractPromptUI.Hide(this);
    }

    void ResolvePlayer()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
            playerStats = playerObject.GetComponent<PlayerStats>();
    }

    bool IsFromCollider(SphereCollider sphere, Collider other)
    {
        if (sphere == null)
            return false;
        return other.bounds.Intersects(sphere.bounds);
    }

    IEnumerator WaitAndLoadLobby()
    {
        Debug.Log("[Objective] Tutorial finished. Returning to lobby.");

        float timeout = 10f;
        while (timeout > 0f && DialogueManager.Instance != null && DialogueManager.Instance.IsActive())
        {
            timeout -= Time.deltaTime;
            yield return null;
        }

        yield return new WaitForSeconds(1.2f);

        PersistentPlayer.Release();
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SceneManager.LoadScene(nextSceneName);
    }
}
