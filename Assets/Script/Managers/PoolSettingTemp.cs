// 파일 이름을 NetworkedItemSpawner.cs 등으로 바꾸는 것을 추천합니다.
using UnityEngine;
using System.Collections;
using Photon.Pun;

public class PoolSettingTemp : MonoBehaviourPunCallbacks // MonoBehaviourPunCallbacks 상속
{
    public GameObject itemPrefab; // Inspector에서 FieldItem 프리팹을 할당

    void Start()
    {
        // 모든 클라이언트는 시작 시 동일한 풀을 생성합니다 (비활성화 상태로).
        // "Item" 이라는 키는 FieldItem의 OnDisable()에서 사용하는 키와 같아야 합니다.
        PoolManager.Instance.CreatePool("Item", itemPrefab, 10);

        // --- 마스터 클라이언트만 스폰 루프를 실행합니다. ---
        if (PhotonNetwork.IsMasterClient)
        {
            StartCoroutine(SpawnLoop());
        }
    }

    // 아이템 스폰을 '명령'하는 메소드
    void SpawnItem(Vector3 location)
    {
        // 로컬에서 스폰하는 대신, 모든 클라이언트에게 스폰하라는 RPC를 보냅니다.
        photonView.RPC("PunRPC_SpawnItemFromPool", RpcTarget.All, location);
    }

    // 이 RPC는 모든 클라이언트에서 실행됩니다.
    [PunRPC]
    void PunRPC_SpawnItemFromPool(Vector3 location)
    {
        // 각 클라이언트는 자신의 로컬 풀에서 아이템을 하나 꺼냅니다.
        GameObject item = PoolManager.Instance.GetFromPool("Item");
        if (item != null)
        {
            item.transform.position = location;
            item.SetActive(true);
            Debug.Log($"Pooled Item activated at {location}");
        }
    }

    IEnumerator SpawnLoop()
    {
        Debug.Log("MasterClient SpawnLoop Activated");
        yield return new WaitForSeconds(5f); // 초기 플레이어 로딩 시간 대기

        while (true)
        {
            // 스폰 가능한 아이템이 있는지 로컬 풀에서 확인 (마스터 클라이언트 기준)
            if (HasItemInPool("Item"))
            {
                Vector3 randomPos = new Vector3(Random.Range(-40f, 40f), 1f, Random.Range(-40f, 40f));
                
                SpawnItem(randomPos);
            }

            yield return new WaitForSeconds(3f); // 스폰 간격
        }
    }

    bool HasItemInPool(string key)
    {
        return PoolManager.Instance != null
            && PoolManager.Instance.poolDictionary.ContainsKey(key)
            && PoolManager.Instance.poolDictionary[key].Count > 0;
    }
}