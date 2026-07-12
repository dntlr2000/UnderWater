using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class ItemDataImporter
{
    private const string TSV_PATH = "Assets/Resources/Data/TSV/03_Items.txt";
    private const string OUTPUT_FOLDER = "Assets/Resources/Data/ItemData";

    [MenuItem("Overflown/Import Items From TSV")]
    public static void ImportItems()
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

    private static string FindExistingAssetPath(int legacyItemId)
    {
        string[] guids = AssetDatabase.FindAssets("t:ItemData");
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ItemData data = AssetDatabase.LoadAssetAtPath<ItemData>(path);
            if (data != null && data.itemId == legacyItemId)
                return path;
        }
        return null;
    }

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

    private static void ApplyCommonFields(ItemData target, Dictionary<string, string> row, int legacyItemId)
    {
        target.itemName = TSVParser.Get(row, "displayName");
        target.itemId = legacyItemId;
        target.stringID = TSVParser.Get(row, "itemID");
        target.description = TSVParser.Get(row, "description");
        target.modelPath = TSVParser.Get(row, "modelPath");
        target.equipEffectType = TSVParser.Get(row, "equipEffectType");
        target.price = TSVParser.GetInt(row, "price");
        target.weight = TSVParser.GetFloat(row, "weight");
        target.damage = TSVParser.GetFloat(row, "damage", 10f);

        string equipFlag = TSVParser.Get(row, "itemType");
        target.type = equipFlag == "Equipment" ? "equipable" : "item";
    }

    private static void ApplyTypeSpecificFields(ItemData target, Dictionary<string, string> row)
    {
        if (target is FoodItem food)
        {
            // 04_ItemEffects는 별도 시트이므로 1차 검증에서는 기본값만 유지
            // 추후 단계에서 ItemEffects 연동 추가 예정
        }
    }
}