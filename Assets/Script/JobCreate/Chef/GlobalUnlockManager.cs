using UnityEngine;
using Photon.Pun;
using System.Collections.Generic;
using ExitGames.Client.Photon;

// 모든 직업의 요리, 장비, 운동, 수집품 해금을 통합 관리하는 싱글톤
public class GlobalUnlockManager : MonoBehaviourPunCallbacks
{
    public static GlobalUnlockManager Instance;

    // 로컬 캐시 (여기에 "Cook_01", "Equip_05" 등 모든 ID가 다 담깁니다)
    private HashSet<string> unlockedItemIDs = new HashSet<string>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        if (propertiesThatChanged.ContainsKey("UnlockedItems"))
        {
            string[] ids = (string[])propertiesThatChanged["UnlockedItems"];
            unlockedItemIDs = new HashSet<string>(ids);
            Debug.Log($"[해금 동기화] 총 {unlockedItemIDs.Count}개의 항목이 해금되었습니다.");
        }
    }

    // [공용] 특정 아이템/레시피를 해금하는 함수 (퀘스트 완료 시 호출)
    public void UnlockItem(string id)
    {
        // 빈 ID이거나 이미 해금되었다면 무시
        if (string.IsNullOrEmpty(id) || unlockedItemIDs.Contains(id)) return;

        unlockedItemIDs.Add(id);
        Debug.Log($"[해금 완료] ID: {id}");

        // 방에 있다면 모든 유저에게 동기화 (마스터/일반 클라이언트 상관없이 갱신)
        if (PhotonNetwork.InRoom)
        {
            string[] idArray = new string[unlockedItemIDs.Count];
            unlockedItemIDs.CopyTo(idArray);

            Hashtable props = new Hashtable { { "UnlockedItems", idArray } };
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        }
    }

    // [조회용] 특정 ID가 해금되었는지 확인
    public bool IsUnlocked(string id)
    {
        // 팁: ID를 아예 안 적어둔(비워둔) 기본 아이템은 항상 해금된 것으로 처리합니다.
        if (string.IsNullOrEmpty(id)) return true;

        return unlockedItemIDs.Contains(id);
    }
}