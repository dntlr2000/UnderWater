using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
public class InvertedHullOverlay : MonoBehaviour
{
    private const string OverlayPrefix = "__InvertedHullOverlay";
    private const string DepthMaskPrefix = "__InvertedHullDepthMask";
    private const string DefaultDepthMaskResourcePath = "Materials/M_InvertedHullDepthMask";

    [Header("Overlay")]
    public Material overlayMaterial;
    public Material depthMaskMaterial;
    public bool includeChildren = true;
    public bool buildOnStart = true;
    public bool useDepthMask = true;

    [Header("Rim")]
    [ColorUsage(false, true)]
    public Color rimColor = new Color(0.2f, 1.2f, 1.4f, 1f);
    [Range(0f, 0.1f)] public float shellOffset = 0.015f;
    [Range(0.25f, 8f)] public float rimPower = 3f;
    [Range(0f, 1f)] public float rimThreshold = 0.2f;
    [Range(0.001f, 1f)] public float rimSoftness = 0.2f;
    [Range(0f, 10f)] public float intensity = 2f;
    [Range(0f, 1f)] public float alpha = 0.7f;

    private readonly List<Renderer> overlayRenderers = new();
    private MaterialPropertyBlock propertyBlock;

    // Builds the overlay automatically for runtime-spawned objects.
    private void Start()
    {
        if (buildOnStart)
        {
            RebuildOverlay();
        }
    }

    // Keeps the generated overlay in sync when values change in the Inspector.
    private void OnValidate()
    {
        ApplyProperties();
    }

    // Recreates the hidden depth mask and visible rim overlay renderers.
    [ContextMenu("Rebuild Overlay")]
    public void RebuildOverlay()
    {
        RemoveOverlay();
        EnsureDepthMaskMaterial();

        if (overlayMaterial == null)
        {
            Debug.LogWarning($"[{nameof(InvertedHullOverlay)}] Overlay material is not assigned.", this);
            return;
        }

        AddMeshRendererOverlays();
        AddSkinnedMeshRendererOverlays();
        ApplyProperties();
    }

    // Deletes generated helper objects without touching the source renderers.
    [ContextMenu("Remove Overlay")]
    public void RemoveOverlay()
    {
        overlayRenderers.Clear();

        var toDelete = new List<GameObject>();
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            if (child != transform && IsGeneratedOverlayObject(child))
            {
                toDelete.Add(child.gameObject);
            }
        }

