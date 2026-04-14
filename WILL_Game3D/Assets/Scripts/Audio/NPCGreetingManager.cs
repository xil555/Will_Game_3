using UnityEngine;

public class NPCGreetingManager : MonoBehaviour
{
    public AudioSource npcAudioSource; 
    public AudioClip interactionClip;  
    public bool playOnlyOnce = true;   
    private bool hasPlayed = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (playOnlyOnce && hasPlayed) return;
            if (interactionClip != null && npcAudioSource != null)
            {
                npcAudioSource.PlayOneShot(interactionClip);
                hasPlayed = true;
            }
        }
    }
}
