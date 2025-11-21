using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class SimpleBloom : MonoBehaviour
{
    [Header("Bloom Settings")]
    [Range(0f, 10f)]
    public float intensity = 2.0f;
    
    [Range(0f, 1f)]
    public float threshold = 0.5f;
    
    [Range(1, 4)]
    public int iterations = 2;
    
    [Range(0.5f, 4f)]
    public float blur = 2.0f;
    
    [Header("Debug")]
    public bool showBloomOnly = false; // Toggle to see only the bloom effect
    
    private Material bloomMaterial;
    private Shader bloomShader;
    
    void Start()
    {
        bloomShader = Shader.Find("Hidden/SimpleBloom");
        if (bloomShader == null)
        {
            Debug.LogError("Bloom shader not found! Make sure SimpleBloom.shader is in your project.");
            enabled = false;
            return;
        }
        
        bloomMaterial = new Material(bloomShader);
        bloomMaterial.hideFlags = HideFlags.HideAndDontSave;
    }
    
    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (bloomMaterial == null)
        {
            Graphics.Blit(source, destination);
            return;
        }
        
        int width = source.width / 2;
        int height = source.height / 2;
        
        // Pass 0: Extract bright pixels
        bloomMaterial.SetFloat("_Threshold", threshold);
        RenderTexture brightPass = RenderTexture.GetTemporary(width, height, 0, source.format);
        Graphics.Blit(source, brightPass, bloomMaterial, 0);
        
        // Blur passes
        RenderTexture current = brightPass;
        for (int i = 0; i < iterations; i++)
        {
            RenderTexture temp = RenderTexture.GetTemporary(width, height, 0, source.format);
            
            // Horizontal blur
            bloomMaterial.SetVector("_BlurOffset", new Vector2(blur / width, 0));
            Graphics.Blit(current, temp, bloomMaterial, 1);
            
            RenderTexture.ReleaseTemporary(current);
            current = temp;
            
            // Vertical blur
            temp = RenderTexture.GetTemporary(width, height, 0, source.format);
            bloomMaterial.SetVector("_BlurOffset", new Vector2(0, blur / height));
            Graphics.Blit(current, temp, bloomMaterial, 1);
            
            RenderTexture.ReleaseTemporary(current);
            current = temp;
        }
        
        // Final combine
        bloomMaterial.SetTexture("_BloomTex", current);
        bloomMaterial.SetFloat("_Intensity", intensity);
        
        // Pass 2 = combine, Pass 3 = bloom only (for debug)
        int passToUse = showBloomOnly ? 3 : 2;
        Graphics.Blit(source, destination, bloomMaterial, passToUse);
        
        RenderTexture.ReleaseTemporary(current);
    }
    
    void OnDisable()
    {
        if (bloomMaterial != null)
        {
            DestroyImmediate(bloomMaterial);
        }
    }
}