using UnityEngine;

public class SliceTextureGenerator : MonoBehaviour
{
    [Header("Generation Settings")]
    public int textureSize = 256;
    public int numberOfSlices = 32;
    public string savePath = "Assets/Textures/Slices/";
    
    [ContextMenu("Generate Test Sphere Slices")]
    public void GenerateTestSphereSlices()
    {
        for (int i = 0; i < numberOfSlices; i++)
        {
            Texture2D slice = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
            
            float z = (i / (float)(numberOfSlices - 1)) * 2f - 1f; // -1 to 1
            
            for (int y = 0; y < textureSize; y++)
            {
                for (int x = 0; x < textureSize; x++)
                {
                    float u = (x / (float)textureSize) * 2f - 1f; // -1 to 1
                    float v = (y / (float)textureSize) * 2f - 1f; // -1 to 1
                    
                    float distance = Mathf.Sqrt(u * u + v * v + z * z);
                    
                    // Create a sphere
                    if (distance < 0.8f)
                    {
                        float intensity = 1f - (distance / 0.8f);
                        Color color = Color.Lerp(Color.blue, Color.red, (z + 1f) * 0.5f);
                        color.a = intensity;
                        slice.SetPixel(x, y, color);
                    }
                    else
                    {
                        slice.SetPixel(x, y, Color.clear);
                    }
                }
            }
            
            slice.Apply();
            
            // Save as PNG
            byte[] bytes = slice.EncodeToPNG();
            string filename = $"{savePath}slice_{i:D3}.png";
            System.IO.File.WriteAllBytes(filename, bytes);
            
            Debug.Log($"Generated {filename}");
        }
        
        #if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
        #endif
    }
}