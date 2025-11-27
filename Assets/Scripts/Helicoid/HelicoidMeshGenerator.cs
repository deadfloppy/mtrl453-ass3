using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class HelicoidMeshGenerator : MonoBehaviour
{
    [Header("Helicoid Parameters")]
    public float radius = 1f;
    public float pitch = 2f; // Vertical distance per full rotation
    public int radialSegments = 64;
    public int heightSegments = 64;
    public int rotations = 2; // Number of full rotations
    
    void Start()
    {
        GenerateHelicoid();
    }
    
    public void GenerateHelicoid()
    {
        Mesh mesh = new Mesh();
        mesh.name = "Helicoid";
        
        int vertexCount = (radialSegments + 1) * (heightSegments + 1);
        Vector3[] vertices = new Vector3[vertexCount];
        Vector3[] normals = new Vector3[vertexCount];
        Vector2[] uvs = new Vector2[vertexCount];
        
        float totalHeight = pitch * rotations;
        
        // Generate vertices
        int vertIndex = 0;
        for (int h = 0; h <= heightSegments; h++)
        {
            float v = h / (float)heightSegments;
            float z = v * totalHeight - (totalHeight * 0.5f); // Center vertically
            float theta = v * rotations * 2f * Mathf.PI;
            
            for (int r = 0; r <= radialSegments; r++)
            {
                float u = r / (float)radialSegments;
                float rho = u * radius;
                
                float x = rho * Mathf.Cos(theta);
                float y = rho * Mathf.Sin(theta);
                
                vertices[vertIndex] = new Vector3(x, y, z);
                
                // Calculate normal (perpendicular to surface)
                Vector3 tangentTheta = new Vector3(-y, x, pitch / (2f * Mathf.PI));
                Vector3 tangentRho = new Vector3(Mathf.Cos(theta), Mathf.Sin(theta), 0);
                normals[vertIndex] = Vector3.Cross(tangentRho, tangentTheta).normalized;
                
                uvs[vertIndex] = new Vector2(u, v);
                
                vertIndex++;
            }
        }
        
        // Generate triangles
        int[] triangles = new int[radialSegments * heightSegments * 6];
        int triIndex = 0;
        
        for (int h = 0; h < heightSegments; h++)
        {
            for (int r = 0; r < radialSegments; r++)
            {
                int bottomLeft = h * (radialSegments + 1) + r;
                int bottomRight = bottomLeft + 1;
                int topLeft = bottomLeft + (radialSegments + 1);
                int topRight = topLeft + 1;
                
                // First triangle
                triangles[triIndex++] = bottomLeft;
                triangles[triIndex++] = topLeft;
                triangles[triIndex++] = bottomRight;
                
                // Second triangle
                triangles[triIndex++] = bottomRight;
                triangles[triIndex++] = topLeft;
                triangles[triIndex++] = topRight;
            }
        }
        
        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        
        mesh.RecalculateBounds();
        
        GetComponent<MeshFilter>().mesh = mesh;
    }
    
    void OnValidate()
    {
        if (Application.isPlaying)
        {
            GenerateHelicoid();
        }
    }
}