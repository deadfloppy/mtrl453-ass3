using UnityEngine;

public class HelicoidRotator : MonoBehaviour
{
    public float rotationSpeed = 30f; // Degrees per second
    public Vector3 rotationAxis = Vector3.up;
    
    void Update()
    {
        transform.Rotate(rotationAxis, rotationSpeed * Time.deltaTime);
    }
}