using System.Collections;
using UnityEngine;
using TMPro;

public class TutorialUIManager : MonoBehaviour
{
    public static TutorialUIManager Instance { get; private set; }

    [SerializeField] private TextMeshProUGUI tutorialText;
    private Coroutine hideCoroutine;

    private void Awake()
    {
        // Singleton setup for easy access from any trigger
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (tutorialText != null)
        {
            tutorialText.gameObject.SetActive(false);
        }
    }

    public void ShowTutorialText(string message, float displayDuration)
    {
        if (tutorialText == null) return;

        // Stop any running hide timer if another trigger is hit immediately
        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
        }

        tutorialText.text = message;
        tutorialText.gameObject.SetActive(true);

        hideCoroutine = StartCoroutine(HideAfterDelay(displayDuration));
    }

    private IEnumerator HideAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        tutorialText.gameObject.SetActive(false);
        hideCoroutine = null;
    }
}
