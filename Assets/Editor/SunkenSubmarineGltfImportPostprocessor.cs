using System;
using System.Collections.Generic;
using GLTFast;
using GLTFast.Addons;
using UnityEditor;
using UnityEngine;

/// <summary>Registers the sunken-submarine collider proxy filter for glTFast Editor imports.</summary>
[InitializeOnLoad]
internal static class SunkenSubmarineGltfImportPostprocessor
{
    private const string TargetAssetPath = "Assets/Art/sunken_submarine_a/v08-de5b5462-package/asset.glb";
    private const string TextureTransformKeyword = "_TEXTURE_TRANSFORM";
    private const string SubmarineInteriorLayerName = "SubmarineInterior";
    private const string StaticMeshColliderSuffix = "__STATIC_MESH_COLLIDER";

    private const int ExpectedProxyNodeCount = 29;
    private const int ExpectedProxyBoxColliderCount = 24;
    private const int ExpectedBoxColliderComponentCount = 25;
    private const int ExpectedStaticMeshColliderCount = 2;

    private static readonly Vector4 PortableAtlasIdentityTransform = new Vector4(1f, 1f, 0f, 0f);

    private static readonly int[] PortableAtlasScaleTransformPropertyIds =
    {
        Shader.PropertyToID("baseColorTexture_ST"),
        Shader.PropertyToID("normalTexture_ST"),
        Shader.PropertyToID("metallicRoughnessTexture_ST"),
        Shader.PropertyToID("emissiveTexture_ST")
    };

    private static readonly HashSet<string> TargetPortableMaterialNames = new HashSet<string>(StringComparer.Ordinal)
    {
        "mat.hull.weathered__PORTABLE",
        "mat.opening.dark__PORTABLE",
        "mat.metal.rust__PORTABLE",
        "mat.hull.weathered.aperture_painted__PORTABLE",
        "mat.sail.weathered.window_painted__PORTABLE"
    };

    private static readonly HashSet<string> EntryBlockingProxyNames = new HashSet<string>(StringComparer.Ordinal)
    {
        "submarine.hull.main__LOD0__COLLIDER",
        "submarine.hull.stern_taper__LOD0__COLLIDER",
        "submarine.damage.breaches__LOD0__COLLIDER",
        "submarine.damage.breaches.rust_rim__LOD0__COLLIDER",
        "submarine.interior.hull_inner_surface.breach_backing__LOD0__COLLIDER"
    };

    private static readonly HashSet<string> StaticHullRenderNames = new HashSet<string>(StringComparer.Ordinal)
    {
        "submarine.hull.main__LOD0",
        "submarine.hull.stern_taper__LOD0"
    };

    private const string BreachThresholdProxyName = "submarine.interior.entry.breach_threshold__LOD0__COLLIDER";
    private const string ForwardInnerWallProxyName = "submarine.interior.hull_inner_surface.forward__LOD0__COLLIDER";
    private const string ForwardDoorwaySideColliderName = "ForwardDoorwaySide__BOX_COLLIDER";
    private const string ForwardDoorwayHeaderColliderName = "ForwardDoorwayHeader__BOX_COLLIDER";
    private const float ForwardDoorwayWidth = 1.3f;
    private const float ForwardDoorwayHeight = 2.2f;

    /// <summary>Registers one package-specific import add-on whenever the Editor domain loads.</summary>
    static SunkenSubmarineGltfImportPostprocessor()
    {
        ImportAddonRegistry.RegisterImportAddon(new ColliderProxyImportAddon());
    }

