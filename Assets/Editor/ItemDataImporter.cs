using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class ItemDataImporter
{
    private const string TSV_PATH = "Assets/Resources/Data/TSV/03_Items.txt";
    private const string OUTPUT_FOLDER = "Assets/Resources/Data/ItemData";

    // 기존 전체 가져오기 메뉴는 같은 검증 경로를 사용합니다.
    [MenuItem("Overflown/Import Items From TSV")]
    public static void ImportItems() => ImportSelected(null);

    // 지정된 ID만 가져올 수 있어 관련 없는 수동 설정을 일괄 변경하지 않습니다.
    public static void ImportSelected(ICollection<int> itemIds)
    {
        if (!File.Exists(TSV_PATH))
        {
            Debug.LogError($"[ItemDataImporter] TSV 파일을 찾을 수 없습니다: {TSV_PATH}");
            return;
        }

        if (!Directory.Exists(OUTPUT_FOLDER))
        {
            Directory.CreateDirectory(OUTPUT_FOLDER);
            AssetDatabase.Refresh();
        }

        string tsvText = File.ReadAllText(TSV_PATH);
        List<Dictionary<string, string>> rows = TSVParser.Parse(tsvText);

        int created = 0, updated = 0, skipped = 0;

        foreach (var row in rows)
        {
            string itemID = TSVParser.Get(row, "itemID");
            if (string.IsNullOrEmpty(itemID))
            {
                skipped++;
                continue;
            }

            int legacyItemId = TSVParser.GetInt(row, "legacyItemId", -1);
            if (legacyItemId == -1)
            {
                Debug.LogWarning($"[ItemDataImporter] legacyItemId가 비어있어 건너뜀: {itemID}");
                skipped++;
                continue;
            }

            if (itemIds != null && !itemIds.Contains(legacyItemId)) continue;

            string itemType = TSVParser.Get(row, "itemType");
            string assetPath = FindExistingAssetPath(legacyItemId);

            ItemData target;
            bool isNew = false;

            if (!string.IsNullOrEmpty(assetPath))
            {
                target = AssetDatabase.LoadAssetAtPath<ItemData>(assetPath);
                updated++;
            }
            else
            {
                target = CreateItemInstance(itemType);
                assetPath = $"{OUTPUT_FOLDER}/{itemID}.asset";
                isNew = true;
                created++;
            }

            ApplyCommonFields(target, row, legacyItemId);
            ApplyTypeSpecificFields(target, row);

            if (isNew)
            {
                AssetDatabase.CreateAsset(target, assetPath);
            }
            else
            {
                EditorUtility.SetDirty(target);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[ItemDataImporter] 완료 생성: {created}, 갱신: {updated}, 스킵: {skipped}");
    }

    // 기존 GUID와 파생 ScriptableObject 타입을 유지할 대상을 실제 ID로 찾습니다.
    private static string FindExistingAssetPath(int legacyItemId)
    {
        string[] guids = AssetDatabase.FindAssets("t:ItemData", new[] { OUTPUT_FOLDER });
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ItemData data = AssetDatabase.LoadAssetAtPath<ItemData>(path);
            if (data != null && data.itemId == legacyItemId)
                return path;
        }
        return null;
    }

    // 새 항목만 TSV의 분류에 맞는 기본 데이터 타입을 생성합니다.
    private static ItemData CreateItemInstance(string itemType)
    {
        switch (itemType)
        {
            case "Consumable":
                return ScriptableObject.CreateInstance<FoodItem>();
            case "Equipment":
                return ScriptableObject.CreateInstance<EquipableItem>();
            default:
                return ScriptableObject.CreateInstance<ItemData>();
        }
    }

    // 이미지 경로를 Sprite 참조로 연결하고 기존 공통 속성을 가져옵니다.
    private static void ApplyCommonFields(ItemData target, Dictionary<string, string> row, int legacyItemId)
    {
        target.itemName = TSVParser.Get(row, "displayName");
        target.itemId = legacyItemId;
        target.stringID = TSVParser.Get(row, "itemID");
        target.description = TSVParser.Get(row, "description");
        target.modelPath = TSVParser.Get(row, "modelPath");
        string iconPath = TSVParser.Get(row, "iconPath");
        if (!string.IsNullOrEmpty(iconPath))
        {
            var icon = Resources.Load<Sprite>(iconPath);
            if (icon != null) target.itemIcon = icon;
            else Debug.LogWarning($"[ItemDataImporter] 아이콘 경로를 확인하세요. 기존 참조는 유지합니다: {iconPath}");
        }
        target.equipEffectType = TSVParser.Get(row, "equipEffectType");
        target.price = TSVParser.GetInt(row, "price");
        target.weight = TSVParser.GetFloat(row, "weight");
        target.damage = TSVParser.GetFloat(row, "damage", 10f);

        string equipFlag = TSVParser.Get(row, "itemType");
        target.type = equipFlag == "Equipment" ? "equipable" : "item";
    }

    // 정의된 즉시 회복 효과만 가져오며 별도 버프 시스템의 효과는 임의로 구현하지 않습니다.
    private static void ApplyTypeSpecificFields(ItemData target, Dictionary<string, string> row)
    {
        if (!(target is FoodItem food)) return;
        const string path = "Assets/Resources/Data/TSV/04_ItemEffects.txt";
        if (!File.Exists(path)) return;
        var effects = TSVParser.Parse(File.ReadAllText(path))
            .Where(e => TSVParser.Get(e, "itemID") == target.stringID &&
                new[] { "HealHP", "RestoreHunger", "RestoreThirst" }.Contains(TSVParser.Get(e, "effectType"))).ToList();
        if (effects.Count == 0) return;
        food.health = food.hunger = food.thirst = 0;
        foreach (var effect in effects)
        {
            float value = TSVParser.GetFloat(effect, "value");
            switch (TSVParser.Get(effect, "effectType"))
            {
                case "HealHP": food.health += value; break;
                case "RestoreHunger": food.hunger += value; break;
                case "RestoreThirst": food.thirst += value; break;
            }
            food.discountAmount = Math.Max(1, TSVParser.GetInt(effect, "discountAmount", 1));
        }
    }
}
