using UnityEngine;

public class RotateY : MonoBehaviour
{
    public float rpm = 60f; // rotations per minute

    void Update()
    {
        float degreesPerSecond = rpm * 6f; // 360 degrees * (rpm/60)
        transform.Rotate(0f, degreesPerSecond * Time.deltaTime, 0f, Space.Self);
    }
}
