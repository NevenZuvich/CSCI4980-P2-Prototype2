using UnityEngine;

public class AudioTrigger : MonoBehaviour
{
    [Header("Audio Settings")]
    [SerializeField] private AudioClip audioClip;
    [Range(0f, 1f)]
    [SerializeField] private float volume = 1f;
    [SerializeField] private bool interruptCurrentAudio = true;

    [Header("Trigger Rules")]
    [SerializeField] private bool triggerOnce = true;

    private bool hasTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered && triggerOnce) return;

        if (other.CompareTag("Player"))
        {
            if (AudioManager.Instance != null && audioClip != null)
            {
                AudioManager.Instance.PlayAudioClip(audioClip, volume, interruptCurrentAudio);
                hasTriggered = true;

                if (triggerOnce)
                {
                    GetComponent<Collider>().enabled = false;
                }
            }
        }
    }
}
