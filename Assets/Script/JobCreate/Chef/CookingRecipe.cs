using System.Collections.Generic;
using UnityEngine;

[System.Serializable] // 인스펙터 창에서 보이게 하려면 필수입니다!
public class RecipeIngredient
{
    public int itemID;          // 인벤토리에서 검사할 실제 아이템 ID
    public string itemName;     // UI에 표시할 이름 (예: "신선한 허브")
    public Sprite itemIcon;     // UI에 표시할 아이콘
    public int requiredAmount;  // 요리에 필요한 개수
}

[CreateAssetMenu(fileName = "NewCookingRecipe", menuName = "Workbench Data/Cooking Recipe")]
public class CookingRecipe : WorkbenchData
{
    public string recipeDescription;
    public float cookTime;

    [Header("Ingredients (필요 재료)")]
    public List<RecipeIngredient> ingredients = new List<RecipeIngredient>();

    [Header("Result (결과물)")]
    public int resultItemID;      // 완성된 요리의 아이템 ID
    public int resultAmount = 1;  // 완성 시 몇 개를 주는지
}
