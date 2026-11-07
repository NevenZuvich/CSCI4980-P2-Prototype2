using UnityEngine;

public class CheckpointController : MonoBehaviour
{

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            PlayerController playerController = other.gameObject.GetComponent<PlayerController>();

            playerController.respawnPosition = transform.position + 6 * Vector3.up;
        }
    }
}
