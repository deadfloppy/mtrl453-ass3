using UnityEngine;

public class POVPersistence : MonoBehaviour
{
    public Material povMaterial;
    public float persistence = 0.9f; // 0.0 = no persistence, 1.0 = long blur

    RenderTexture accumulation;

    void Start()
    {
        accumulation = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBHalf);
        accumulation.filterMode = FilterMode.Bilinear;
        Graphics.Blit(Texture2D.blackTexture, accumulation);
    }

    void OnRenderImage(RenderTexture src, RenderTexture dst)
    {
        povMaterial.SetFloat("_Persistence", persistence);

        // feed the previous accumulation into the shader
        povMaterial.SetTexture("_HistoryTex", accumulation);

        // write output into a temp texture
        RenderTexture temp = RenderTexture.GetTemporary(src.width, src.height, 0, src.format);

        Graphics.Blit(src, temp, povMaterial);

        // copy temp back into accumulation
        Graphics.Blit(temp, accumulation);

        // output the new frame
        Graphics.Blit(temp, dst);

        RenderTexture.ReleaseTemporary(temp);
    }
}