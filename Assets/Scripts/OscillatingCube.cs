using UnityEngine;

public class OscillatingCube : MonoBehaviour
{
    [Header("Oscillation Settings")]
    public float amplitude = 5f; // Distance from center
    public float speed = 1f; // Speed of oscillation (frequency)

    [Header("Axis")]
    public bool oscillateX = true;
    public bool oscillateY = false;
    public bool oscillateZ = false;

    private Vector3 startPosition;
    private float time;

    void Start()
    {
        startPosition = transform.position;
    }

    void Update()
    {
        time += Time.deltaTime * speed;

        // Calculate oscillation offset using sine wave
        float offset = Mathf.Sin(time) * amplitude;

        // Apply to selected axes
        Vector3 newPosition = startPosition;

        if (oscillateX)
            newPosition.x += offset;

        if (oscillateY)
            newPosition.y += offset;

        if (oscillateZ)
            newPosition.z += offset;

        transform.position = newPosition;
    }
}
