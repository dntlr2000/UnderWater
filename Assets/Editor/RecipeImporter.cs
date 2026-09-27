using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class RecipeImporter
{
    private const string RECIPE_TSV_PATH = "Assets/Resources/Data/TSV/11_Recipes.txt";
    private const string INGREDIENT_TSV_PATH = "Assets/Resources/Data/TSV/12_RecipeIngredients.txt";
    private const string OUTPUT_FOLDER = "Assets/Resources/Data/CreateBenchData";

    [MenuItem("Overflown/Import Recipes From TSV")]
    public static void ImportRecipes()
    {
        if (!File.Exists(RECIPE_TSV_PATH))
        {
            Debug.LogError($"[RecipeImporter] TSV 없음: {RECIPE_TSV_PATH}");
            return;
        }

        if (!Directory.Exists(OUTPUT_FOLDER))
        {
            Directory.CreateDirectory(OUTPUT_FOLDER);
            AssetDatabase.Refresh();
        }

        // 재료 시트를 recipeID 기준으로 그룹핑
        var ingredientRows = ParseTSV(INGREDIENT_TSV_PATH);
        var ingredientMap = ingredientRows
            .GroupBy(r => TSVParser.Get(r, "recipeID"))
            .ToDictionary(g => g.Key, g => g.OrderBy(r => TSVParser.GetInt(r, "seq")).ToList());

        var recipeRows = ParseTSV(RECIPE_TSV_PATH);
        Debug.Log($"[RecipeImporter] 파싱된 레시피 행 수: {recipeRows.Count}");

        int created = 0, updated = 0, skipped = 0;

        foreach (var row in recipeRows)
        {
            string recipeID = TSVParser.Get(row, "recipeID");
            Debug.Log($"[RecipeImporter] recipeID: '{recipeID}'");
            if (string.IsNullOrEmpty(recipeID)) { skipped++; continue; }

            string workbenchID = TSVParser.Get(row, "workbenchID");
            string resultItemID = TSVParser.Get(row, "resultItemID");
            int resultAmount = TSVParser.GetInt(row, "resultAmount", 1);
            float craftTimeSec = TSVParser.GetFloat(row, "craftTimeSec");

            // 결과 아이템 itemId(int) 찾기
            int resultItemIntID = FindItemIntID(resultItemID);

            // 기존 에셋 찾기
            string assetPath = $"{OUTPUT_FOLDER}/{recipeID}.asset";
            CookingRecipe recipe = AssetDatabase.LoadAssetAtPath<CookingRecipe>(assetPath);

            bool isNew = recipe == null;
            if (isNew) recipe = ScriptableObject.CreateInstance<CookingRecipe>();

            // 기본 정보
            recipe.id = recipeID;
            recipe.displayName = TSVParser.Get(row, "displayName");
            recipe.recipeDescription = TSVParser.Get(row, "description");
            recipe.cookTime = craftTimeSec;
            recipe.resultItemID = resultItemIntID;
            recipe.resultAmount = resultAmount;
            recipe.isBasic = string.IsNullOrEmpty(TSVParser.Get(row, "requiredJob"));

            // 재료 목록 생성
            recipe.ingredients = new List<RecipeIngredient>();
            if (ingredientMap.TryGetValue(recipeID, out var ingRows))
            {
                foreach (var ingRow in ingRows)
                {
                    string ingStringID = TSVParser.Get(ingRow, "itemID");
                    int ingIntID = FindItemIntID(ingStringID);
                    ItemData ingItemData = FindItemData(ingStringID);

                    recipe.ingredients.Add(new RecipeIngredient
                    {
                        itemID = ingIntID,
                        itemName = ingItemData?.itemName ?? ingStringID,
                        itemIcon = ingItemData?.itemIcon,
                        requiredAmount = TSVParser.GetInt(ingRow, "amount", 1),
                    });
                }
            }

            if (isNew)
            {
                AssetDatabase.CreateAsset(recipe, assetPath);
                created++;
            }
            else
            {
                EditorUtility.SetDirty(recipe);
                updated++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[RecipeImporter] 완료 생성:{created}, 갱신:{updated}, 스킵:{skipped}");
    }

    private static int FindItemIntID(string stringID)
    {
        if (string.IsNullOrEmpty(stringID)) return -1;
        string[] guids = AssetDatabase.FindAssets("t:ItemData");
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ItemData data = AssetDatabase.LoadAssetAtPath<ItemData>(path);
            if (data != null && data.stringID == stringID)
                return data.itemId;
        }
        return -1;
    }

    private static ItemData FindItemData(string stringID)
    {
        if (string.IsNullOrEmpty(stringID)) return null;
        string[] guids = AssetDatabase.FindAssets("t:ItemData");
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ItemData data = AssetDatabase.LoadAssetAtPath<ItemData>(path);
            if (data != null && data.stringID == stringID)
                return data;
        }
        return null;
    }

    private static List<Dictionary<string, string>> ParseTSV(string path)
    {
        if (!File.Exists(path)) return new List<Dictionary<string, string>>();
        return TSVParser.Parse(File.ReadAllText(path));
    }
}
