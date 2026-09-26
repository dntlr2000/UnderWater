using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Photon.Pun;
using UnityEditor;
using UnityEngine;

public static class ItemAssetValidation
{
    private static readonly List<string> results = new();

    // 실제 Unity 임포트 결과와 JSON/아이템 계산을 사용자 씬이나 저장을 변경하지 않고 검사합니다.
    [MenuItem("Overflown/Items/Validate Six Model Items")]
    public static void Run()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode before validating assets.");
        results.Clear();
        const BindingFlags flags = BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic;
        var instanceField = typeof(ItemDatabase).GetField("_instance", flags);
        var original = instanceField.GetValue(null);
        var fixture = new GameObject("Item validation") { hideFlags = HideFlags.HideAndDontSave };
        fixture.SetActive(false);
        try
        {
            var database = fixture.AddComponent<ItemDatabase>();
            typeof(ItemDatabase).GetMethod("LoadAllItems", flags).Invoke(database, null);
            instanceField.SetValue(null, database);
            ValidateAssets(database);
            ValidateTrades(database);
            WriteResults();
            Debug.Log($"[ItemAssetValidation] {results.Count} checks passed.");
        }
        catch (Exception exception)
        {
            results.Add("FAIL " + exception);
            WriteResults();
            throw;
        }
        finally
        {
            instanceField.SetValue(null, original);
            UnityEngine.Object.DestroyImmediate(fixture);
        }
    }

    // 중복 ID, 이미지 참조, 모델 렌더러 및 상호작용/네트워크 프리팹 구성을 확인합니다.
    private static void ValidateAssets(ItemDatabase database)
    {
        Check(database.itemDatas.Select(d => d.itemId).Distinct().Count() == database.itemDatas.Count, "Unique numeric item IDs");
        foreach (int id in ItemAssetBuilder.ItemIds)
        {
            var item = database.GetItem(id);
            Check(item != null && item.itemIcon != null && item.itemIcon.texture.width == 1000 && item.itemIcon.texture.height == 1000, "1000px sprite " + id);
            Check(item.durability == -1 && !item.sigularity && item.price > 0, "Non-durable stackable item " + id);
            var prefab = Resources.Load<GameObject>($"FieldItem/Object{id}");
            Check(prefab != null && prefab.GetComponent<FieldItem>()?.itemID == id, "Correct pickup prefab " + id);
            var field = prefab.GetComponent<FieldItem>();
            Check(field.usePhoton && field.amount == 1 && field.durability == -1 && field.PrefabPath == $"FieldItem/Object{id}", "Pickup and save paths " + id);
            Check(prefab.GetComponent<Rigidbody>() != null && prefab.GetComponent<BoxCollider>()?.size.sqrMagnitude > 0, "Physics root " + id);
            var view = prefab.GetComponent<PhotonView>();
            Check(view != null && view.ObservedComponents.Count == 1 && view.ObservedComponents[0] == prefab.GetComponent<PhotonTransformView>(), "Photon observation reference " + id);
            var renderers = prefab.GetComponentsInChildren<Renderer>(true);
            Check(renderers.Length > 0 && renderers.All(r => r.sharedMaterials.Length > 0 && r.sharedMaterials.All(m => m != null && m.shader != null)), "Imported model and materials " + id);
            var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(item.itemIcon)) as TextureImporter;
            Check(importer != null && importer.textureType == TextureImporterType.Sprite && importer.alphaIsTransparency && !importer.mipmapEnabled, "UI texture import settings " + id);
            Check(Resources.Load<GameObject>(item.modelPath) != null, "Model resource path " + id);
        }
        var catalog = Resources.Load<ShopCatalog>("Data/ShopCatalog");
        Check(catalog != null && catalog.entries.Count == 11 && catalog.entries.Select(e => e.item.itemId).Distinct().Count() == 11, "Eleven unique shop products");
        Check(catalog.Find(0) == null && ItemAssetBuilder.ItemIds.All(id => catalog.Find(id) != null), "No placeholder and all six products registered");
        Check(catalog.Find(6).InitialDurability == 80, "Existing oxygen stock durability preserved");
        Check(database.GetItem(12) is FoodItem water && water.thirst == 40 && water.hunger == 0, "Water effect definition");
        Check(database.GetItem(16) is FoodItem bar && bar.hunger == 30 && bar.thirst == 0, "Energy bar effect definition");
    }

    // 실제 Unity JSON 복사본으로 거래 실패·수량·중복 확인·저장 복원을 검증합니다.
    private static void ValidateTrades(ItemDatabase database)
    {
        var catalog = Resources.Load<ShopCatalog>("Data/ShopCatalog");
        foreach (int id in ItemAssetBuilder.ItemIds)
        {
            var wallet = Empty(25, 10000);
            var mailbox = Empty(2);
            Check(ShopTransactions.TryPurchase(wallet, mailbox, catalog.Find(id), 3, "native/" + id, out var paid, out var stocked, out _)
                && paid.money == 10000 - database.GetItem(id).price * 6 && stocked.quantity[0] == 3 && stocked.id[0] == id, "Native purchase " + id);
            Check(wallet.money == 10000 && mailbox.id[0] == -1, "Source preserved before commit " + id);
            Check(ShopTransactions.TrySell(stocked, 0, id, 3, 2, out var sold) && sold.id[0] == -1 && sold.durability[0] == -1
                && sold.money == ShopTransactions.SellPrice(database.GetItem(id), 3), "Native sale " + id);
        }
        var full = Empty(1); full.id[0] = 1; full.quantity[0] = 1;
        Check(!ShopTransactions.TryPurchase(Empty(25, 100), full, catalog.Find(20), 1, "full", out var debit, out var credit, out _)
            && debit == null && credit == null && full.id[0] == 1, "Full mailbox changes neither side");
        var records = new List<ShopPurchaseRecord> { new() { requestId = "confirmed", buyerId = "owner", success = true, cost = 20 } };
        var buyer = Empty(25, 100);
        Check(ShopTransactions.ApplyPurchases(buyer, "owner", records) && buyer.money == 80, "Apply own confirmed payment");
        var loaded = JsonUtility.FromJson<InventoryData>(JsonUtility.ToJson(buyer));
        loaded.money += 10;
        Check(!ShopTransactions.ApplyPurchases(loaded, "owner", records) && loaded.money == 90, "Reload and repeated confirmation preserve later income");
        Check(ShopTransactions.SellPrice(database.GetItem(9), 10) == 12, "Bulk seaweed rounding preserved");
        Check(new[] { 9, 13, 18, 20 }.All(id => database.GetItem(id).Use(null, 5) == 5), "Materials and undefined raw food are not consumed");
    }

    // 슬롯이 비어 있는 검증용 인벤토리를 메모리에 만듭니다.
    private static InventoryData Empty(int slots, int money = 0)
    {
        var data = new InventoryData();
        data.GenerateData(slots);
        data.money = money;
        return data;
    }

    // 실패한 조건의 이름을 예외와 검증 로그에 함께 남깁니다.
    private static void Check(bool condition, string description)
    {
        if (!condition) throw new InvalidOperationException(description);
        results.Add("PASS " + description);
    }

    // 명시적으로 지정한 E 드라이브 결과 경로만 사용하며 메뉴 실행은 로그만 표시합니다.
    private static void WriteResults()
    {
        string path = Environment.GetEnvironmentVariable("ITEM_VALIDATION_OUTPUT");
        if (string.IsNullOrEmpty(path)) return;
        path = Path.GetFullPath(path);
        if (!path.StartsWith("E:\\CodexValidation\\", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Validation output must be under E:\\CodexValidation.");
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllLines(path, results);
    }
}
