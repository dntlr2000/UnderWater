using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

// Draw a union of item coverage, then add color only outside that coverage.
public sealed class ItemSilhouetteRendererFeature : ScriptableRendererFeature
{
    [SerializeField] private Shader maskShader;
    [SerializeField] private Shader compositeShader;
    private SilhouettePass pass;

    // Serialized shader references keep both passes available in standalone builds.
    public override void Create()
    {
        pass?.Dispose();
        pass = maskShader != null && compositeShader != null
            ? new SilhouettePass(maskShader, compositeShader) : null;
    }

    // Draw world items for player/Scene views, excluding UI overlays and asset previews.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        CameraType type = renderingData.cameraData.cameraType;
        if (pass == null || InvertedHullOverlay.Active.Count == 0 ||
            (type != CameraType.Game && type != CameraType.SceneView) ||
            renderingData.cameraData.renderType != CameraRenderType.Base) return;
        renderer.EnqueuePass(pass);
    }

    // Dispose cached mask materials when the renderer is rebuilt or unloaded.
    protected override void Dispose(bool disposing)
    {
        pass?.Dispose();
        pass = null;
    }

    private sealed class SilhouettePass : ScriptableRenderPass
    {
        private static readonly int ColorId = Shader.PropertyToID("_ItemOutlineColor");
        private static readonly int WidthId = Shader.PropertyToID("_ItemOutlineWidth");
        private static readonly int MaskId = Shader.PropertyToID("_ItemSilhouetteMask");
        private static readonly int DataId = Shader.PropertyToID("_ItemSilhouetteData");
        private static readonly int DepthId = Shader.PropertyToID("_CameraDepthTexture");
        private static readonly int RadiusId = Shader.PropertyToID("_ItemOutlineRadius");
        private readonly Shader maskShader;
        private readonly Material composite;
        private readonly Dictionary<Material, Material> maskMaterials = new();
        private readonly List<Material> sourceMaterials = new();
        private readonly List<Material> unusedMaterials = new();
        private readonly HashSet<Material> usedMaterials = new();
        private readonly Plane[] frustum = new Plane[6];

        private struct Draw
        {
            public Renderer renderer;
            public Material material;
            public int submesh;
            public Color color;
            public int width;
        }

        private sealed class MaskPassData
        {
            public List<Draw> draws;
            public TextureHandle sceneDepth;
        }

        private sealed class CompositePassData
        {
            public Material material;
            public TextureHandle mask, data, sceneDepth;
            public int radius;
        }

        // Run before transparent water, allowing it to tint/cover the world outline normally.
        public SilhouettePass(Shader shader, Shader compositeShader)
        {
            maskShader = shader;
            composite = CoreUtils.CreateEngineMaterial(compositeShader);
            renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
            ConfigureInput(ScriptableRenderPassInput.Depth);
        }

        // Record explicit mask/depth dependencies for Unity 6's active Render Graph path.
        public override void RecordRenderGraph(RenderGraph graph, ContextContainer frameData)
        {
            UniversalResourceData resources = frameData.Get<UniversalResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            List<Draw> draws = CollectDraws(cameraData.camera, out int radius);
            if (draws.Count == 0 || !resources.cameraDepthTexture.IsValid()) return;

            var descriptor = new TextureDesc(cameraData.cameraTargetDescriptor)
            {
                name = "Item silhouette coverage", colorFormat = GraphicsFormat.R16G16B16A16_SFloat,
                msaaSamples = MSAASamples.None, bindTextureMS = false,
                clearBuffer = true, clearColor = Color.clear, filterMode = FilterMode.Point
            };
            TextureHandle mask = graph.CreateTexture(descriptor);
            descriptor.name = "Item silhouette eye depth and width";
            descriptor.colorFormat = GraphicsFormat.R32G32_SFloat;
            TextureHandle data = graph.CreateTexture(descriptor);
            descriptor.name = "Item silhouette private depth";
            descriptor.depthBufferBits = DepthBits.Depth32;
            TextureHandle depth = graph.CreateTexture(descriptor);

            using (var builder = graph.AddRasterRenderPass<MaskPassData>("Item silhouette mask", out var pd))
            {
                pd.draws = draws;
                pd.sceneDepth = resources.cameraDepthTexture;
                builder.UseTexture(pd.sceneDepth, AccessFlags.Read);
                builder.SetRenderAttachment(mask, 0, AccessFlags.Write);
                builder.SetRenderAttachment(data, 1, AccessFlags.Write);
                builder.SetRenderAttachmentDepth(depth, AccessFlags.Write);
                builder.AllowGlobalStateModification(true);
                builder.SetRenderFunc((MaskPassData p, RasterGraphContext ctx) => DrawMask(p, ctx));
            }

            using (var builder = graph.AddRasterRenderPass<CompositePassData>("Item outer silhouette", out var pd))
            {
                pd.material = composite;
                pd.mask = mask;
                pd.data = data;
                pd.sceneDepth = resources.cameraDepthTexture;
                pd.radius = radius;
                builder.UseTexture(mask, AccessFlags.Read);
                builder.UseTexture(data, AccessFlags.Read);
                builder.UseTexture(pd.sceneDepth, AccessFlags.Read);
                // Blending reads the destination; never sample/write camera color simultaneously.
                builder.SetRenderAttachment(resources.activeColorTexture, 0, AccessFlags.ReadWrite);
                builder.AllowGlobalStateModification(true);
                builder.SetRenderFunc((CompositePassData p, RasterGraphContext ctx) => CompositeMask(p, ctx));
            }
        }

        // Cull using this camera and emit every material slot, including skinned meshes.
        private List<Draw> CollectDraws(Camera camera, out int radius)
        {
            var draws = new List<Draw>();
            radius = 1;
            usedMaterials.Clear();
            GeometryUtility.CalculateFrustumPlanes(camera, frustum);
            foreach (InvertedHullOverlay item in InvertedHullOverlay.Active)
            {
                if (item == null || !item.isActiveAndEnabled || !item.gameObject.scene.IsValid() ||
                    item.alpha <= 0f || item.intensity <= 0f) continue;
                int width = Mathf.Clamp(item.outlineWidth, 1, 4);
                Color color = item.rimColor * item.intensity;
                // Old prefabs have rimColor.a = 0; their separate alpha is the intended opacity.
                color.a = Mathf.Clamp01(item.alpha);
                foreach (Renderer source in item.Sources)
                {
                    if (source == null || !source.enabled || source.forceRenderingOff ||
                        !source.gameObject.activeInHierarchy ||
                        (camera.cullingMask & (1 << source.gameObject.layer)) == 0 ||
                        !GeometryUtility.TestPlanesAABB(frustum, source.bounds)) continue;
#if UNITY_EDITOR
                    if (!UnityEditor.SceneManagement.StageUtility.IsGameObjectRenderedByCamera(source.gameObject, camera) ||
                        (camera.cameraType == CameraType.SceneView &&
                         UnityEditor.SceneVisibilityManager.instance.IsHidden(source.gameObject))) continue;
#endif
                    Mesh mesh = source is SkinnedMeshRenderer skinned ? skinned.sharedMesh :
                        source.GetComponent<MeshFilter>()?.sharedMesh;
                    if (mesh == null || mesh.subMeshCount == 0) continue;
                    source.GetSharedMaterials(sourceMaterials);
                    for (int i = 0; i < sourceMaterials.Count; i++)
                    {
                        Material original = sourceMaterials[i];
                        if (original == null) continue;
                        Material maskMaterial = GetMaskMaterial(original);
                        draws.Add(new Draw { renderer = source, material = maskMaterial,
                            submesh = Mathf.Min(i, mesh.subMeshCount - 1), color = color, width = width });
                    }
                    radius = Mathf.Max(radius, width);
                }
            }
            ReleaseUnusedMaterials();
            return draws;
        }

        // Match URP/glTF base alpha and culling without editing the original renderer/material.
        private Material GetMaskMaterial(Material source)
        {
            if (!maskMaterials.TryGetValue(source, out Material material))
            {
                material = CoreUtils.CreateEngineMaterial(maskShader);
                maskMaterials.Add(source, material);
            }
            if (!usedMaterials.Add(source)) return material;
            string map = source.HasProperty("baseColorTexture") ? "baseColorTexture" :
                source.HasProperty("_BaseMap") ? "_BaseMap" : "_MainTex";
            Texture texture = source.HasProperty(map) ? source.GetTexture(map) : null;
            material.SetTexture("_MaskBaseMap", texture != null ? texture : Texture2D.whiteTexture);
            Vector2 scale = source.HasProperty(map) ? source.GetTextureScale(map) : Vector2.one;
            Vector2 offset = source.HasProperty(map) ? source.GetTextureOffset(map) : Vector2.zero;
            material.SetVector("_MaskST", new Vector4(scale.x, scale.y, offset.x, offset.y));
            material.SetVector("_MaskRotation", source.HasProperty("baseColorTexture_Rotation")
                ? source.GetVector("baseColorTexture_Rotation") : Vector4.zero);
            material.SetFloat("_MaskUVSet", ReadFloat(source, "baseColorTexture_texCoord", 0));
            string tint = source.HasProperty("baseColorFactor") ? "baseColorFactor" :
                source.HasProperty("_BaseColor") ? "_BaseColor" : "_Color";
            material.SetFloat("_MaskAlpha", source.HasProperty(tint) ? source.GetColor(tint).a : 1f);
            bool clipped = source.IsKeywordEnabled("_ALPHATEST_ON") || ReadFloat(source, "_AlphaClip", 0) > 0;
            float cutoff = clipped ? ReadFloat(source, "alphaCutoff", ReadFloat(source, "_Cutoff", 0.5f)) :
                source.renderQueue >= (int)RenderQueue.Transparent ? 0.01f : -1f;
            material.SetFloat("_MaskCutoff", cutoff);
            material.SetFloat("_MaskCull", ReadFloat(source, "_Cull", ReadFloat(source, "_CullMode", 2)));
            return material;
        }

        // Read optional source shader properties with a defined opaque-material fallback.
        private static float ReadFloat(Material source, string property, float fallback)
        {
            return source.HasProperty(property) ? source.GetFloat(property) : fallback;
        }

        // Write per-item colors and coverage using a private depth buffer, leaving scene depth intact.
        private static void DrawMask(MaskPassData data, RasterGraphContext context)
        {
            context.cmd.SetGlobalTexture(DepthId, data.sceneDepth);
            foreach (Draw draw in data.draws)
            {
                context.cmd.SetGlobalColor(ColorId, draw.color);
                context.cmd.SetGlobalFloat(WidthId, draw.width);
                context.cmd.DrawRenderer(draw.renderer, draw.material, draw.submesh, 0);
            }
        }

        // Add the dilated mask's outside pixels; the shader rejects covered and occluded pixels.
        private static void CompositeMask(CompositePassData data, RasterGraphContext context)
        {
            context.cmd.SetGlobalTexture(MaskId, data.mask);
            context.cmd.SetGlobalTexture(DataId, data.data);
            context.cmd.SetGlobalTexture(DepthId, data.sceneDepth);
            context.cmd.SetGlobalInteger(RadiusId, data.radius);
            context.cmd.DrawProcedural(Matrix4x4.identity, data.material, 0, MeshTopology.Triangles, 3);
        }

        // Drop destroyed sources but retain live materials across Scene/Game cameras to avoid allocation churn.
        private void ReleaseUnusedMaterials()
        {
            unusedMaterials.Clear();
            foreach (var pair in maskMaterials)
                if (pair.Key == null) unusedMaterials.Add(pair.Key);
            foreach (Material source in unusedMaterials)
            {
                CoreUtils.Destroy(maskMaterials[source]);
                maskMaterials.Remove(source);
            }
        }

        // Release all owned GPU materials; Render Graph owns the temporary mask textures.
        public void Dispose()
        {
            foreach (Material material in maskMaterials.Values) CoreUtils.Destroy(material);
            maskMaterials.Clear();
            CoreUtils.Destroy(composite);
        }
    }
}
