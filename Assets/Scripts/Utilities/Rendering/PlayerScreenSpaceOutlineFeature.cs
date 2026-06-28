using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

public sealed class PlayerScreenSpaceOutlineFeature : ScriptableRendererFeature
{
    static readonly int OutlineFadeId =
        Shader.PropertyToID("_PlayerOutlineFade");

    [Serializable]
    public sealed class Settings
    {
        [Tooltip("Every active renderer on these layers contributes to one combined silhouette.")]
        public LayerMask playerLayer = 1 << 7;

        [ColorUsage(true, true)]
        public Color outlineColor = new(0.02f, 0.03f, 0.04f, 1f);

        [Tooltip("Preserves the previous 0.035 setting, mapped to roughly 3.5 screen pixels.")]
        [Range(0.01f, 0.08f)]
        public float outlineWidth = 0.035f;

        [Range(1f, 32f)]
        public float ditherCellSize = 7f;

        public RenderPassEvent renderPassEvent =
            RenderPassEvent.AfterRenderingOpaques;

        [Tooltip("Assign PlayerSilhouetteMask.shader so it is included in builds.")]
        public Shader silhouetteMaskShader;

        [Tooltip("Assign PlayerScreenSpaceOutline.shader so it is included in builds.")]
        public Shader outlineShader;
    }

    [SerializeField]
    Settings settings = new();

    Material _silhouetteMaskMaterial;
    Material _outlineMaterial;
    PlayerOutlinePass _outlinePass;

    public override void Create()
    {
        CoreUtils.Destroy(_silhouetteMaskMaterial);
        CoreUtils.Destroy(_outlineMaterial);
        Shader.SetGlobalFloat(OutlineFadeId, 1f);

        Shader maskShader = settings.silhouetteMaskShader != null
            ? settings.silhouetteMaskShader
            : Shader.Find("Hidden/Summer Game/Player Silhouette Mask");

        Shader outlineShader = settings.outlineShader != null
            ? settings.outlineShader
            : Shader.Find("Hidden/Summer Game/Player Screen Space Outline");

        if (maskShader == null || outlineShader == null)
        {
            _outlinePass = null;
            return;
        }

        _silhouetteMaskMaterial =
            CoreUtils.CreateEngineMaterial(maskShader);

        _outlineMaterial =
            CoreUtils.CreateEngineMaterial(outlineShader);

        _outlinePass = new PlayerOutlinePass(
            _silhouetteMaskMaterial,
            _outlineMaterial);
    }

    public override void AddRenderPasses(
        ScriptableRenderer renderer,
        ref RenderingData renderingData)
    {
        if (_outlinePass == null ||
            renderingData.cameraData.cameraType == CameraType.Preview ||
            renderingData.cameraData.cameraType == CameraType.Reflection)
        {
            return;
        }

        _outlinePass.renderPassEvent = settings.renderPassEvent;
        _outlinePass.Setup(
            settings.playerLayer,
            settings.outlineColor,
            settings.outlineWidth,
            settings.ditherCellSize);

        renderer.EnqueuePass(_outlinePass);
    }

    protected override void Dispose(bool disposing)
    {
        CoreUtils.Destroy(_silhouetteMaskMaterial);
        CoreUtils.Destroy(_outlineMaterial);
    }

    sealed class PlayerOutlinePass : ScriptableRenderPass
    {
        static readonly int MaskTextureId =
            Shader.PropertyToID("_PlayerOutlineMask");

        static readonly int MaskTexelSizeId =
            Shader.PropertyToID("_PlayerOutlineMask_TexelSize");

        static readonly List<ShaderTagId> ShaderTags = new()
        {
            new ShaderTagId("UniversalForwardOnly"),
            new ShaderTagId("UniversalForward"),
            new ShaderTagId("SRPDefaultUnlit"),
            new ShaderTagId("LightweightForward")
        };

        readonly Material _silhouetteMaskMaterial;
        readonly Material _outlineMaterial;

        LayerMask _playerLayer;

        sealed class MaskPassData
        {
            public RendererListHandle rendererList;
        }

        sealed class CompositePassData
        {
            public TextureHandle maskTexture;
            public Material material;
            public Vector4 maskTexelSize;
        }

        public PlayerOutlinePass(
            Material silhouetteMaskMaterial,
            Material outlineMaterial)
        {
            _silhouetteMaskMaterial = silhouetteMaskMaterial;
            _outlineMaterial = outlineMaterial;
        }

        public void Setup(
            LayerMask playerLayer,
            Color outlineColor,
            float outlineWidth,
            float ditherCellSize)
        {
            _playerLayer = playerLayer;
            _outlineMaterial.SetColor("_OutlineColor", outlineColor);
            _outlineMaterial.SetFloat("_OutlineWidth", outlineWidth);
            _outlineMaterial.SetFloat("_DitherCellSize", ditherCellSize);
        }

