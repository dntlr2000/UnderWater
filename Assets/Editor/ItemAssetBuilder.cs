using System;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using UnityEditor;
using UnityEngine;

public static class ItemAssetBuilder
{
    public static readonly int[] ItemIds = { 9, 12, 13, 16, 18, 20 };
    private static readonly string[] Models = { "Seaweed_BAG", "WaterBottle_BAG", "SilverFish_BAG", "EnergyBar_BAG", "TealGem_BAG", "Item_Cloth_BAG" };

    // 렌더된 PNG를 가져온 뒤 여섯 아이템의 데이터·필드 프리팹·상점 목록을 연결합니다.
    [MenuItem("Overflown/Items/Build Six Model Items")]
    public static void Build()
    {
        foreach (int id in ItemIds) ImportIcon(id);
        ItemDataImporter.ImportSelected(ItemIds);
        var data = Resources.LoadAll<ItemData>("Data/ItemData").ToDictionary(i => i.itemId);
        var template = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/FieldItem/Object1.prefab");
        if (template == null) throw new InvalidOperationException("FieldItem template is missing.");
        for (int i = 0; i < ItemIds.Length; i++)
        {
            var item = data[ItemIds[i]];
            item.itemIcon = Resources.Load<Sprite>($"Item/Item{item.itemId}");
            item.modelPath = "FBX/Items/" + Models[i];
            item.durability = -1;
            item.sigularity = false;
            item.damage = 0;
            EditorUtility.SetDirty(item);
            BuildField(item, template);
        }
        BuildCatalog(data);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[ItemAssetBuilder] Six icons, item definitions, field prefabs and shop entries are ready.");
    }

    // 기존 UI가 사용하는 개별 Sprite/texture 규칙과 투명 배경을 적용합니다.
    private static void ImportIcon(int id)
    {
        string path = $"Assets/Resources/Item/Item{id}.png";
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) throw new InvalidOperationException("Render the icon first: " + path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 100;
        importer.spritePivot = new Vector2(0.5f, 0.5f);
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 1024;
        importer.SaveAndReimport();
    }

    // 원본 GLB를 자식으로 유지하고 모델 범위에 맞는 상호작용 루트를 만듭니다.
    private static void BuildField(ItemData item, GameObject template)
    {
        var model = Resources.Load<GameObject>(item.modelPath);
        if (model == null) throw new InvalidOperationException("Imported GLB is missing: " + item.modelPath);
        var root = new GameObject("Object" + item.itemId) { layer = template.layer, tag = template.tag };
        try
        {
            var visual = (GameObject)PrefabUtility.InstantiatePrefab(model, root.transform);
            visual.name = "Model";
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;
            var renderers = visual.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) throw new InvalidOperationException("No model renderer: " + item.itemName);
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
            visual.transform.localPosition = new Vector3(-bounds.center.x, -bounds.min.y, -bounds.center.z);
            var body = CopyComponent<Rigidbody>(template, root);
            body.mass = Mathf.Max(0.1f, item.weight);
            var collider = root.AddComponent<BoxCollider>();
            collider.center = new Vector3(0, bounds.size.y * 0.5f, 0);
            collider.size = Vector3.Max(bounds.size, new Vector3(0.08f, 0.04f, 0.08f));
            var field = CopyComponent<FieldItem>(template, root);
            field.itemID = item.itemId;
            field.amount = 1;
            field.durability = -1;
            field.objectName = item.itemName;
            field.usePhoton = true;
            var view = CopyComponent<PhotonView>(template, root);
            var movement = CopyComponent<PhotonTransformView>(template, root);
            view.ObservedComponents = new List<Component> { movement };
            view.observableSearch = PhotonView.ObservableSearch.Manual;
            var overlay = CopyComponent<InvertedHullOverlay>(template, root);
            overlay.shellOffset = Mathf.Min(0.004f, bounds.size.magnitude * 0.005f);
            PrefabUtility.SaveAsPrefabAsset(root, $"Assets/Resources/FieldItem/Object{item.itemId}.prefab");
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    // 기존 필드 컴포넌트의 직렬화 설정을 새 루트에 복사합니다.
    private static T CopyComponent<T>(GameObject source, GameObject target) where T : Component
    {
        var original = source.GetComponent<T>();
        if (original == null) throw new InvalidOperationException("Template component missing: " + typeof(T).Name);
        var component = target.AddComponent<T>();
        EditorUtility.CopySerialized(original, component);
        return component;
    }

    // 기존 실제 상품과 신규 여섯 품목을 중복 없이 등록하고 기존 산소통 초기 내구도를 보존합니다.
    private static void BuildCatalog(Dictionary<int, ItemData> data)
    {
        const string path = "Assets/Resources/Data/ShopCatalog.asset";
        var catalog = AssetDatabase.LoadAssetAtPath<ShopCatalog>(path);
        if (catalog == null) { catalog = ScriptableObject.CreateInstance<ShopCatalog>(); AssetDatabase.CreateAsset(catalog, path); }
        foreach (int id in new[] { 1, 2, 3, 4, 6 }.Concat(ItemIds))
        {
            if (catalog.Find(id) != null) continue;
            catalog.entries.Add(new ShopCatalogEntry { item = data[id], overrideDurability = id == 6, durability = id == 6 ? 80 : -1 });
        }
        catalog.entries.RemoveAll(e => e?.item == null || e.item.itemId == 0);
        EditorUtility.SetDirty(catalog);
    }
}
