using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Runs in the Game Over scene: unlocks the cursor, adds Retry, and returns to the menu
/// without leaving a persistent player behind.
/// </summary>
public class GameOverMenu : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void HookSceneLoad()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!IsGameOverScene(scene.name))
            return;

        GameObject host = GameObject.Find("GameManager");
        if (host == null)
            host = new GameObject("GameOverMenu");

        if (host.GetComponent<GameOverMenu>() == null)
            host.AddComponent<GameOverMenu>();
    }

    void Start()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        EnsureRetryButton();
    }

    public void RetryLevel()
    {
        string sceneName = PlayerDeath.LastGameplayScene;
        if (string.IsNullOrEmpty(sceneName) || IsGameOverScene(sceneName))
            sceneName = "Easy";

        PersistentPlayer.Release();
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }

    public void OpenMainMenu()
    {
        PersistentPlayer.Release();
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    void EnsureRetryButton()
    {
        Button retry = FindButton("Retry");
        if (retry != null)
        {
            retry.onClick.RemoveAllListeners();
            retry.onClick.AddListener(RetryLevel);
            return;
        }

        Button menuButton = FindButton("Return to Menu");
        if (menuButton == null)
            return;

        GameObject clone = Instantiate(menuButton.gameObject, menuButton.transform.parent);
        clone.name = "Retry";

        RectTransform rt = clone.GetComponent<RectTransform>();
        RectTransform menuRt = menuButton.GetComponent<RectTransform>();
        rt.anchoredPosition = menuRt.anchoredPosition + Vector2.up * 90f;

        TMP_Text label = clone.GetComponentInChildren<TMP_Text>();
        if (label != null)
            label.text = "Retry";

        Button retryButton = clone.GetComponent<Button>();
        retryButton.onClick = new Button.ButtonClickedEvent();
        retryButton.onClick.AddListener(RetryLevel);
    }

    static Button FindButton(string objectName)
    {
#if UNITY_6000_0_OR_NEWER
        Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
        Button[] buttons = FindObjectsOfType<Button>(true);
#endif
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] != null && buttons[i].gameObject.name == objectName)
                return buttons[i];
        }
        return null;
    }

    static bool IsGameOverScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
            return false;
        return sceneName.Replace('\u00A0', ' ') == "Game Over";
    }
}
