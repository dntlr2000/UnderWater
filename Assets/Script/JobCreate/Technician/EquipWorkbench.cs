using UnityEngine;
using System.Collections.Generic;

public class EquipWorkbench : BaseWorkbench<EquipRecipe>
{
    protected override void Awake()
    {
        base.Awake();
        objectName = "장비 제작대";
    }

    protected override void OpenSpecificUI(List<EquipRecipe> filteredList, bool isSpecialist)
    {
        if (EquipUIManager.Instance == null)
        {
            Debug.LogError("EquipUIManager가 씬에 없습니다! UI 캔버스를 확인하세요.");
            return;
        }

        EquipUIManager.Instance.OpenUI(filteredList, isSpecialist);
        Debug.Log($"장비 제작대 오픈: {filteredList.Count}개 레시피 표시. 전문가 모드: {isSpecialist}");
    }
}