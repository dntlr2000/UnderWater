using UnityEngine;
using System.Collections.Generic;

public class CollectionWorkbench : BaseWorkbench<CollectionData>
{
    protected override void Awake()
    {
        base.Awake();
        objectName = "수집품 보관함";
    }

    protected override void OpenSpecificUI(List<CollectionData> filteredList, bool isSpecialist)
    {
        if (CollectionUIManager.Instance == null)
        {
            Debug.LogError("CollectionUIManager가 씬에 없습니다! UI 캔버스를 확인하세요.");
            return;
        }

        CollectionUIManager.Instance.OpenUI(filteredList, isSpecialist);
        Debug.Log($"수집품 보관함 오픈: {filteredList.Count}개 항목 표시. 전문가 모드: {isSpecialist}");
    }
}