using Photon.Pun;
using System.Collections;
using UnityEngine;

public class SellBox : OpenableStorageBox
{
    [Header("SellBox Options")]
    public float sellTimer = 60f; //판매 대기 시간
    public float passedTime = 0f; //판매 경과 시간

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

    public void SellItems()
    {
        int inventoryLength = storageData.id.Length;

        for (int i = 0; i < inventoryLength; i++)
        {
            if (storageData.id[i] == -1) continue;

            storageData.money += ItemDatabase.Instance.getPrice(storageData.id[i]) * storageData.quantity[i];

            //슬롯 초기화
            storageData.id[i] = -1;
            storageData.quantity[i] = 0;
            storageData.durability[i] = -1f;
        }

        //타이머 초기화 및 네트워크 동기화
        passedTime = 0f;
        SyncDataToAll();
        Debug.Log($"[SellBox] 아이템 자동 판매 완료! 현재 창고 돈: {storageData.money}G");
    }
}