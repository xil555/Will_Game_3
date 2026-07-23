using UnityEngine;
using UnityEngine.SceneManagement;

public class ButtonHandler : MonoBehaviour
{
    void Start()
    {
        // Lobby is a menu screen — cursor must be visible and free
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void OnEasyButtonClick()
    {
        Debug.Log("Easy Button Clicked!");
        LockCursorAndLoad("Easy");
    }
    public void OnMediumButtonClick()
    {
        Debug.Log("Medium Button Clicked!");
        LockCursorAndLoad("Medium");
    }
    public void OnHardButtonClick()
    {
        Debug.Log("Hard Button Clicked!");
        LockCursorAndLoad("Hard");
    }

    public void OnTutorialButtonClick()
    {
        Debug.Log("Tutorial Button Clicked!");
        LockCursorAndLoad("Tutorial");
    }

    private void LockCursorAndLoad(string sceneName)
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        SceneManager.LoadScene(sceneName);
    }
}
