using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;
using System.Collections.Generic;

public class BloomRenderFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
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
        public bool showBloomOnly = false;

        public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
    }

    public Settings settings = new Settings();
    private BloomRenderPass renderPass;

    public override void Create()
    {
        renderPass = new BloomRenderPass(settings);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (renderPass == null) return;

        // Update settings in case they changed
        renderPass.UpdateSettings(settings);

        // Only add the pass if we're rendering a game or scene camera
        if (renderingData.cameraData.cameraType == CameraType.Game ||
            renderingData.cameraData.cameraType == CameraType.SceneView)
        {
            renderer.EnqueuePass(renderPass);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && renderPass != null)
        {
            renderPass.Dispose();
        }
    }
}

public class BloomRenderPass : ScriptableRenderPass
{
    private BloomRenderFeature.Settings settings;
    private Material bloomMaterial;

    private const string ProfilerTag = "Bloom";
    private ProfilingSampler profilingSampler;

    private RTHandle[] blurBuffers;
    private const int MaxBlurIterations = 4;

    public BloomRenderPass(BloomRenderFeature.Settings settings)
    {
        this.settings = settings;
        renderPassEvent = settings.renderPassEvent;
        profilingSampler = new ProfilingSampler(ProfilerTag);

        blurBuffers = new RTHandle[MaxBlurIterations * 2]; // *2 for horizontal and vertical

        // Load shader
        Shader shader = Shader.Find("Hidden/SimpleBloom");
        if (shader != null)
        {
            bloomMaterial = CoreUtils.CreateEngineMaterial(shader);
        }
        else
        {
            Debug.LogError("Bloom shader 'Hidden/SimpleBloom' not found!");
        }
    }

    public void UpdateSettings(BloomRenderFeature.Settings newSettings)
    {
        this.settings = newSettings;
        renderPassEvent = settings.renderPassEvent;
    }

    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
        // No persistent setup needed - we'll allocate temporary buffers in Execute
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        if (bloomMaterial == null)
        {
            Debug.LogWarning("Bloom material is null. Skipping pass.");
            return;
        }

        CommandBuffer cmd = CommandBufferPool.Get(ProfilerTag);

