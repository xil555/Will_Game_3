using UnityEngine;
using UnityEngine.SceneManagement;

public class ButtonHandler : MonoBehaviour
{
    public void OnEasyButtonClick()
    {
        Debug.Log("Easy Button Clicked!");
        SceneManager.LoadScene("Easy");
    }
    public void OnMediumButtonClick()
    {
        Debug.Log("Medium Button Clicked!");
        SceneManager.LoadScene("Medium");
    }
    public void OnHardButtonClick()
    {
        Debug.Log("Hard Button Clicked!");
        SceneManager.LoadScene("Hard");
    }

    public void OnTutorialButtonClick()
    {
        Debug.Log("Tutorial Button Clicked!");
        SceneManager.LoadScene("Tutorial");
    }

}
