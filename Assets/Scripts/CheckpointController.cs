using UnityEngine;

public class CheckpointController : MonoBehaviour
{
    [SerializeField] private float yOffset = 1.0f; // Small lift to prevent falling through floor

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController playerController = other.GetComponent<PlayerController>();
            if (playerController != null)
            {
                playerController.respawnPosition = transform.position + (Vector3.up * yOffset);
            }
        }
    }
}
