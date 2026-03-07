using UnityEngine;
using System.Collections.Generic;

public class CookingWorkbench : BaseWorkbench<CookingRecipe>
{
    protected override void Awake()
    {
        base.Awake();
        objectName = "요리 작업대";
    }

    protected override void OpenSpecificUI(List<CookingRecipe> filteredList, bool isSpecialist)
    {
        if (CookingUIManager.Instance == null)
        {
            Debug.LogError("CookingUIManager가 씬에 없습니다!");
            return;
        }

        CookingUIManager.Instance.OpenUI(filteredList, isSpecialist);

        Debug.Log($"요리 작업대 오픈: {filteredList.Count}개 레시피 표시. 전문가 모드: {isSpecialist}");
    }
}