        using (new ProfilingScope(cmd, profilingSampler))
        {
            // Get camera color target
            RTHandle cameraColorTarget = renderingData.cameraData.renderer.cameraColorTargetHandle;

            var descriptor = renderingData.cameraData.cameraTargetDescriptor;
            descriptor.depthBufferBits = 0;

            int width = descriptor.width / 2;
            int height = descriptor.height / 2;

            var downscaleDescriptor = descriptor;
            downscaleDescriptor.width = width;
            downscaleDescriptor.height = height;

            // Allocate bright pass buffer
            RTHandle brightPass = RTHandles.Alloc(
                width, height, 1,
                DepthBits.None,
                descriptor.graphicsFormat,
                FilterMode.Bilinear,
                TextureWrapMode.Clamp,
                dimension: TextureDimension.Tex2D,
                name: "_BloomBrightPass"
            );

            // Pass 0: Extract bright pixels
            bloomMaterial.SetFloat("_Threshold", settings.threshold);
            Blitter.BlitCameraTexture(cmd, cameraColorTarget, brightPass, bloomMaterial, 0);

            RTHandle current = brightPass;

            // Blur passes
            for (int i = 0; i < settings.iterations; i++)
            {
                // Horizontal blur
                RTHandle horizontalBlur = RTHandles.Alloc(
                    width, height, 1,
                    DepthBits.None,
                    descriptor.graphicsFormat,
                    FilterMode.Bilinear,
                    TextureWrapMode.Clamp,
                    dimension: TextureDimension.Tex2D,
                    name: $"_BloomHorizontal{i}"
                );

                bloomMaterial.SetVector("_BlurOffset", new Vector2(settings.blur / width, 0));
                Blitter.BlitCameraTexture(cmd, current, horizontalBlur, bloomMaterial, 1);

                // Release previous buffer if not the bright pass
                if (i > 0 || current != brightPass)
                {
                    if (current != brightPass)
                    {
                        current.Release();
                    }
                }

                // Vertical blur
                RTHandle verticalBlur = RTHandles.Alloc(
                    width, height, 1,
                    DepthBits.None,
                    descriptor.graphicsFormat,
                    FilterMode.Bilinear,
                    TextureWrapMode.Clamp,
                    dimension: TextureDimension.Tex2D,
                    name: $"_BloomVertical{i}"
                );

                bloomMaterial.SetVector("_BlurOffset", new Vector2(0, settings.blur / height));
                Blitter.BlitCameraTexture(cmd, horizontalBlur, verticalBlur, bloomMaterial, 1);

                horizontalBlur.Release();
                current = verticalBlur;

                // Store for cleanup
                blurBuffers[i] = current;
            }

            // Final combine
            bloomMaterial.SetTexture("_BloomTex", current);
            bloomMaterial.SetFloat("_Intensity", settings.intensity);

            // Pass 2 = combine, Pass 3 = bloom only (for debug)
            int passToUse = settings.showBloomOnly ? 3 : 2;

            // Create temporary target for final output
            RTHandle finalTarget = RTHandles.Alloc(
                descriptor.width, descriptor.height, 1,
                DepthBits.None,
                descriptor.graphicsFormat,
                FilterMode.Bilinear,
                TextureWrapMode.Clamp,
                dimension: TextureDimension.Tex2D,
                name: "_BloomFinal"
            );

            Blitter.BlitCameraTexture(cmd, cameraColorTarget, finalTarget, bloomMaterial, passToUse);
            Blitter.BlitCameraTexture(cmd, finalTarget, cameraColorTarget);

            // Cleanup
            finalTarget.Release();
            brightPass.Release();
            for (int i = 0; i < settings.iterations; i++)
            {
                if (blurBuffers[i] != null)
                {
                    blurBuffers[i].Release();
                    blurBuffers[i] = null;
                }
            }
        }

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    public void Dispose()
    {
        if (bloomMaterial != null)
        {
            CoreUtils.Destroy(bloomMaterial);
            bloomMaterial = null;
        }

        // Cleanup any remaining buffers
        for (int i = 0; i < blurBuffers.Length; i++)
        {
            if (blurBuffers[i] != null)
            {
                blurBuffers[i].Release();
                blurBuffers[i] = null;
            }
        }
    }

    public override void OnCameraCleanup(CommandBuffer cmd)
    {
        // Cleanup if needed
    }

    // RenderGraph implementation (Unity 2022.2+)
    private class PassData
    {
        internal Material bloomMaterial;
        internal float threshold;
        internal float intensity;
        internal float blur;
        internal bool showBloomOnly;
        internal int iterations;
        internal TextureHandle source;
        internal TextureHandle bloomTexture;
        internal List<TextureHandle> blurTextures;
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        if (bloomMaterial == null)
        {
            Debug.LogWarning("Bloom material is null. Skipping pass.");
            return;
        }

        UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
        UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

        var source = resourceData.activeColorTexture;
        var descriptor = cameraData.cameraTargetDescriptor;
        descriptor.depthBufferBits = 0;

        int width = descriptor.width / 2;
        int height = descriptor.height / 2;

        var downscaleDescriptor = descriptor;
        downscaleDescriptor.width = width;
        downscaleDescriptor.height = height;

        // Create bright pass texture
        TextureHandle brightPass = UniversalRenderer.CreateRenderGraphTexture(renderGraph, downscaleDescriptor, "_BloomBrightPass", false);

        // Bright pass extraction
        using (var builder = renderGraph.AddRasterRenderPass<PassData>("Bloom Bright Pass", out var passData, profilingSampler))
        {
            passData.bloomMaterial = bloomMaterial;
            passData.threshold = settings.threshold;
            passData.source = source;

            builder.UseTexture(source);
            builder.SetRenderAttachment(brightPass, 0);

            builder.AllowPassCulling(false);
            builder.AllowGlobalStateModification(true);

            builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
            {
                data.bloomMaterial.SetFloat("_Threshold", data.threshold);
                context.cmd.SetGlobalTexture("_MainTex", data.source);
                context.cmd.DrawProcedural(Matrix4x4.identity, data.bloomMaterial, 0, MeshTopology.Triangles, 3, 1);
            });
        }

