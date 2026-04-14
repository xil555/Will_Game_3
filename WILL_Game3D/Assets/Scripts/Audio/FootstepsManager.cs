using UnityEngine;

public class FootstepsManager : MonoBehaviour
{
    [Header("Audio Setup")]
    public AudioSource audioSource;
    public AudioClip[] footstepClips; 

    [Header("Movement Settings")]
    public float stepInterval = 0.5f; 
    private float stepTimer;

    void Update()
    {
        // Get movement input
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        // If the player is moving (not standing still)
        if (horizontal != 0 || vertical != 0)
        {
            stepTimer -= Time.deltaTime;

            if (stepTimer <= 0)
            {
                PlayFootstep();
                stepTimer = stepInterval;
            }
        }
    }

    void PlayFootstep()
    {
        if (footstepClips.Length > 0 && audioSource != null)
        {
            // Pick a random sound so it doesn't sound like a machine
            int index = Random.Range(0, footstepClips.Length);
            
            // Randomize pitch slightly for extra realism
            audioSource.pitch = Random.Range(0.9f, 1.1f);
            
            audioSource.PlayOneShot(footstepClips[index]);
        }
    }
}
