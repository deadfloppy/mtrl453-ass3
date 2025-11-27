using UnityEngine;

public class HelicoidModelController : MonoBehaviour
{
    [Header("Rotation Properties")]
    public float rotationSpeed = 1440f; // RPM (revolutions per minute)

    [Header("Material Properties")]
    public Material modelMaterial;


    void Start()
    {
        // If no material is assigned, load the default HelicoidMaterial
        if (modelMaterial == null)
        {
            modelMaterial = Resources.Load<Material>("Materials/Helicoid/HelicoidMaterial");
            if (modelMaterial == null)
            {
                Debug.LogWarning("HelicoidMaterial not found in Resources. Make sure it's in Assets/Resources/Materials/Helicoid/");
            }
        }

        // Apply the material to the MeshRenderer
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.material = modelMaterial;
        }
    }

    void Update()
    {
        // Convert RPM to degrees per second
        // RPM * 360 degrees per rotation / 60 seconds per minute = degrees per second
        float degreesPerSecond = rotationSpeed * 360f / 60f;

        // Rotate around the Y-axis (up)
        transform.Rotate(0f, degreesPerSecond * Time.deltaTime, 0f);
    }
}
