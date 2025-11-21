using UnityEngine;

public class OrbitCamera : MonoBehaviour
{
    public Transform target; // Assign the HelicoidDisplay GameObject
    public float distance = 5f;
    public float rotationSpeed = 5f;
    public float zoomSpeed = 2f;
    public float minDistance = 2f;
    public float maxDistance = 20f;
    
    private float currentX = 0f;
    private float currentY = 20f;
    
    void Start()
    {
        if (target == null)
        {
            Debug.LogError("OrbitCamera: No target assigned! Please assign the HelicoidDisplay GameObject.");
        }
    }
    
    void LateUpdate()
    {
        if (target == null) return;
        
        // Rotate camera with mouse
        if (Input.GetMouseButton(0) || Input.GetMouseButton(1))
        {
            currentX += Input.GetAxis("Mouse X") * rotationSpeed;
            currentY -= Input.GetAxis("Mouse Y") * rotationSpeed;
            currentY = Mathf.Clamp(currentY, -89f, 89f);
        }
        
        // Zoom with scroll wheel
        distance -= Input.GetAxis("Mouse ScrollWheel") * zoomSpeed;
        distance = Mathf.Clamp(distance, minDistance, maxDistance);
        
        // Calculate position
        Quaternion rotation = Quaternion.Euler(currentY, currentX, 0);
        Vector3 position = rotation * new Vector3(0, 0, -distance) + target.position;
        
        // Apply
        transform.position = position;
        transform.LookAt(target.position);
    }
}