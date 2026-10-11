using System.Collections.Generic;
using UnityEngine;

// Keep this component's name/GUID so existing field-item prefabs retain their settings.
[ExecuteAlways, DisallowMultipleComponent]
public class InvertedHullOverlay : MonoBehaviour
{
    private const string OverlayPrefix = "__InvertedHullOverlay";
    private const string DepthMaskPrefix = "__InvertedHullDepthMask";

    // Legacy serialized fields remain compatible with existing prefabs and editor tools.
    [HideInInspector] public Material overlayMaterial;
    [HideInInspector] public Material depthMaskMaterial;
    [HideInInspector] public bool useDepthMask = true;
    [HideInInspector] public float shellOffset = 0.015f;
    [HideInInspector] public float rimPower = 3f;
    [HideInInspector] public float rimThreshold = 0.2f;
    [HideInInspector] public float rimSoftness = 0.2f;

    [Header("Screen-space silhouette")]
    public bool includeChildren = true;
    public bool buildOnStart = true;
    [ColorUsage(false, true)] public Color rimColor = new Color(0.2f, 1.2f, 1.4f, 1f);
    [Range(1, 4), Tooltip("Outline width in render-target pixels, independent of model scale.")]
    public int outlineWidth = 2;
    [Range(0f, 10f)] public float intensity = 2f;
    [Range(0f, 1f)] public float alpha = 0.7f;

    internal static readonly HashSet<InvertedHullOverlay> Active = new();
    internal readonly List<Renderer> Sources = new();
    private bool built;

    // Register scene objects and runtime-spawned items without creating extra meshes.
    private void OnEnable()
    {
        if (buildOnStart || built)
        {
            CacheSources();
            built = true;
            Active.Add(this);
        }
    }

    // Stop drawing immediately when an item is picked up, pooled or disabled.
    private void OnDisable()
    {
        Active.Remove(this);
    }

    // Release the registry entry even when destruction happens outside normal play flow.
    private void OnDestroy()
    {
        Active.Remove(this);
    }

    // Refresh a replaced model hierarchy without changing its original materials.
    private void OnTransformChildrenChanged()
    {
        if (built) CacheSources();
    }

    // Clamp Inspector settings; the render pass reads them again for each camera.
    private void OnValidate()
    {
        ApplyProperties();
    }

    // Preserve the old public API while rebuilding a mask-source list instead of shells.
    [ContextMenu("Rebuild Overlay")]
    public void RebuildOverlay()
    {
        RemoveOverlay();
        CacheSources();
        built = true;
        if (isActiveAndEnabled) Active.Add(this);
    }

    // Unregister the outline and remove only helper objects made by the former hull code.
    [ContextMenu("Remove Overlay")]
    public void RemoveOverlay()
    {
        Active.Remove(this);
        built = false;
        Sources.Clear();
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            if (child == null || child == transform || !IsLegacyHelper(child)) continue;
            child.gameObject.SetActive(false);
            if (Application.isPlaying) Destroy(child.gameObject);
            else DestroyImmediate(child.gameObject);
        }
    }

    // Collect all submeshes as one item; disable any previously saved hull helpers.
    private void CacheSources()
    {
        Sources.Clear();
        Renderer[] renderers = includeChildren
            ? GetComponentsInChildren<Renderer>(true)
            : GetComponents<Renderer>();
        foreach (Renderer source in renderers)
        {
            if (!(source is MeshRenderer) && !(source is SkinnedMeshRenderer)) continue;
            if (IsLegacyHelper(source.transform))
            {
                source.enabled = false;
                continue;
            }
            // A nested item manages its own renderers, avoiding duplicate mask draws.
            if (source.GetComponentInParent<InvertedHullOverlay>() != this) continue;
            Sources.Add(source);
        }
    }

    // Recognize only the generated names used by the previous implementation.
    private static bool IsLegacyHelper(Transform target)
    {
        return target.name.StartsWith(OverlayPrefix) || target.name.StartsWith(DepthMaskPrefix);
    }

    // Preserve external callers; source materials and their property blocks stay intact.
    public void ApplyProperties()
    {
        outlineWidth = Mathf.Clamp(outlineWidth, 1, 4);
        intensity = Mathf.Clamp(intensity, 0f, 10f);
        alpha = Mathf.Clamp01(alpha);
    }
}
