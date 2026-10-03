using Photon.Pun;
using System.Collections;
using UnityEngine;

public class SellBox : OpenableStorageBox
{
    [Header("SellBox Options")]
    public float sellTimer = 60f; //판매 대기 시간
    public float passedTime = 0f; //판매 경과 시간

    // 기존 판매 주기를 유지하며 방장에서만 타이머를 진행합니다.
    void Update()
    {
        //방장만 연산 & 데이터 로드 전에는 대기 (Null 에러 방지)
        if (!PhotonNetwork.IsMasterClient || storageData == null || storageData.id == null) return;

        //상자에 아이템이 하나라도 있는지 확인
        bool hasItem = !storageData.CheckInventoryEmpty();

        //아이템이 있다면 타이머를 굴리고, 없다면 즉시 초기화
        if (hasItem)
        {
            passedTime += Time.deltaTime;

            // 타이머가 다 되면 판매!
            if (passedTime >= sellTimer)
            {
                SellItems();
            }
        }
        else
        {
            passedTime = 0f;
        }
    }

    // 출고 응답을 기다리는 물건은 판매하지 않고 확정 뒤 다음 판매 주기에 처리합니다.
    public void SellItems()
    {
        var service = RewardDeliveryService.Instance;
        if (!PhotonNetwork.IsMasterClient || service == null || service.HasReservedWithdrawals(boxName) ||
            !service.TryGetBoxSnapshot(this, out var data, out int revision)) return;
        int inventoryLength = data.id.Length;

        for (int i = 0; i < inventoryLength; i++)
        {
            if (data.id[i] == -1) continue;

            data.money += ItemDatabase.Instance.getPrice(data.id[i]) * data.quantity[i];

            //슬롯 초기화
            data.id[i] = -1;
            data.quantity[i] = 0;
            data.durability[i] = -1f;
        }

        //타이머 초기화 및 네트워크 동기화
        if (!service.PublishBox(this, revision, data)) return;
        passedTime = 0f;
        Debug.Log($"[SellBox] 아이템 자동 판매 완료! 현재 창고 돈: {storageData.money}G");
    }
}
