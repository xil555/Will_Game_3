using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerDeath : MonoBehaviour
{
    // Type the exact name of your Death or Main Menu scene here
    [SerializeField] private string deathSceneName = "MainMenu"; 

    private void OnTriggerEnter(Collider other)
    {
        // Check if the object we hit has the "Police" tag
        if (other.gameObject.CompareTag("Police"))
        {
            TriggerDeath();
        }
    }


    private void TriggerDeath()
    {
        Debug.Log("Player was hit by the police!");
        
        // Optional: Reset time scale in case your menu relies on it
        Time.timeScale = 1f; 
        
        // Load the scene
        SceneManager.LoadScene(deathSceneName);
    }
}