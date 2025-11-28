using UnityEngine;

public class HelicoidProjectionReceiver : MonoBehaviour, IProjectionInputReceiver
{
    [Header("Hook these up in the Inspector")]
    [SerializeField] private HelicoidModelController modelController;
    [SerializeField] private MeshFilter displayMeshFilter;
    [SerializeField] private Camera targetCamera;

    [Header("Size mapping")]
    [Tooltip("World radius the object should roughly fit into when size slider = 1")]
    public float targetRadiusAtSize1 = 0.3f;

    Mesh currentMesh;
    float baseScale = 1f;

    public void ApplyProjectionInput(Mesh mesh, float rpm, float helicoidSize)
    {
        if (modelController == null)
        {
            Debug.LogError("HelicoidProjectionReceiver: modelController not assigned");
            return;
        }

        // New mesh? Normalize it once and frame the camera
        if (mesh != null && mesh != currentMesh && displayMeshFilter != null)
        {
            currentMesh = mesh;

            // Center the mesh around its local origin so it spins around its own axis
            CenterMesh(currentMesh);

            displayMeshFilter.sharedMesh = currentMesh;

            ComputeBaseScaleForMesh(currentMesh);
            ApplySize(helicoidSize);
            FrameCameraOnce();
        }

        else
        {
            // Same mesh, just update size
            ApplySize(helicoidSize);
        }

        // RPM always updates
        modelController.rotationSpeed = rpm;
    }

    void CenterMesh(Mesh mesh)
    {
        if (mesh == null) return;

        // Get current bounds (local space)
        var bounds = mesh.bounds;
        var center = bounds.center;

        // Shift all vertices so the bounds center moves to (0,0,0)
        var verts = mesh.vertices;
        for (int i = 0; i < verts.Length; i++)
        {
            verts[i] -= center;
        }

        mesh.vertices = verts;
        mesh.RecalculateBounds();
    }

    void ComputeBaseScaleForMesh(Mesh mesh)
    {
        if (displayMeshFilter == null || mesh == null) return;

        var bounds = mesh.bounds;
        float meshRadius = bounds.extents.magnitude;
        if (meshRadius <= 0f) { baseScale = 1f; return; }

        float targetRadius = Mathf.Max(0.01f, targetRadiusAtSize1);
        baseScale = targetRadius / meshRadius;
    }

    void ApplySize(float helicoidSize)
    {
        if (displayMeshFilter == null) return;

        float s = baseScale * Mathf.Max(0.01f, helicoidSize);
        displayMeshFilter.transform.localScale = Vector3.one * s;
    }

    void FrameCameraOnce()
    {
        if (targetCamera == null || displayMeshFilter == null) return;

        var renderer = displayMeshFilter.GetComponent<Renderer>();
        if (renderer == null) return;

        Bounds b = renderer.bounds;
        Vector3 center = b.center;
        float radius = b.extents.magnitude;

        float fovRad = targetCamera.fieldOfView * Mathf.Deg2Rad;
        float dist = radius / Mathf.Sin(fovRad * 0.5f) * 1.3f;

        Vector3 camDir = targetCamera.transform.forward.normalized;
        targetCamera.transform.position = center - camDir * dist;
        targetCamera.transform.LookAt(center);
    }
}
