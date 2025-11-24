using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

public class MotionBlurRenderFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class MotionBlurSettings
    {
        [Range(0f, 0.99f)]
        public float intensity = 0.8f;

        public RenderPassEvent injectionPoint = RenderPassEvent.BeforeRenderingPostProcessing;
    }

    public MotionBlurSettings settings = new MotionBlurSettings();
    private MotionBlurPass motionBlurPass;
    private Material motionBlurMaterial;

    public override void Create()
    {
        Shader shader = Shader.Find("Hidden/TemporalMotionBlur");
        if (shader == null)
        {
            Debug.LogError("Motion Blur Shader not found!");
            return;
        }

        motionBlurMaterial = CoreUtils.CreateEngineMaterial(shader);
        motionBlurPass = new MotionBlurPass(motionBlurMaterial, settings);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (motionBlurPass == null || motionBlurMaterial == null)
            return;

        if (renderingData.cameraData.cameraType == CameraType.Game ||
            renderingData.cameraData.cameraType == CameraType.SceneView)
        {
            motionBlurPass.ConfigureInput(ScriptableRenderPassInput.Color);
            renderer.EnqueuePass(motionBlurPass);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            motionBlurPass?.Dispose();
            CoreUtils.Destroy(motionBlurMaterial);
        }
    }

    private class MotionBlurPass : ScriptableRenderPass
    {
        private Material material;
        private MotionBlurSettings settings;
        private RTHandle accumulationTexture;

        private static readonly int BlendFactorID = Shader.PropertyToID("_BlendFactor");
        private static readonly int AccumulationTexID = Shader.PropertyToID("_AccumulationTex");

        public MotionBlurPass(Material mat, MotionBlurSettings config)
        {
            material = mat;
            settings = config;
            renderPassEvent = config.injectionPoint;
            profilingSampler = new ProfilingSampler("Motion Blur Effect");
        }

        private class PassData
        {
            internal Material material;
            internal float blendFactor;
            internal TextureHandle source;
            internal TextureHandle accumulation;
            internal TextureHandle output;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            RenderTextureDescriptor desc = renderingData.cameraData.cameraTargetDescriptor;
            desc.depthBufferBits = 0;
            desc.msaaSamples = 1;

            RenderingUtils.ReAllocateIfNeeded(
                ref accumulationTexture,
                desc,
                FilterMode.Bilinear,
                TextureWrapMode.Clamp,
                name: "_MotionBlurAccumulation"
            );
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (material == null || accumulationTexture == null)
                return;

            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

            if (!resourceData.isActiveTargetBackBuffer)
                return;

            TextureHandle sourceTexture = resourceData.activeColorTexture;

            RenderTextureDescriptor desc = cameraData.cameraTargetDescriptor;
            desc.depthBufferBits = 0;
            desc.msaaSamples = 1;

            TextureHandle accumulationHandle = renderGraph.ImportTexture(accumulationTexture);
            TextureHandle outputHandle = UniversalRenderer.CreateRenderGraphTexture(
                renderGraph,
                desc,
                "_MotionBlurOutput",
                false
            );

            using (var builder = renderGraph.AddRasterRenderPass<PassData>("Motion Blur Pass", out var passData, profilingSampler))
            {
                passData.material = material;
                passData.blendFactor = settings.intensity;
                passData.source = sourceTexture;
                passData.accumulation = accumulationHandle;
                passData.output = outputHandle;

                builder.UseTexture(sourceTexture, AccessFlags.Read);
                builder.UseTexture(accumulationHandle, AccessFlags.Read);
                builder.SetRenderAttachment(outputHandle, 0, AccessFlags.Write);

                builder.AllowPassCulling(false);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    data.material.SetFloat(BlendFactorID, data.blendFactor);
                    data.material.SetTexture(AccumulationTexID, data.accumulation);

                    Blitter.BlitTexture(context.cmd, data.source, new Vector4(1, 1, 0, 0), data.material, 0);
                });
            }

            using (var builder = renderGraph.AddRasterRenderPass<PassData>("Update Accumulation Buffer", out var passData))
            {
                passData.output = outputHandle;

                builder.UseTexture(outputHandle, AccessFlags.Read);
                builder.SetRenderAttachment(accumulationHandle, 0, AccessFlags.Write);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    Blitter.BlitTexture(context.cmd, data.output, new Vector4(1, 1, 0, 0), 0, false);
                });
            }

            using (var builder = renderGraph.AddRasterRenderPass<PassData>("Copy to Camera Target", out var passData))
            {
                passData.output = outputHandle;

                builder.UseTexture(outputHandle, AccessFlags.Read);
                builder.SetRenderAttachment(sourceTexture, 0, AccessFlags.Write);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    Blitter.BlitTexture(context.cmd, data.output, new Vector4(1, 1, 0, 0), 0, false);
                });
            }
        }

        public void Dispose()
        {
            accumulationTexture?.Release();
        }
    }
}