        TextureHandle current = brightPass;
        List<TextureHandle> tempTextures = new List<TextureHandle>();

        // Blur iterations
        for (int i = 0; i < settings.iterations; i++)
        {
            // Horizontal blur
            TextureHandle horizontalBlur = UniversalRenderer.CreateRenderGraphTexture(renderGraph, downscaleDescriptor, $"_BloomHorizontal{i}", false);

            using (var builder = renderGraph.AddRasterRenderPass<PassData>($"Bloom Horizontal Blur {i}", out var passData))
            {
                passData.bloomMaterial = bloomMaterial;
                passData.blur = settings.blur;
                passData.source = current;

                builder.UseTexture(current);
                builder.SetRenderAttachment(horizontalBlur, 0);

                int capturedWidth = width;
                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    data.bloomMaterial.SetVector("_BlurOffset", new Vector2(data.blur / capturedWidth, 0));
                    context.cmd.SetGlobalTexture("_MainTex", data.source);
                    context.cmd.DrawProcedural(Matrix4x4.identity, data.bloomMaterial, 1, MeshTopology.Triangles, 3, 1);
                });
            }

            // Vertical blur
            TextureHandle verticalBlur = UniversalRenderer.CreateRenderGraphTexture(renderGraph, downscaleDescriptor, $"_BloomVertical{i}", false);

            using (var builder = renderGraph.AddRasterRenderPass<PassData>($"Bloom Vertical Blur {i}", out var passData))
            {
                passData.bloomMaterial = bloomMaterial;
                passData.blur = settings.blur;
                passData.source = horizontalBlur;

                builder.UseTexture(horizontalBlur);
                builder.SetRenderAttachment(verticalBlur, 0);

                int capturedHeight = height;
                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    data.bloomMaterial.SetVector("_BlurOffset", new Vector2(0, data.blur / capturedHeight));
                    context.cmd.SetGlobalTexture("_MainTex", data.source);
                    context.cmd.DrawProcedural(Matrix4x4.identity, data.bloomMaterial, 1, MeshTopology.Triangles, 3, 1);
                });
            }

            current = verticalBlur;
            tempTextures.Add(verticalBlur);
        }

        // Final combine - we need a temp texture because we can't read and write to source
        TextureHandle combinedOutput = UniversalRenderer.CreateRenderGraphTexture(renderGraph, descriptor, "_BloomCombined", false);

        using (var builder = renderGraph.AddRasterRenderPass<PassData>("Bloom Combine", out var passData, profilingSampler))
        {
            passData.bloomMaterial = bloomMaterial;
            passData.intensity = settings.intensity;
            passData.showBloomOnly = settings.showBloomOnly;
            passData.source = source;
            passData.bloomTexture = current;

            builder.UseTexture(source);
            builder.UseTexture(current);
            builder.SetRenderAttachment(combinedOutput, 0);

            builder.AllowPassCulling(false);
            builder.AllowGlobalStateModification(true);

            builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
            {
                // Set material properties
                data.bloomMaterial.SetFloat("_Intensity", data.intensity);

                // Set textures using command buffer (allowed with AllowGlobalStateModification)
                context.cmd.SetGlobalTexture("_MainTex", data.source);
                context.cmd.SetGlobalTexture("_BloomTex", data.bloomTexture);

                int passToUse = data.showBloomOnly ? 3 : 2;
                context.cmd.DrawProcedural(Matrix4x4.identity, data.bloomMaterial, passToUse, MeshTopology.Triangles, 3, 1);
            });
        }

        // Copy combined result back to source
        using (var builder = renderGraph.AddRasterRenderPass<PassData>("Bloom Copy to Output", out var passData))
        {
            passData.source = combinedOutput;

            builder.UseTexture(combinedOutput);
            builder.SetRenderAttachment(source, 0);

            builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
            {
                Blitter.BlitTexture(context.cmd, data.source, new Vector4(1, 1, 0, 0), 0, false);
            });
        }
    }
}
