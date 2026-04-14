using UnityEngine;
using System.Collections;

public class MusicManager : MonoBehaviour
{
    [Header("Audio Settings")]
    public AudioSource musicSource; 
    public AudioClip backgroundMusic; 

    [Header("Volume Control")]
    public float maxVolume = 0.5f;
    public float fadeDuration = 2.0f;

    void Start()
    {
        if (backgroundMusic != null)
        {
            musicSource.clip = backgroundMusic;
            musicSource.volume = 0;
            musicSource.Play();
            StartCoroutine(FadeMusic(maxVolume));
        }
    }

    IEnumerator FadeMusic(float targetVolume)
    {
        while (!Mathf.Approximately(musicSource.volume, targetVolume))
        {
            musicSource.volume = Mathf.MoveTowards(musicSource.volume, targetVolume, Time.deltaTime / fadeDuration);
            yield return null;
        }
    }
}
