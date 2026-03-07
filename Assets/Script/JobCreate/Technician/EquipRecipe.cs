using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewEquipRecipe", menuName = "Workbench Data/Equip Recipe")]
public class EquipRecipe : WorkbenchData
{
    [TextArea]
    public string recipeDescription;
    public float craftTime;

    [Header("Ingredients (필요 재료)")]
    public List<RecipeIngredient> ingredients = new List<RecipeIngredient>();

    [Header("Result (완성된 장비)")]
    public int resultEquipID;     // 장비 아이템 ID
    public int resultAmount = 1;  // 제작 시 나오는 개수 (화살 같은 건 여러 개 나올 수 있으니까요!)
}