using EPOOutline;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

/// <summary>Unity 6 bridge for EPO's mask/dilate/composite implementation. Render Graph remains enabled.</summary>
public sealed class URPOutlineFeature : ScriptableRendererFeature
{
    private sealed class OutlinePass : ScriptableRenderPass
    {
        private sealed class PassData
        {
            internal TextureHandle Color, Depth;
            internal Outliner Outliner;
            internal OutlineParameters Parameters;
            internal Camera Camera;
            internal RenderTextureDescriptor Descriptor;
        }
        private readonly OutlineParameters parameters = new OutlineParameters();
        internal Outliner Outliner;

        internal OutlinePass()
        {
            renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
            requiresIntermediateTexture = true;
        }
        public override void RecordRenderGraph(RenderGraph graph, ContextContainer frameData)
        {
            var resources = frameData.Get<UniversalResourceData>();
            var camera = frameData.Get<UniversalCameraData>();
            using var builder = graph.AddUnsafePass<PassData>("EPO / thin object contours", out var data);
            data.Color = resources.activeColorTexture;
            data.Depth = resources.activeDepthTexture;
            data.Outliner = Outliner;
            data.Parameters = parameters;
            data.Camera = camera.camera;
            data.Descriptor = camera.cameraTargetDescriptor;
            builder.UseTexture(data.Color, AccessFlags.ReadWrite);
            builder.UseTexture(data.Depth, AccessFlags.ReadWrite);
            builder.AllowGlobalStateModification(true);
            builder.SetRenderFunc(static (PassData d, UnsafeGraphContext context) =>
            {
                var p = d.Parameters;
                Outlinable.GetAllActiveOutlinables(d.Camera, p.OutlinablesToRender);
                RendererFilteringUtility.Filter(d.Camera, p);
                d.Outliner.UpdateSharedParameters(p, d.Camera, false);
                if (p.OutlinablesToRender.Count == 0) return;
                p.Buffer = CommandBufferHelpers.GetNativeCommandBuffer(context.cmd);
                RTHandle color = d.Color, depth = d.Depth;
                p.Target = RenderTargetUtility.ComposeTarget(p, color.nameID);
                p.DepthTarget = RenderTargetUtility.ComposeTarget(p, depth.nameID);
                p.TargetWidth = d.Descriptor.width;
                p.TargetHeight = d.Descriptor.height;
                p.Antialiasing = d.Descriptor.msaaSamples;
                OutlineEffect.SetupOutline(p);
                p.BlitMesh = null;
                p.MeshPool.ReleaseAllMeshes();
            });
        }
    }
    private OutlinePass pass;
    public override void Create() => pass = new OutlinePass();
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData data)
    {
        if (data.cameraData.cameraType != CameraType.Game ||
            !data.cameraData.camera.TryGetComponent<Outliner>(out var outliner) || !outliner.isActiveAndEnabled) return;
        pass.Outliner = outliner;
        renderer.EnqueuePass(pass);
    }
}
