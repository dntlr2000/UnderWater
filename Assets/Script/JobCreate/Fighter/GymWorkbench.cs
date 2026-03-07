using UnityEngine;
using System.Collections.Generic;

public class GymWorkbench : BaseWorkbench<GymExercise>
{
    protected override void Awake()
    {
        base.Awake();
        objectName = "운동 기구";
    }

    protected override void OpenSpecificUI(List<GymExercise> filteredList, bool isSpecialist)
    {
        if (GymUIManager.Instance == null)
        {
            Debug.LogError("GymUIManager가 씬에 없습니다! UI 캔버스를 확인하세요.");
            return;
        }

        GymUIManager.Instance.OpenUI(filteredList, isSpecialist);
        Debug.Log($"운동 기구 오픈: {filteredList.Count}개 운동 표시. 전문가 모드: {isSpecialist}");
    }
}