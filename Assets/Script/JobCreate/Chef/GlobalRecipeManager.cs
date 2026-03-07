using UnityEngine;
using Photon.Pun;
using System.Collections.Generic;
using ExitGames.Client.Photon;

// 싱글톤 + 포톤 콜백
public class GlobalRecipeManager : MonoBehaviourPunCallbacks
{
    public static GlobalRecipeManager Instance;

    // 로컬에서 빠르게 확인하기 위한 캐시 리스트
    private HashSet<string> unlockedRecipeIDs = new HashSet<string>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    // 게임 시작 시 혹은 룸 접속 시 포톤에서 데이터 가져오기
    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        if (propertiesThatChanged.ContainsKey("UnlockedRecipes"))
        {
            string[] ids = (string[])propertiesThatChanged["UnlockedRecipes"];
            unlockedRecipeIDs = new HashSet<string>(ids);
            Debug.Log("레시피 해금 정보 동기화 완료");
        }
    }

    // [요리사 전용] 레시피 해금 함수
    public void UnlockRecipe(string id)
    {
        if (unlockedRecipeIDs.Contains(id)) return;

        unlockedRecipeIDs.Add(id);

        // 포톤 룸 프로퍼티 업데이트 (모든 유저에게 전송)
        if (PhotonNetwork.IsMasterClient || PhotonNetwork.InRoom) // 방어 코드
        {
            string[] idArray = new string[unlockedRecipeIDs.Count];
            unlockedRecipeIDs.CopyTo(idArray);

            Hashtable props = new Hashtable { { "UnlockedRecipes", idArray } };
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        }
    }

    // [조회용] 특정 레시피가 현재 해금되어 있는가?
    public bool IsRecipeUnlocked(string id)
    {
        // 기본적으로 1번은 항상 해금되어 있다고 가정하려면 || id == "Recipe_01" 추가
        return unlockedRecipeIDs.Contains(id);
    }
}