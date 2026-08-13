using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    public static bool IsPaused { get; private set; }

    public GameObject pauseMenu;

    Canvas pauseCanvas;

    // Unity keeps Time.timeScale at 0 after you stop play while paused.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void ResetTimeOnLoad()
    {
        IsPaused = false;
        Time.timeScale = 1f;
    }

    void Awake()
    {
        IsPaused = false;
        Time.timeScale = 1f;
    }

    void Start()
    {
        EnsureEventSystem();

        if (pauseMenu != null)
        {
            pauseCanvas = pauseMenu.GetComponent<Canvas>();
            if (pauseCanvas == null)
                pauseCanvas = pauseMenu.GetComponentInParent<Canvas>();

            pauseMenu.SetActive(false);
        }

        IsPaused = false;
        Time.timeScale = 1f;
        SetCursorLocked(true);
    }

    void OnDisable()
    {
        if (IsPaused)
        {
            Time.timeScale = 1f;
            IsPaused = false;
        }
    }

    void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape))
            return;

        if (IsPaused)
            Resume();
        else
            Pause();
    }

    public void ResumeButton()
    {
        Resume();
    }

    public void MainMenuButton()
    {
        Resume();
        SceneManager.LoadScene("MainMenu");
    }

    public void QuitGame()
    {
        Resume();
        Application.Quit();
    }

    void Pause()
    {
        if (pauseMenu == null)
            return;

        IsPaused = true;
        pauseMenu.SetActive(true);
        Time.timeScale = 0f;

        if (pauseCanvas != null)
        {
            pauseCanvas.overrideSorting = true;
            pauseCanvas.sortingOrder = 200;
        }

        SetCursorLocked(false);
        EnsureEventSystem();
    }

    void Resume()
    {
        IsPaused = false;

        if (pauseMenu != null)
            pauseMenu.SetActive(false);

        Time.timeScale = 1f;
        SetCursorLocked(true);
    }

    static void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    static void EnsureEventSystem()
    {
        if (EventSystem.current != null)
            return;

        GameObject es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<StandaloneInputModule>();
    }
}