        public override void RecordRenderGraph(
            RenderGraph renderGraph,
            ContextContainer frameData)
        {
            UniversalResourceData resourceData =
                frameData.Get<UniversalResourceData>();

            UniversalRenderingData renderingData =
                frameData.Get<UniversalRenderingData>();

            UniversalCameraData cameraData =
                frameData.Get<UniversalCameraData>();

            UniversalLightData lightData =
                frameData.Get<UniversalLightData>();

            if (!resourceData.activeColorTexture.IsValid() ||
                !resourceData.activeDepthTexture.IsValid())
            {
                return;
            }

            TextureDesc maskDescriptor =
                renderGraph.GetTextureDesc(resourceData.activeColorTexture);

            maskDescriptor.name = "_PlayerOutlineMask";
            maskDescriptor.colorFormat = GraphicsFormat.R8_UNorm;
            maskDescriptor.msaaSamples = MSAASamples.None;
            maskDescriptor.filterMode = FilterMode.Point;
            maskDescriptor.wrapMode = TextureWrapMode.Clamp;
            maskDescriptor.clearBuffer = true;
            maskDescriptor.clearColor = Color.clear;

            TextureHandle maskTexture =
                renderGraph.CreateTexture(maskDescriptor);

            RecordMaskPass(
                renderGraph,
                renderingData,
                cameraData,
                lightData,
                resourceData.activeDepthTexture,
                maskTexture);

            RecordCompositePass(
                renderGraph,
                resourceData.activeColorTexture,
                maskTexture,
                maskDescriptor);
        }

        void RecordMaskPass(
            RenderGraph renderGraph,
            UniversalRenderingData renderingData,
            UniversalCameraData cameraData,
            UniversalLightData lightData,
            TextureHandle depthTexture,
            TextureHandle maskTexture)
        {
            using IRasterRenderGraphBuilder builder =
                renderGraph.AddRasterRenderPass<MaskPassData>(
                    "Player Silhouette Mask",
                    out MaskPassData passData);

            DrawingSettings drawingSettings =
                RenderingUtils.CreateDrawingSettings(
                    ShaderTags,
                    renderingData,
                    cameraData,
                    lightData,
                    cameraData.defaultOpaqueSortFlags);

            drawingSettings.overrideMaterial =
                _silhouetteMaskMaterial;

            drawingSettings.overrideMaterialPassIndex = 0;

            FilteringSettings filteringSettings =
                new(RenderQueueRange.opaque, _playerLayer);

            RendererListParams rendererListParams = new(
                renderingData.cullResults,
                drawingSettings,
                filteringSettings);

            passData.rendererList =
                renderGraph.CreateRendererList(rendererListParams);

            if (!passData.rendererList.IsValid())
            {
                return;
            }

            builder.UseRendererList(passData.rendererList);
            builder.SetRenderAttachment(
                maskTexture,
                0,
                AccessFlags.Write);

            builder.SetRenderAttachmentDepth(
                depthTexture,
                AccessFlags.Read);

            builder.SetRenderFunc(static (
                MaskPassData data,
                RasterGraphContext context) =>
            {
                context.cmd.DrawRendererList(data.rendererList);
            });
        }

        void RecordCompositePass(
            RenderGraph renderGraph,
            TextureHandle cameraColor,
            TextureHandle maskTexture,
            TextureDesc maskDescriptor)
        {
            using IRasterRenderGraphBuilder builder =
                renderGraph.AddRasterRenderPass<CompositePassData>(
                    "Player Screen Space Outline",
                    out CompositePassData passData);

            passData.maskTexture = maskTexture;
            passData.material = _outlineMaterial;
            passData.maskTexelSize = new Vector4(
                1f / maskDescriptor.width,
                1f / maskDescriptor.height,
                maskDescriptor.width,
                maskDescriptor.height);

            builder.UseTexture(maskTexture, AccessFlags.Read);
            builder.SetRenderAttachment(
                cameraColor,
                0,
                AccessFlags.ReadWrite);

            builder.SetRenderFunc(static (
                CompositePassData data,
                RasterGraphContext context) =>
            {
                data.material.SetTexture(
                    MaskTextureId,
                    data.maskTexture);

                data.material.SetVector(
                    MaskTexelSizeId,
                    data.maskTexelSize);

                context.cmd.DrawProcedural(
                    Matrix4x4.identity,
                    data.material,
                    0,
                    MeshTopology.Triangles,
                    3,
                    1);
            });
        }
    }
}
