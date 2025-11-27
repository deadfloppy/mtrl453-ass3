using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class OBJImporter : MonoBehaviour
{
    [Header("OBJ filename inside StreamingAssets")]
    public string objFileName = "model.obj";
    [Header("Model Scaling")]
    public float modelScaleFactor = 0.001f;

    private GameObject loadedModel;

    void Start()
    {
        LoadModel();
    }

    [ContextMenu("Reload Model")]
    public void LoadModel()
    {
        // Destroy previous model if it exists
        if (loadedModel != null)
        {
            Destroy(loadedModel);
        }

        string path = Path.Combine(Application.streamingAssetsPath, objFileName);
        Debug.Log("Loading OBJ: " + path + " with scale: " + modelScaleFactor);

        if (File.Exists(path))
            LoadOBJ(path);
        else
            Debug.LogError("OBJ file not found at: " + path);
    }

    void LoadOBJ(string path)
    {
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
                    float.Parse(p[1]) * modelScaleFactor,
                    float.Parse(p[2]) * modelScaleFactor,
                    float.Parse(p[3]) * modelScaleFactor
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

        loadedModel = new GameObject(Path.GetFileNameWithoutExtension(path));
        var mf = loadedModel.AddComponent<MeshFilter>();
        var mr = loadedModel.AddComponent<MeshRenderer>();
        var controller = loadedModel.AddComponent<HelicoidModelController>();

        mf.mesh = mesh;
        mr.material = new Material(Shader.Find("Standard"));

        Debug.Log($"Model loaded successfully with {verts.Count} vertices");
    }
}