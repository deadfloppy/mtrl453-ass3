using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class SimpleMotionBlur : MonoBehaviour
{
    [Header("Motion Blur Settings")]
    [Range(0f, 0.95f)]
    public float blurAmount = 0.7f; // How much previous frames contribute
    
    [Header("Visualization Mode")]
    public bool useEnhancedMode = false; // Link to helicoid's visualization mode
    [Range(0.5f, 0.99f)]
    public float enhancedBlurAmount = 0.95f; // Extreme blur for visualization
    
    private RenderTexture accumulationBuffer;
    private Material blurMaterial;
    private HelicoidVolumetricDisplay helicoidDisplay;
    
    void Start()
    {
        Shader shader = Shader.Find("Hidden/SimpleMotionBlur");
        if (shader == null)
        {
            Debug.LogError("Motion blur shader not found!");
            enabled = false;
            return;
        }
        
        blurMaterial = new Material(shader);
        blurMaterial.hideFlags = HideFlags.HideAndDontSave;
    }
    
    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (blurMaterial == null)
        {
            Graphics.Blit(source, destination);
            return;
        }
        
        // Create accumulation buffer if needed
        if (accumulationBuffer == null || 
            accumulationBuffer.width != source.width || 
            accumulationBuffer.height != source.height)
        {
            if (accumulationBuffer != null)
                accumulationBuffer.Release();
                
            accumulationBuffer = new RenderTexture(source.width, source.height, 0, source.format);
            accumulationBuffer.hideFlags = HideFlags.HideAndDontSave;
            Graphics.Blit(source, accumulationBuffer);
        }
        
        // Use enhanced blur amount if in visualization mode
        float activeBlurAmount = useEnhancedMode ? enhancedBlurAmount : blurAmount;
        
        // Blend current frame with accumulated frames
        blurMaterial.SetTexture("_PrevTex", accumulationBuffer);
        blurMaterial.SetFloat("_BlurAmount", activeBlurAmount);
        
        RenderTexture temp = RenderTexture.GetTemporary(source.width, source.height, 0, source.format);
        Graphics.Blit(source, temp, blurMaterial);
        Graphics.Blit(temp, accumulationBuffer);
        Graphics.Blit(temp, destination);
        
        RenderTexture.ReleaseTemporary(temp);
    }
    
    void OnDisable()
    {
        if (accumulationBuffer != null)
        {
            accumulationBuffer.Release();
            DestroyImmediate(accumulationBuffer);
        }
        
        if (blurMaterial != null)
        {
            DestroyImmediate(blurMaterial);
        }
    }
}