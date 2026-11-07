using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    private AudioSource audioSource;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        
        // Ensures 2D global sound playback
        audioSource.spatialBlend = 0f; 
    }

    public void PlayAudioClip(AudioClip clip, float volume = 1f, bool interruptPrevious = true)
    {
        if (clip == null || audioSource == null) return;

        if (interruptPrevious)
        {
            audioSource.Stop();
            audioSource.clip = clip;
            audioSource.volume = volume;
            audioSource.Play();
        }
        else
        {
            audioSource.PlayOneShot(clip, volume);
        }
    }
}
