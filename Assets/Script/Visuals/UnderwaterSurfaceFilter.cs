using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class UnderwaterSurfaceFilter : MonoBehaviour
{
    private const string FilterObjectName = "__UnderwaterSurfaceFilter";
    private const string DefaultMaterialPath = "Assets/Shader/BackgroundEffects/M_UnderwaterSurfaceFilter.mat";

    [Header("Setup")]
    public Material filterMaterial;
    public bool buildOnStart = true;
    public Vector3 localOffset = new Vector3(0f, -0.02f, 0f);

    [Header("Look")]
    [ColorUsage(false, true)]
    public Color tintColor = new Color(0.08f, 0.65f, 0.85f, 1f);
    [Range(0f, 1f)] public float tintStrength = 0.45f;
    [Range(0f, 1f)] public float alpha = 0.55f;
    [Range(0f, 0.08f)] public float distortionStrength = 0.018f;
    [Range(0.1f, 80f)] public float noiseScale = 18f;
    [Range(0f, 3f)] public float noiseSpeed = 0.18f;
    [Range(0.25f, 8f)] public float fresnelPower = 2.2f;
    [Range(0f, 1f)] public float fresnelStrength = 0.22f;
    [Range(0f, 3f)] public float brightness = 1.05f;

    [Header("Caustics")]
    [ColorUsage(false, true)]
    public Color causticsColor = new Color(0.45f, 1f, 0.9f, 1f);
    [Range(0f, 10f)] public float causticsIntensity = 1.2f;
    [Range(0f, 1f)] public float causticsAlphaBoost = 0.12f;
    [Range(0.01f, 20f)] public float causticsTiling = 2.5f;
    public Vector2 causticsSpeedA = new Vector2(0.03f, 0.02f);
    public Vector2 causticsSpeedB = new Vector2(-0.02f, 0.04f);

    private MeshFilter sourceFilter;
    private MeshRenderer sourceRenderer;
    private MeshRenderer filterRenderer;
    private MaterialPropertyBlock propertyBlock;

    // Initializes the filter material and generated child when the component is added.
    private void Reset()
    {
        CacheSourceComponents();
        LoadDefaultMaterialInEditor();
        RebuildFilter();
    }

    // Builds the generated filter for runtime scenes if requested.
    private void Start()
    {
        if (buildOnStart && filterRenderer == null)
        {
            RebuildFilter();
        }
    }

    // Keeps existing generated filters aligned with Inspector changes.
    private void OnValidate()
    {
        CacheSourceComponents();
        ApplyRendererTransform();
        ApplyProperties();
    }

    // Recreates the child renderer that draws the underwater-only filter.
    [ContextMenu("Rebuild Filter")]
    public void RebuildFilter()
    {
        CacheSourceComponents();
        LoadDefaultMaterialInEditor();
        RemoveFilter();

        if (sourceFilter == null || sourceFilter.sharedMesh == null)
        {
            Debug.LogWarning($"[{nameof(UnderwaterSurfaceFilter)}] Source MeshFilter has no mesh.", this);
            return;
        }

        if (filterMaterial == null)
        {
            Debug.LogWarning($"[{nameof(UnderwaterSurfaceFilter)}] Filter material is not assigned.", this);
            return;
        }

        GameObject filterObject = new GameObject(FilterObjectName);
        Transform filterTransform = filterObject.transform;
        filterTransform.SetParent(transform, false);
        filterTransform.localPosition = localOffset;
        filterTransform.localRotation = Quaternion.identity;
        filterTransform.localScale = Vector3.one;
        filterObject.layer = gameObject.layer;

        MeshFilter meshFilter = filterObject.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = sourceFilter.sharedMesh;

        filterRenderer = filterObject.AddComponent<MeshRenderer>();
        filterRenderer.sharedMaterial = filterMaterial;
        CopyRendererSettings(sourceRenderer, filterRenderer);
        ApplyProperties();
    }

    // Removes generated filter children without touching the original water renderer.
    [ContextMenu("Remove Filter")]
    public void RemoveFilter()
    {
        filterRenderer = null;

        var childrenToRemove = new List<GameObject>();
        foreach (Transform child in transform)
        {
            if (child.name == FilterObjectName)
            {
                childrenToRemove.Add(child.gameObject);
            }
        }

        foreach (GameObject child in childrenToRemove)
        {
            if (Application.isPlaying)
            {
                Destroy(child);
            }
            else
            {
                DestroyImmediate(child);
            }
        }
    }

    // Caches source components from the water surface object.
    private void CacheSourceComponents()
    {
        if (sourceFilter == null)
        {
            sourceFilter = GetComponent<MeshFilter>();
        }

        if (sourceRenderer == null)
        {
            sourceRenderer = GetComponent<MeshRenderer>();
        }

        if (filterRenderer == null)
        {
            Transform filterTransform = transform.Find(FilterObjectName);
            filterRenderer = filterTransform != null ? filterTransform.GetComponent<MeshRenderer>() : null;
        }
    }

    // Loads the default material in the editor to reduce manual setup.
    private void LoadDefaultMaterialInEditor()
    {
#if UNITY_EDITOR
        if (filterMaterial == null)
        {
            filterMaterial = AssetDatabase.LoadAssetAtPath<Material>(DefaultMaterialPath);
        }
#endif
    }

    // Copies safe renderer settings and disables lighting/shadow overhead on the filter.
    private static void CopyRendererSettings(Renderer source, Renderer target)
    {
        target.shadowCastingMode = ShadowCastingMode.Off;
        target.receiveShadows = false;
        target.lightProbeUsage = LightProbeUsage.Off;
        target.reflectionProbeUsage = ReflectionProbeUsage.Off;

        if (source == null)
        {
            return;
        }

        target.renderingLayerMask = source.renderingLayerMask;
        target.sortingLayerID = source.sortingLayerID;
        target.sortingOrder = source.sortingOrder + 1;
    }

    // Applies the local offset to an existing generated filter child.
    private void ApplyRendererTransform()
    {
        if (filterRenderer == null)
        {
            return;
        }

        Transform filterTransform = filterRenderer.transform;
        filterTransform.localPosition = localOffset;
        filterTransform.localRotation = Quaternion.identity;
        filterTransform.localScale = Vector3.one;
    }

    // Applies per-object shader values without cloning the shared material.
    private void ApplyProperties()
    {
        if (filterRenderer == null)
        {
            return;
        }

        propertyBlock ??= new MaterialPropertyBlock();
        propertyBlock.SetColor("_TintColor", tintColor);
        propertyBlock.SetFloat("_TintStrength", tintStrength);
        propertyBlock.SetFloat("_Alpha", alpha);
        propertyBlock.SetFloat("_DistortionStrength", distortionStrength);
        propertyBlock.SetFloat("_NoiseScale", noiseScale);
        propertyBlock.SetFloat("_NoiseSpeed", noiseSpeed);
        propertyBlock.SetFloat("_FresnelPower", fresnelPower);
        propertyBlock.SetFloat("_FresnelStrength", fresnelStrength);
        propertyBlock.SetFloat("_Brightness", brightness);
        propertyBlock.SetColor("_CausticsColor", causticsColor);
        propertyBlock.SetFloat("_CausticsIntensity", causticsIntensity);
        propertyBlock.SetFloat("_CausticsAlphaBoost", causticsAlphaBoost);
        propertyBlock.SetFloat("_CausticsTiling", causticsTiling);
        propertyBlock.SetVector("_CausticsSpeedA", new Vector4(causticsSpeedA.x, causticsSpeedA.y, 0f, 0f));
        propertyBlock.SetVector("_CausticsSpeedB", new Vector4(causticsSpeedB.x, causticsSpeedB.y, 0f, 0f));
        filterRenderer.SetPropertyBlock(propertyBlock);
    }
}
