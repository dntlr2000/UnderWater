using UnityEngine;
using UnityEngine.Rendering.Universal;

[ExecuteAlways]
[RequireComponent(typeof(DecalProjector))]
public class SoftLightDecalProjectorController : MonoBehaviour
{
    [Header("Shape")]
    [Min(0.01f)] public float diameter = 3f;
    [Min(0.01f)] public float heightRatio = 1f;
    [Min(0.01f)] public float projectionDepth = 2f;
    public bool centerPivotToDepth = true;

    [Header("Visibility")]
    [Range(0f, 1f)] public float opacity = 1f;

    private DecalProjector decalProjector;

    // Caches the DecalProjector reference when the component is first added.
    private void Reset()
    {
        CacheProjector();
        ReadCurrentProjectorSize();
        ApplyProjectorSettings();
    }

    // Applies scene-time changes immediately in the Inspector.
    private void OnValidate()
    {
        CacheProjector();
        ApplyProjectorSettings();
    }

    // Ensures runtime-spawned decals use the same size values.
    private void Awake()
    {
        CacheProjector();
        ApplyProjectorSettings();
    }

    // Finds the DecalProjector once and reuses the cached reference.
    private void CacheProjector()
    {
        if (decalProjector == null)
        {
            decalProjector = GetComponent<DecalProjector>();
        }
    }

    // Copies the current DecalProjector size into the controller fields.
    private void ReadCurrentProjectorSize()
    {
        if (decalProjector == null)
        {
            return;
        }

        Vector3 size = decalProjector.size;
        diameter = Mathf.Max(size.x, 0.01f);
        heightRatio = size.x > 0f ? Mathf.Max(size.y / size.x, 0.01f) : 1f;
        projectionDepth = Mathf.Max(size.z, 0.01f);
        opacity = decalProjector.fadeFactor;
    }

    // Writes diameter, aspect ratio, depth, and opacity back to the DecalProjector.
    private void ApplyProjectorSettings()
    {
        if (decalProjector == null)
        {
            return;
        }

        diameter = Mathf.Max(diameter, 0.01f);
        heightRatio = Mathf.Max(heightRatio, 0.01f);
        projectionDepth = Mathf.Max(projectionDepth, 0.01f);

        decalProjector.size = new Vector3(diameter, diameter * heightRatio, projectionDepth);
        decalProjector.fadeFactor = opacity;

        if (centerPivotToDepth)
        {
            Vector3 pivot = decalProjector.pivot;
            pivot.z = projectionDepth * 0.5f;
            decalProjector.pivot = pivot;
        }
    }
}
