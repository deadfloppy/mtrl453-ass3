using UnityEngine;

public class MockProjectionReceiver : MonoBehaviour, IProjectionInputReceiver
{
    public Transform previewAnchor;
    GameObject currentPreview;

    public void ApplyProjectionInput(Mesh mesh, float rpm, float helicoidSize)
    {
        Debug.Log($"[MOCK RECEIVER] mesh={mesh.name} rpm={rpm} helicoidSize={helicoidSize}");

        if (previewAnchor == null) previewAnchor = this.transform;

        if (currentPreview != null) Destroy(currentPreview);

        currentPreview = new GameObject("PreviewMesh");
        currentPreview.transform.SetParent(previewAnchor, false);

        var rot = currentPreview.AddComponent<RotateY>();
        rot.rpm = rpm;   // use the rpm the user selected

        var mf = currentPreview.AddComponent<MeshFilter>();
        mf.sharedMesh = mesh;

        var mr = currentPreview.AddComponent<MeshRenderer>();
        mr.sharedMaterial = new Material(Shader.Find("Standard"));

        // ---- AUTO SCALE + CENTER ----
        // Get mesh bounds in its own space
        var bounds = mesh.bounds;
        float maxDim = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
        if (maxDim < 1e-6f) maxDim = 1f;

        // Scale so largest dimension becomes ~1 Unity unit, then multiply by helicoidSize
        float autoScale = 1f / maxDim;

        currentPreview.transform.localScale = Vector3.one * autoScale * helicoidSize;

        // Center the mesh on the anchor
        currentPreview.transform.localPosition = -bounds.center * autoScale * helicoidSize;

        Debug.Log("MockReceiver: ApplyProjectionInput was called!");
    }
}