    /// <summary>Forces a synchronous reimport of the target GLB so the registered proxy filter is applied.</summary>
    public static void ReimportTargetAsset()
    {
        AssetDatabase.ImportAsset(TargetAssetPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        ValidateTargetAsset();
        ValidatePortableAtlasMaterials();
    }

    /// <summary>Verifies the render, proxy, and breach-safe collider hierarchy produced by the importer.</summary>
    private static void ValidateTargetAsset()
    {
        GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(TargetAssetPath);
        if (root == null)
        {
            throw new InvalidOperationException($"Unable to load imported GLB at {TargetAssetPath}.");
        }

        int proxyNodeCount = 0;
        int proxyMeshFilterCount = 0;
        int proxyRendererCount = 0;
        int authoredRendererCount = 0;
        int proxyBoxColliderCount = 0;
        int blockedProxyColliderCount = 0;
        int staticMeshColliderCount = 0;
        int invalidStaticMeshColliderCount = 0;
        int breachThresholdColliderCount = 0;
        int forwardDoorwayColliderCount = 0;
        int invalidForwardDoorwayColliderCount = 0;
        int rigidbodyCount = root.GetComponentsInChildren<Rigidbody>(true).Length;
        int submarineInteriorLayer = GetSubmarineInteriorLayer();

        foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
        {
            bool isProxy = transform.name.StartsWith("submarine.", StringComparison.Ordinal)
                && transform.name.IndexOf("__COLLIDER", StringComparison.Ordinal) >= 0;

            if (isProxy)
            {
                proxyNodeCount++;
                proxyMeshFilterCount += transform.GetComponent<MeshFilter>() == null ? 0 : 1;
                proxyRendererCount += transform.GetComponents<Renderer>().Length;

                Collider[] proxyColliders = transform.GetComponents<Collider>();
                if (EntryBlockingProxyNames.Contains(transform.name))
                {
                    blockedProxyColliderCount += transform.GetComponentsInChildren<Collider>(true).Length;
                }
                else if (transform.name == ForwardInnerWallProxyName)
                {
                    BoxCollider[] doorwayColliders = transform.GetComponentsInChildren<BoxCollider>(true);
                    forwardDoorwayColliderCount = doorwayColliders.Length;

                    bool hasValidDoorwayChildren = proxyColliders.Length == 0
                        && doorwayColliders.Length == 2
                        && Array.Exists(
                            doorwayColliders,
                            collider => collider.name == ForwardDoorwaySideColliderName
                                && collider.transform.parent == transform
                                && collider.gameObject.layer == submarineInteriorLayer)
                        && Array.Exists(
                            doorwayColliders,
                            collider => collider.name == ForwardDoorwayHeaderColliderName
                                && collider.transform.parent == transform
                                && collider.gameObject.layer == submarineInteriorLayer);

                    if (hasValidDoorwayChildren)
                    {
                        proxyBoxColliderCount++;
                    }
                    else
                    {
                        invalidForwardDoorwayColliderCount++;
                    }
                }
                else
                {
                    BoxCollider boxCollider = transform.GetComponent<BoxCollider>();
                    if (boxCollider != null && transform.gameObject.layer == submarineInteriorLayer)
                    {
                        proxyBoxColliderCount++;
                    }
                }

                if (transform.name == BreachThresholdProxyName
                    && transform.GetComponent<BoxCollider>() != null)
                {
                    breachThresholdColliderCount++;
                }

            }
            else
            {
                authoredRendererCount += transform.GetComponents<Renderer>().Length;
            }

            MeshCollider meshCollider = transform.GetComponent<MeshCollider>();
            if (meshCollider != null && transform.name.EndsWith(StaticMeshColliderSuffix, StringComparison.Ordinal))
            {
                staticMeshColliderCount++;
                if (meshCollider.convex
                    || meshCollider.sharedMesh == null
                    || transform.gameObject.layer != submarineInteriorLayer
                    || transform.parent == null
                    || !StaticHullRenderNames.Contains(transform.parent.name))
                {
                    invalidStaticMeshColliderCount++;
                }
            }
        }

        int colliderComponentCount = root.GetComponentsInChildren<Collider>(true).Length;
        int boxColliderComponentCount = root.GetComponentsInChildren<BoxCollider>(true).Length;

        if (proxyNodeCount != ExpectedProxyNodeCount
            || proxyMeshFilterCount != ExpectedProxyNodeCount
            || proxyRendererCount != 0
            || authoredRendererCount != 25
            || proxyBoxColliderCount != ExpectedProxyBoxColliderCount
            || blockedProxyColliderCount != 0
            || staticMeshColliderCount != ExpectedStaticMeshColliderCount
            || invalidStaticMeshColliderCount != 0
            || breachThresholdColliderCount != 1
            || forwardDoorwayColliderCount != 2
            || invalidForwardDoorwayColliderCount != 0
            || boxColliderComponentCount != ExpectedBoxColliderComponentCount
            || colliderComponentCount != ExpectedBoxColliderComponentCount + ExpectedStaticMeshColliderCount
            || rigidbodyCount != 0)
        {
            throw new InvalidOperationException(
                $"Unexpected imported hierarchy: proxyNodes={proxyNodeCount}, proxyMeshes={proxyMeshFilterCount}, "
                + $"proxyRenderers={proxyRendererCount}, authoredRenderers={authoredRendererCount}, "
                + $"proxyBoxes={proxyBoxColliderCount}, blockedProxyColliders={blockedProxyColliderCount}, "
                + $"staticMeshColliders={staticMeshColliderCount}, invalidStaticMeshColliders={invalidStaticMeshColliderCount}, "
                + $"breachThresholds={breachThresholdColliderCount}, forwardDoorwayBoxes={forwardDoorwayColliderCount}, "
                + $"invalidForwardDoorwayBoxes={invalidForwardDoorwayColliderCount}, "
                + $"boxColliders={boxColliderComponentCount}, colliders={colliderComponentCount}, "
                + $"rigidbodies={rigidbodyCount}.");
        }

        Debug.Log(
            $"[SunkenSubmarineGltfImportPostprocessor] Verified {TargetAssetPath}: "
            + $"proxyNodes={proxyNodeCount}, proxyMeshes={proxyMeshFilterCount}, proxyRenderers={proxyRendererCount}, "
            + $"authoredRenderers={authoredRendererCount}, proxyBoxes={proxyBoxColliderCount}, "
            + $"blockedProxyColliders={blockedProxyColliderCount}, staticMeshColliders={staticMeshColliderCount}, "
            + $"breachThresholds={breachThresholdColliderCount}, forwardDoorwayBoxes={forwardDoorwayColliderCount}, "
            + $"invalidForwardDoorwayBoxes={invalidForwardDoorwayColliderCount}, "
            + $"boxColliders={boxColliderComponentCount}, colliders={colliderComponentCount}, "
            + $"rigidbodies={rigidbodyCount}.");
    }

    /// <summary>Resolves the dedicated collision layer and fails import validation if it is unavailable.</summary>
    private static int GetSubmarineInteriorLayer()
    {
        int layer = LayerMask.NameToLayer(SubmarineInteriorLayerName);
        if (layer < 0)
        {
            throw new InvalidOperationException($"Required layer {SubmarineInteriorLayerName} is not defined.");
        }

        return layer;
    }

    /// <summary>Verifies that all package-authored portable materials use the imported UVs without an extra transform.</summary>
    private static void ValidatePortableAtlasMaterials()
    {
        int verifiedMaterialCount = 0;

        foreach (UnityEngine.Object subAsset in AssetDatabase.LoadAllAssetsAtPath(TargetAssetPath))
        {
            if (!(subAsset is Material material) || !TargetPortableMaterialNames.Contains(material.name))
            {
                continue;
            }

            foreach (int propertyId in PortableAtlasScaleTransformPropertyIds)
            {
                if (!material.HasProperty(propertyId)
                    || material.GetVector(propertyId) != PortableAtlasIdentityTransform)
                {
                    throw new InvalidOperationException(
                        $"Material {material.name} is missing the expected identity atlas transform.");
                }
            }

            if (material.IsKeywordEnabled(TextureTransformKeyword))
            {
                throw new InvalidOperationException(
                    $"Material {material.name} still has the redundant {TextureTransformKeyword} keyword enabled.");
            }

            verifiedMaterialCount++;
        }

        if (verifiedMaterialCount != TargetPortableMaterialNames.Count)
        {
            throw new InvalidOperationException(
                $"Expected {TargetPortableMaterialNames.Count} portable materials but verified {verifiedMaterialCount}.");
        }

        Debug.Log(
            $"[SunkenSubmarineGltfImportPostprocessor] Verified identity atlas transforms on "
            + $"{verifiedMaterialCount} portable materials.");
    }

    /// <summary>Creates a filter instance for each glTFast import operation.</summary>
    private sealed class ColliderProxyImportAddon : ImportAddon<ColliderProxyImportAddonInstance>
    {
    }

    /// <summary>Removes renderers from collider proxy nodes while retaining their transforms and meshes.</summary>
    private sealed class ColliderProxyImportAddonInstance : ImportAddonInstance
    {
        private const string ColliderMarker = "__COLLIDER";
        private const string SubmarineSemanticPrefix = "submarine.";
        private const string BatchedSubmarineSemanticPrefix = "CBM_submarine_";

        private readonly HashSet<GameObjectInstantiator> instantiators = new HashSet<GameObjectInstantiator>();
        private readonly HashSet<Material> processedMaterials = new HashSet<Material>();

        /// <summary>Creates one import-scoped renderer filter instance for glTFast.</summary>
        public ColliderProxyImportAddonInstance()
        {
        }

        /// <summary>Reports no custom glTF extension support because filtering uses node and mesh names.</summary>
        public override bool SupportsGltfExtension(string extensionName)
        {
            return false;
        }

        /// <summary>Adds this instance to the current glTF import lifecycle.</summary>
        public override void Inject(GltfImportBase gltfImport)
        {
            gltfImport.AddImportAddonInstance(this);
        }

        /// <summary>Subscribes to GameObject mesh creation for the current scene instantiator.</summary>
        public override void Inject(IInstantiator instantiator)
        {
            if (instantiator is GameObjectInstantiator gameObjectInstantiator
                && instantiators.Add(gameObjectInstantiator))
            {
                gameObjectInstantiator.MeshAdded += OnMeshAdded;
            }
        }

        /// <summary>Unsubscribes from all instantiators used by this import operation.</summary>
        public override void Dispose()
        {
            foreach (GameObjectInstantiator instantiator in instantiators)
            {
                instantiator.MeshAdded -= OnMeshAdded;
            }

            instantiators.Clear();
            processedMaterials.Clear();
        }

        /// <summary>Normalizes atlas sampling, builds breach-safe colliders, and hides source proxy geometry.</summary>
        private void OnMeshAdded(
            GameObject gameObject,
            uint nodeIndex,
            string meshName,
            MeshResult meshResult,
            uint[] joints,
            uint? rootJoint,
            float[] morphTargetWeights,
            int meshNumeration)
        {
            if (IsSunkenSubmarineSemantic(gameObject.name, meshName))
            {
                NormalizePortableAtlasTransform(gameObject);

                if (StaticHullRenderNames.Contains(gameObject.name))
                {
                    AddStaticHullMeshCollider(gameObject);
                }
            }

            if (!IsSunkenSubmarineColliderProxy(gameObject.name, meshName))
            {
                return;
            }

            if (!EntryBlockingProxyNames.Contains(gameObject.name))
            {
                if (gameObject.name == ForwardInnerWallProxyName)
                {
                    AddForwardDoorwayColliders(gameObject);
                }
                else
                {
                    AddProxyBoxCollider(gameObject);
                }
            }

            foreach (Renderer renderer in gameObject.GetComponents<Renderer>())
            {
                UnityEngine.Object.DestroyImmediate(renderer);
            }
        }

        /// <summary>Creates a local BoxCollider from one retained proxy mesh and assigns the collision-only layer.</summary>
        private static void AddProxyBoxCollider(GameObject gameObject)
        {
            MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
            if (meshFilter == null || meshFilter.sharedMesh == null)
            {
                throw new InvalidOperationException($"Collider proxy {gameObject.name} has no source mesh.");
            }

            AddBoxCollider(gameObject, meshFilter.sharedMesh.bounds.center, meshFilter.sharedMesh.bounds.size);
            gameObject.layer = GetSubmarineInteriorLayer();
        }

        /// <summary>Builds side and header boxes around a player-sized opening in the forward interior wall.</summary>
        private static void AddForwardDoorwayColliders(GameObject gameObject)
        {
            MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
            if (meshFilter == null || meshFilter.sharedMesh == null)
            {
                throw new InvalidOperationException($"Forward wall proxy {gameObject.name} has no source mesh.");
            }

            Bounds bounds = meshFilter.sharedMesh.bounds;
            float doorwayMinimumZ = bounds.max.z - ForwardDoorwayWidth;
            float doorwayMaximumY = bounds.min.y + ForwardDoorwayHeight;
            float sideWidth = doorwayMinimumZ - bounds.min.z;
            float headerHeight = bounds.max.y - doorwayMaximumY;

            if (sideWidth <= 0f || headerHeight <= 0f)
            {
                throw new InvalidOperationException(
                    $"Forward wall proxy {gameObject.name} is too small for the configured doorway.");
            }

            AddDoorwayBoxChild(
                gameObject,
                ForwardDoorwaySideColliderName,
                new Vector3(bounds.center.x, bounds.center.y, bounds.min.z + sideWidth * 0.5f),
                new Vector3(bounds.size.x, bounds.size.y, sideWidth));
            AddDoorwayBoxChild(
                gameObject,
                ForwardDoorwayHeaderColliderName,
                new Vector3(bounds.center.x, doorwayMaximumY + headerHeight * 0.5f, bounds.max.z - ForwardDoorwayWidth * 0.5f),
                new Vector3(bounds.size.x, headerHeight, ForwardDoorwayWidth));
            gameObject.layer = GetSubmarineInteriorLayer();
        }

        /// <summary>Creates one uniquely named collision-only child for a forward-wall doorway segment.</summary>
        private static void AddDoorwayBoxChild(GameObject parent, string childName, Vector3 center, Vector3 size)
        {
            GameObject colliderObject = new GameObject(childName)
            {
                layer = GetSubmarineInteriorLayer()
            };
            colliderObject.transform.SetParent(parent.transform, false);
            AddBoxCollider(colliderObject, center, size);
        }

        /// <summary>Adds one BoxCollider with explicit local center and size.</summary>
        private static void AddBoxCollider(GameObject gameObject, Vector3 center, Vector3 size)
        {
            BoxCollider boxCollider = gameObject.AddComponent<BoxCollider>();
            boxCollider.center = center;
            boxCollider.size = size;
        }

        /// <summary>Creates a collision-only child that follows the breached static hull surface without sealing it.</summary>
        private static void AddStaticHullMeshCollider(GameObject gameObject)
        {
            MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
            if (meshFilter == null || meshFilter.sharedMesh == null)
            {
                throw new InvalidOperationException($"Static hull node {gameObject.name} has no source mesh.");
            }

            GameObject colliderObject = new GameObject(gameObject.name + StaticMeshColliderSuffix)
            {
                layer = GetSubmarineInteriorLayer()
            };
            colliderObject.transform.SetParent(gameObject.transform, false);

            MeshCollider meshCollider = colliderObject.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = meshFilter.sharedMesh;
            meshCollider.convex = false;
        }

        /// <summary>Removes glTFast's redundant V-flip from the target package's shared embedded atlas materials.</summary>
        private void NormalizePortableAtlasTransform(GameObject gameObject)
        {
            foreach (Renderer renderer in gameObject.GetComponents<Renderer>())
            {
                foreach (Material material in renderer.sharedMaterials)
                {
                    if (material == null
                        || !TargetPortableMaterialNames.Contains(material.name)
                        || !processedMaterials.Add(material))
                    {
                        continue;
                    }

                    foreach (int propertyId in PortableAtlasScaleTransformPropertyIds)
                    {
                        if (material.HasProperty(propertyId))
                        {
                            material.SetVector(propertyId, PortableAtlasIdentityTransform);
                        }
                    }

                    material.DisableKeyword(TextureTransformKeyword);
                }
            }
        }

        /// <summary>Identifies render and batched nodes belonging to the delivered submarine namespace.</summary>
        private static bool IsSunkenSubmarineSemantic(string objectName, string meshName)
        {
            return StartsWithOrdinal(objectName, SubmarineSemanticPrefix)
                || StartsWithOrdinal(meshName, SubmarineSemanticPrefix)
                || StartsWithOrdinal(objectName, BatchedSubmarineSemanticPrefix)
                || StartsWithOrdinal(meshName, BatchedSubmarineSemanticPrefix);
        }

        /// <summary>Limits filtering to the delivered submarine semantic namespace and collider naming convention.</summary>
        private static bool IsSunkenSubmarineColliderProxy(string objectName, string meshName)
        {
            bool hasColliderMarker = ContainsOrdinal(objectName, ColliderMarker)
                || ContainsOrdinal(meshName, ColliderMarker);
            bool hasSubmarinePrefix = StartsWithOrdinal(objectName, SubmarineSemanticPrefix)
                || StartsWithOrdinal(meshName, SubmarineSemanticPrefix);

            return hasColliderMarker && hasSubmarinePrefix;
        }

        /// <summary>Checks a string prefix with deterministic ordinal comparison and null safety.</summary>
        private static bool StartsWithOrdinal(string value, string prefix)
        {
            return value != null && value.StartsWith(prefix, StringComparison.Ordinal);
        }

        /// <summary>Checks a substring with deterministic ordinal comparison and null safety.</summary>
        private static bool ContainsOrdinal(string value, string token)
        {
            return value != null && value.IndexOf(token, StringComparison.Ordinal) >= 0;
        }
    }
}
