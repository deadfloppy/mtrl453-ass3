using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

//
// HDRP CustomPass implementing a temporal accumulation (POV persistence).
// Targets Unity 2022/2023/2024+ HDRP APIs (should work in Unity 6 HDRP with small version differences).
//
class POVPersistencePass : CustomPass
{
    public Material povMaterial = null;
    [Range(0f, 0.999f)] public float persistence = 0.9f;
    [Header("Debug")]
    public bool showPersistenceBuffer = false;

    // persistent accumulation RT
    private RenderTexture accumulation = null;

    protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
    {
        // create accumulation RT sized to screen
        if (accumulation == null)
        {
            var desc = new RenderTextureDescriptor(Screen.width, Screen.height, RenderTextureFormat.ARGBHalf, 0);
            desc.sRGB = (QualitySettings.activeColorSpace == ColorSpace.Linear) ? false : true; // keep it simple
            accumulation = new RenderTexture(desc);
            accumulation.name = "POV_Accumulation";
            accumulation.Create();

            // initialize to black
            cmd.SetRenderTarget(accumulation);
            cmd.ClearRenderTarget(false, true, Color.black);
        }
    }

    protected override void Execute(CustomPassContext ctx)
    {
        if (povMaterial == null || accumulation == null)
            return;

        // ensure accumulation matches camera size (resize when resolution changes)
        int width = ctx.cameraColorBuffer.referenceSize.x;
        int height = ctx.cameraColorBuffer.referenceSize.y;
        if (accumulation.width != width || accumulation.height != height)
        {
            accumulation.Release();
            var desc = new RenderTextureDescriptor(width, height, RenderTextureFormat.ARGBHalf, 0);
            desc.sRGB = (QualitySettings.activeColorSpace == ColorSpace.Linear) ? false : true;
            accumulation = new RenderTexture(desc) { name = "POV_Accumulation" };
            accumulation.Create();
            // clear
            ctx.cmd.SetRenderTarget(accumulation);
            ctx.cmd.ClearRenderTarget(false, true, Color.black);
        }

        // set shader params
        povMaterial.SetFloat("_Persistence", persistence);
        povMaterial.SetTexture("_HistoryTex", accumulation);

        // Create a temporary RT
        int tempID = Shader.PropertyToID("_POVTempRT");
        ctx.cmd.GetTemporaryRT(tempID, width, height, 0, FilterMode.Bilinear, RenderTextureFormat.ARGBHalf);

        // Note: ctx.cameraColorBuffer is an RTHandle; it works as a source RenderTargetIdentifier
        RenderTargetIdentifier cameraColor = ctx.cameraColorBuffer;

        // 1) Render: source (camera buffer) -> temp using povMaterial (the shader will read _HistoryTex)
        ctx.cmd.Blit(cameraColor, tempID, povMaterial);

        // 2) Copy temp -> accumulation (so next frame has history)
        ctx.cmd.Blit(tempID, accumulation);

        // 3) Copy temp -> camera final buffer (so user sees the blended result)
        // Or show the persistence buffer directly if debug mode is enabled
        if (showPersistenceBuffer)
        {
            ctx.cmd.Blit(accumulation, cameraColor);
        }
        else
        {
            ctx.cmd.Blit(tempID, cameraColor);
        }

        // release temp
        ctx.cmd.ReleaseTemporaryRT(tempID);
    }

    protected override void Cleanup()
    {
        if (accumulation != null)
        {
            accumulation.Release();
            accumulation = null;
        }
    }
}