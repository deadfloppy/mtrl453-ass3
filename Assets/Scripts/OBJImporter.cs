using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class OBJImporter : MonoBehaviour
{
    [Header("OBJ filename inside StreamingAssets")]
    public string objFileName = "model.obj";

    [Header("Model Scaling")]
    public float modelScaleFactor = 0.001f;

    [Header("Behaviour")]
    public bool loadOnStart = true;   // <- turn this OFF to prevent auto-load

    private GameObject loadedModel;

    void Start()
    {
        if (loadOnStart)
        {
            LoadModelFromStreamingAssets();
        }
    }

    [ContextMenu("Reload model from StreamingAssets")]
    public void LoadModelFromStreamingAssets()
    {
        string path = Path.Combine(Application.streamingAssetsPath, objFileName);
        LoadModelAtPath(path);
    }

    /// <summary>
    /// Called by your UI after the user picks a file.
    /// </summary>
    public void LoadModelAtPath(string path)
    {
        if (!File.Exists(path))
        {
            Debug.LogError($"OBJImporter: OBJ file not found at: {path}");
            return;
        }

        // Destroy previous model if it exists
        if (loadedModel != null)
        {
            Destroy(loadedModel);
            loadedModel = null;
        }

        Mesh mesh = LoadMeshFromFile(path, modelScaleFactor);
        if (mesh == null)
        {
            Debug.LogError("OBJImporter: failed to load mesh.");
            return;
        }

        loadedModel = new GameObject(Path.GetFileNameWithoutExtension(path));
        var mf = loadedModel.AddComponent<MeshFilter>();
        var mr = loadedModel.AddComponent<MeshRenderer>();
        var controller = loadedModel.AddComponent<HelicoidModelController>();

        mf.sharedMesh = mesh;
        mr.material = new Material(Shader.Find("Standard"));

        Debug.Log($"OBJImporter: model loaded successfully with {mesh.vertexCount} vertices");
    }

    /// <summary>
    /// Pure utility: parses an OBJ file and returns a Mesh.
    /// You can call this directly from your UI file loader if you want.
    /// </summary>
    public static Mesh LoadMeshFromFile(string path, float scaleFactor = 0.001f)
    {
        if (!File.Exists(path))
        {
            Debug.LogError($"OBJImporter.LoadMeshFromFile: file does not exist: {path}");
            return null;
        }

        List<Vector3> verts = new();
        List<Vector2> uvs = new();
        List<Vector3> norms = new();
        List<int> triangles = new();

        string[] lines = File.ReadAllLines(path);

        foreach (string l in lines)
        {
            string line = l.Trim();

            if (line.StartsWith("v "))
            {
                string[] p = line.Split(' ');
                verts.Add(new Vector3(
                    float.Parse(p[1]) * scaleFactor,
                    float.Parse(p[2]) * scaleFactor,
                    float.Parse(p[3]) * scaleFactor
                ));
            }
            else if (line.StartsWith("vt "))
            {
                string[] p = line.Split(' ');
                uvs.Add(new Vector2(
                    float.Parse(p[1]),
                    float.Parse(p[2])
                ));
            }
            else if (line.StartsWith("vn "))
            {
                string[] p = line.Split(' ');
                norms.Add(new Vector3(
                    float.Parse(p[1]),
                    float.Parse(p[2]),
                    float.Parse(p[3])
                ));
            }
            else if (line.StartsWith("f "))
            {
                // faces: f v/vt/vn v/vt/vn v/vt/vn
                string[] p = line.Substring(2).Split(' ');

                foreach (var face in p)
                {
                    string[] comp = face.Split('/');
                    int vertIndex = int.Parse(comp[0]) - 1;
                    triangles.Add(vertIndex);
                }
            }
        }

        Mesh mesh = new Mesh();
        mesh.SetVertices(verts);
        mesh.SetTriangles(triangles, 0);

        if (uvs.Count > 0)
            mesh.SetUVs(0, uvs);

        if (norms.Count > 0)
            mesh.SetNormals(norms);
        else
            mesh.RecalculateNormals();

        mesh.RecalculateBounds();

        return mesh;
    }
}
