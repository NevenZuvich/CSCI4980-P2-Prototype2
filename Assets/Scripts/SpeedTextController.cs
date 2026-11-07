using UnityEngine;
using TMPro;

public class SpeedTextController : MonoBehaviour
{
    Rigidbody playerRB;
    TMP_Text textComponent;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        playerRB = GameObject.Find("Player").GetComponent<Rigidbody>();
        textComponent = GetComponent<TMP_Text>();
    }

    void Update()
    {
        if (playerRB != null)
        { 
          float vx = playerRB.linearVelocity.x;
          float vz = playerRB.linearVelocity.z;

          float horizontalSpeed = Mathf.Sqrt(Mathf.Pow(vx, 2f) + Mathf.Pow(vz, 2f));

          float absRoundedSpeed = Mathf.Round(horizontalSpeed *100f) / 100f;
          textComponent.text = absRoundedSpeed.ToString();
          if (absRoundedSpeed > 21)
            textComponent.color = new Color(1, 165f/ 255f, 0, 1); // orange
          else if (absRoundedSpeed > 16)
            textComponent.color = Color.red;
          else
            textComponent.color = Color.white;
        }
    }
}
