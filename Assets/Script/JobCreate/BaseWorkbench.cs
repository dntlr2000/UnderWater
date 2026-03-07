using UnityEngine;
using System.Collections.Generic;

public abstract class BaseWorkbench<T> : InteractableObject where T : WorkbenchData
{
    [Header("Base Settings")]
    public JobType ownerJob; //작업대 주인이 누구야
    public List<T> dataList;
    
    protected abstract void OpenSpecificUI(List<T> filteredList, bool isSpecialist);

    public override void Interact()
    {
        if (!Input.GetKeyDown(KeyCode.E)) return;

        // 여기서부터는 E키를 누른 딱 그 프레임에 1번만 실행
        Debug.Log("<color=yellow>E키를 눌렀습니다! 작업대 로직 시작!</color>");

        if (Player.localPlayer == null)
        {
            Debug.LogError("Player.localPlayer가 없습니다!");
            return;
        }

        JobType userJob = Player.localPlayer.CurrentJobType;
        bool isSpecialist = (userJob == ownerJob);

        Debug.Log($"내 직업: {userJob}, 작업대 주인: {ownerJob}, 전문가 모드: {isSpecialist}");

        //데이터 필터링
        List<T> availableData = new List<T>();

        foreach (var data in dataList)
        {
            /*if (GlobalRecipeManager.Instance == null || !GlobalRecipeManager.Instance.IsRecipeUnlocked(data.id))
                continue;*/

            if (isSpecialist)
            {
                availableData.Add(data);
            }
            else
            {
                if (data.isBasic)
                {
                    availableData.Add(data);
                }
            }
        }

        // 자식 클래스에게 "필터링된 데이터"와 "권한"을 넘겨주며 UI 오픈 요청
        OpenSpecificUI(availableData, isSpecialist);
    }

    public override InteractionType GetInteractionType() => InteractionType.Instant;
}
