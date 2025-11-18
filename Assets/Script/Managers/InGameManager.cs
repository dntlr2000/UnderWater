using UnityEngine;
using Photon.Pun;
using System.Linq;
using System.Collections;
using Photon.Realtime;

public class InGameManager : MonoBehaviourPunCallbacks
{
    [Header("직업별 프리팹")]
    public GameObject[] jobPrefabs;

    IEnumerator Start()
    {
        // 1. SaveManager 인스턴스가 생성될 때까지 대기 (안전 장치)
        yield return new WaitUntil(() => SaveManager.Instance != null);

        // 2. 데이터 동기화 대기 (씬 이동 직후 데이터가 도착하지 않았을 수 있음)
        // 최대 3초간 데이터를 기다립니다.
        float timeout = 3f;
        while (!SaveManager.Instance.IsDataReady && timeout > 0)
        {
            timeout -= Time.deltaTime;
            yield return null;
        }

        // 3. 데이터 수신 결과 확인
        if (!SaveManager.Instance.IsDataReady)
        {
            Debug.LogWarning("[InGameManager] 저장 데이터를 받지 못했습니다. (Timeout or New Game). 기본값으로 진행합니다.");
        }
        else
        {
            Debug.Log("[InGameManager] 저장 데이터 준비 완료.");
        }

        // 4. 플레이어 스폰 로직 실행
        SpawnPlayer();
    }

    void SpawnPlayer()
    {
        string myUserId = PhotonNetwork.LocalPlayer.UserId;
        int finalJobIndex = -1;
        Vector3 spawnPos = new Vector3(0, 7f, 0);

        // 1. SaveManager에서 저장된 데이터(직업, 위치) 조회
        if (SaveManager.Instance.IsDataReady)
        {
            // 직업 조회
            finalJobIndex = SaveManager.Instance.GetSavedJob(myUserId) ?? -1;

            // 위치 조회
            var myData = SaveManager.Instance.GetCurrentSave().players.FirstOrDefault(p => p.playerId == myUserId);
            if (myData != null && myData.position != null)
            {
                spawnPos = myData.position.ToVector3();
            }
        }

        // 2. SaveManager에 없으면 CustomProperties 확인 (보완책 - 로비에서 설정한 값)
        if (finalJobIndex < 0 && PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("JobIndex", out object jobObj))
        {
            finalJobIndex = (int)jobObj;
        }

        // 3. 유효한 직업 인덱스가 확인되면 스폰 진행
        if (finalJobIndex >= 0 && finalJobIndex < jobPrefabs.Length)
        {
            object[] initData = new object[] { spawnPos, finalJobIndex };

            GameObject playerObj = PhotonNetwork.Instantiate(
                jobPrefabs[finalJobIndex].name,
                spawnPos,
                Quaternion.identity,
                0,
                initData
            );

            // 주의: 실제 플레이어 컨트롤러 스크립트 이름이 Player인지 PlayerController인지 확인하세요.
            Player player = playerObj.GetComponent<Player>();

            if (player != null)
            {
                player.SetJob(finalJobIndex);
                Debug.Log($"[InGameManager] {PhotonNetwork.LocalPlayer.NickName} 스폰 완료 - 위치:{spawnPos}, 직업Index:{finalJobIndex}");

                // 로컬 플레이어 정보 즉시 업데이트 (내 정보 저장)
                PlayerData pd = new PlayerData
                {
                    playerId = myUserId,
                    playerName = PhotonNetwork.NickName,
                    jobIndex = finalJobIndex,
                    position = new PlayerLocation(spawnPos) // 끊겼던 부분 수정 완료
                };

                // 내 로컬 캐시 업데이트
                SaveManager.Instance.UpdatePlayerCache(pd);

                // 방장이 아니라면, 방장에게 내 정보를 보내서 저장 데이터에 반영 요청
                if (!PhotonNetwork.IsMasterClient)
                {
                    photonView.RPC("RPC_SendPlayerInfoToMaster", RpcTarget.MasterClient,
                        myUserId,
                        PhotonNetwork.LocalPlayer.NickName,
                        finalJobIndex,
                        spawnPos);
                }
            }
        }
        else
        {
            Debug.LogError($"[InGameManager] 스폰 실패. 유효하지 않은 JobIndex: {finalJobIndex}");
        }
    }

    // 참가자 플레이어가 방장에게 자기 정보를 전송하여 저장 데이터에 등록 요청
    [PunRPC]
    void RPC_SendPlayerInfoToMaster(string playerId, string playerName, int jobIndex, Vector3 position)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        Debug.Log($"[InGameManager] 비마스터로부터 정보 수신: {playerId} (JobIndex:{jobIndex})");

        PlayerData newData = new PlayerData
        {
            playerId = playerId,
            playerName = playerName,
            jobIndex = jobIndex,
            position = new PlayerLocation(position)
        };

        // 마스터의 SaveManager에 반영 (이후 자동 저장 시 포함됨)
        SaveManager.Instance.UpdatePlayerCache(newData);
    }
}