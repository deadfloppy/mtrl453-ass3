using UnityEngine;

public class OrbitCamera : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float rotationSpeed = 5f;
    public int frameRateLimit = 60;

    private float currentX = 0f;
    private float currentY = 20f;

    void Start()
    {
        Application.targetFrameRate = frameRateLimit;
    }

    void LateUpdate()
    {
        // WASD movement
        float horizontal = Input.GetAxis("Horizontal"); // A/D keys
        float vertical = Input.GetAxis("Vertical");     // W/S keys

        if (horizontal != 0 || vertical != 0)
        {
            // Get camera's forward and right directions (flattened to XZ plane)
            Vector3 forward = transform.forward;
            forward.y = 0;
            forward.Normalize();

            Vector3 right = transform.right;
            right.y = 0;
            right.Normalize();

            // Calculate movement direction
            Vector3 moveDirection = forward * vertical + right * horizontal;

            // Move the camera directly
            transform.position += moveDirection * moveSpeed * Time.deltaTime;
        }

        // Rotate camera with mouse
        if (Input.GetMouseButton(0) || Input.GetMouseButton(1))
        {
            currentX += Input.GetAxis("Mouse X") * rotationSpeed;
            currentY -= Input.GetAxis("Mouse Y") * rotationSpeed;
            currentY = Mathf.Clamp(currentY, -89f, 89f);
        }

        // Apply rotation
        transform.rotation = Quaternion.Euler(currentY, currentX, 0);
    }
}