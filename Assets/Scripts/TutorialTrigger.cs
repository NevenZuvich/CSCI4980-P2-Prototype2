using UnityEngine;

public class TutorialTrigger : MonoBehaviour
{
  [Header("Message Settings")]
  [TextArea(2, 4)]
  [SerializeField] private string message = "Press SHIFT to Crouch & Slide";
  [SerializeField] private float displayDuration = 6f;

  [Header("Trigger Rules")]
  [SerializeField] private bool triggerOnce = true;

  private bool hasTriggered = false;

  private void OnTriggerEnter(Collider other)
  {
    if (hasTriggered && triggerOnce) return;

    if (other.CompareTag("Player"))
    {
      if (TutorialUIManager.Instance != null)
      {
        TutorialUIManager.Instance.ShowTutorialText(message, displayDuration);
        hasTriggered = true;

        if (triggerOnce)
        {
          // Disable collider so it never fires again
          GetComponent<Collider>().enabled = false;
        }
      }
    }
  }
}