        foreach (GameObject obj in toDelete)
        {
            if (Application.isPlaying)
            {
                Destroy(obj);
            }
            else
            {
                DestroyImmediate(obj);
            }
        }
    }

    // Adds overlay renderers for normal MeshRenderer sources.
    private void AddMeshRendererOverlays()
    {
        MeshRenderer[] renderers = includeChildren
            ? GetComponentsInChildren<MeshRenderer>(true)
            : GetComponents<MeshRenderer>();

        foreach (MeshRenderer sourceRenderer in renderers)
        {
            if (IsOverlayObject(sourceRenderer.transform)) continue;

            MeshFilter sourceFilter = sourceRenderer.GetComponent<MeshFilter>();
            if (sourceFilter == null || sourceFilter.sharedMesh == null) continue;

            if (useDepthMask && depthMaskMaterial != null)
            {
                GameObject depthMask = CreateOverlayObject(sourceRenderer.transform, DepthMaskPrefix);
                MeshFilter depthMaskFilter = depthMask.AddComponent<MeshFilter>();
                depthMaskFilter.sharedMesh = sourceFilter.sharedMesh;

                MeshRenderer depthMaskRenderer = depthMask.AddComponent<MeshRenderer>();
                CopyRendererSettings(sourceRenderer, depthMaskRenderer, 0);
                AssignRepeatedMaterial(sourceRenderer, depthMaskRenderer, depthMaskMaterial);
            }

            GameObject overlay = CreateOverlayObject(sourceRenderer.transform);
            MeshFilter overlayFilter = overlay.AddComponent<MeshFilter>();
            overlayFilter.sharedMesh = sourceFilter.sharedMesh;

            MeshRenderer overlayRenderer = overlay.AddComponent<MeshRenderer>();
            CopyRendererSettings(sourceRenderer, overlayRenderer, 1);
            AssignRepeatedMaterial(sourceRenderer, overlayRenderer, overlayMaterial);
            overlayRenderers.Add(overlayRenderer);
        }
    }

    // Adds overlay renderers for animated SkinnedMeshRenderer sources.
    private void AddSkinnedMeshRendererOverlays()
    {
        SkinnedMeshRenderer[] renderers = includeChildren
            ? GetComponentsInChildren<SkinnedMeshRenderer>(true)
            : GetComponents<SkinnedMeshRenderer>();

        foreach (SkinnedMeshRenderer sourceRenderer in renderers)
        {
            if (IsOverlayObject(sourceRenderer.transform)) continue;
            if (sourceRenderer.sharedMesh == null) continue;

            if (useDepthMask && depthMaskMaterial != null)
            {
                GameObject depthMask = CreateOverlayObject(sourceRenderer.transform, DepthMaskPrefix);
                SkinnedMeshRenderer depthMaskRenderer = depthMask.AddComponent<SkinnedMeshRenderer>();
                CopySkinnedMeshSettings(sourceRenderer, depthMaskRenderer);
                CopyRendererSettings(sourceRenderer, depthMaskRenderer, 0);
                AssignRepeatedMaterial(sourceRenderer, depthMaskRenderer, depthMaskMaterial);
            }

            GameObject overlay = CreateOverlayObject(sourceRenderer.transform);
            SkinnedMeshRenderer overlayRenderer = overlay.AddComponent<SkinnedMeshRenderer>();
            CopySkinnedMeshSettings(sourceRenderer, overlayRenderer);
            CopyRendererSettings(sourceRenderer, overlayRenderer, 1);
            AssignRepeatedMaterial(sourceRenderer, overlayRenderer, overlayMaterial);
            overlayRenderers.Add(overlayRenderer);
        }
    }

    // Creates a child object that follows a source renderer exactly.
    private GameObject CreateOverlayObject(Transform source)
    {
        return CreateOverlayObject(source, OverlayPrefix);
    }

    // Creates a named generated child object for either the depth mask or rim shell.
    private GameObject CreateOverlayObject(Transform source, string prefix)
    {
        GameObject overlay = new GameObject($"{prefix}_{source.name}");
        Transform overlayTransform = overlay.transform;
        overlayTransform.SetParent(source, false);
        overlayTransform.localPosition = Vector3.zero;
        overlayTransform.localRotation = Quaternion.identity;
        overlayTransform.localScale = Vector3.one;
        overlay.layer = source.gameObject.layer;
        return overlay;
    }

    // Checks whether a renderer belongs to a generated helper object.
    private static bool IsOverlayObject(Transform target)
    {
        Transform current = target;
        while (current != null)
        {
            if (IsGeneratedOverlayObject(current)) return true;
            current = current.parent;
        }
        return false;
    }

    // Checks whether a transform was created by this component.
    private static bool IsGeneratedOverlayObject(Transform target)
    {
        return target.name.StartsWith(OverlayPrefix) || target.name.StartsWith(DepthMaskPrefix);
    }

    // Copies renderer settings that should match the source without adding lighting cost.
    private static void CopyRendererSettings(Renderer source, Renderer target, int sortingOrderOffset)
    {
        target.shadowCastingMode = ShadowCastingMode.Off;
        target.receiveShadows = false;
        target.lightProbeUsage = LightProbeUsage.Off;
        target.reflectionProbeUsage = ReflectionProbeUsage.Off;
        target.renderingLayerMask = source.renderingLayerMask;
        target.sortingLayerID = source.sortingLayerID;
        target.sortingOrder = source.sortingOrder + sortingOrderOffset;
    }

    // Copies skinning data so animated meshes keep their original deformation.
    private static void CopySkinnedMeshSettings(SkinnedMeshRenderer source, SkinnedMeshRenderer target)
    {
        target.sharedMesh = source.sharedMesh;
        target.bones = source.bones;
        target.rootBone = source.rootBone;
        target.updateWhenOffscreen = source.updateWhenOffscreen;
        target.localBounds = source.localBounds;
    }

    // Repeats one material across every source submesh slot.
    private static void AssignRepeatedMaterial(Renderer source, Renderer target, Material material)
    {
        Material[] sourceMaterials = source.sharedMaterials;
        int materialCount = Mathf.Max(1, sourceMaterials.Length);
        Material[] materials = new Material[materialCount];

        for (int i = 0; i < materials.Length; i++)
        {
            materials[i] = material;
        }

        target.sharedMaterials = materials;
    }

    // Loads the default invisible depth mask material when the field is empty.
    private void EnsureDepthMaskMaterial()
    {
        if (!useDepthMask || depthMaskMaterial != null)
        {
            return;
        }

        depthMaskMaterial = Resources.Load<Material>(DefaultDepthMaskResourcePath);
    }

    // Applies per-object rim values without cloning the shared overlay material.
    public void ApplyProperties()
    {
        if (propertyBlock == null)
        {
            propertyBlock = new MaterialPropertyBlock();
        }

        propertyBlock.SetColor("_RimColor", rimColor);
        propertyBlock.SetFloat("_ShellOffset", shellOffset);
        propertyBlock.SetFloat("_RimPower", rimPower);
        propertyBlock.SetFloat("_RimThreshold", rimThreshold);
        propertyBlock.SetFloat("_RimSoftness", rimSoftness);
        propertyBlock.SetFloat("_Intensity", intensity);
        propertyBlock.SetFloat("_Alpha", alpha);

        overlayRenderers.RemoveAll(renderer => renderer == null);
        foreach (Renderer renderer in overlayRenderers)
        {
            renderer.SetPropertyBlock(propertyBlock);
        }
    }
}
