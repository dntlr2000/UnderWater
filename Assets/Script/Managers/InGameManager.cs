using UnityEngine;
using Photon.Pun;
using System.Linq;
using System.Collections;
using Photon.Realtime;

public class InGameManager : MonoBehaviourPunCallbacks
{
    [Header("테스트 설정 (인게임 바로 시작용)")]
    public bool isTestMode = false;

    [Header("직업별 프리팹")]
    public GameObject[] jobPrefabs;

    [Header("스폰 위치 설정")]
    [Tooltip("맵에 배치된 빈 게임 오브젝트(SpawnPoint)를 연결하세요.")]
    public Transform defaultSpawnPoint;

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

        if (SaveManager.Instance.isGameLoadedFromSave && PhotonNetwork.IsMasterClient)
        {
            RestoreWorldObjects();
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
        if (isTestMode || FindAnyObjectByType<AuthManager>() == null)
        {
            Debug.LogWarning("[테스트 모드] 포톤이 오프라인입니다! 가짜 방을 만들고 테스트 캐릭터를 소환합니다.");

            // 에디터에서 SampleScene만 바로 실행할 때 SaveData가 비어 발생하던
            // 상점/창고 계열 null 참조를 막기 위해 기본 SaveData를 먼저 보장합니다.
            EnsureTestSaveData();

            if (!PhotonNetwork.IsConnected)
            {
                PhotonNetwork.OfflineMode = true;
            }

            // 방에 안 들어가 있다면 가짜 방 만들기
            if (!PhotonNetwork.InRoom)
            {
                PhotonNetwork.JoinOrCreateRoom("OfflineRoom", new RoomOptions(), TypedLobby.Default);
            }

            Vector3 testSpawnPos = new Vector3(600f, 1000f, 600f);
            int testJobIndex = 0;

            if (jobPrefabs != null && jobPrefabs.Length > 0)
            {
                object[] testData = new object[] { testSpawnPos, testJobIndex };
                PhotonNetwork.Instantiate(jobPrefabs[testJobIndex].name, testSpawnPos, Quaternion.identity, 0, testData);
                Debug.Log($"[테스트 모드] {jobPrefabs[testJobIndex].name} 소환 완료!");
            }
            else
            {
                Debug.LogError("[테스트 모드 실패] 인스펙터에 jobPrefabs가 비어있습니다!");
            }

            return; // 🚨 여기서 함수 종료! 절대 아래 에러나는 코드로 내려가지 않습니다.
        }

        string myUserId = PhotonNetwork.LocalPlayer.UserId;

        if (AuthManager.Instance != null) myUserId = AuthManager.Instance.currentUserId;

        int finalJobIndex = -1;
        Vector3 spawnPos = defaultSpawnPoint != null ? defaultSpawnPoint.position : new Vector3(0, 7f, 0);

        // 1. SaveManager에서 저장된 데이터(직업, 위치) 조회
        if (SaveManager.Instance.IsDataReady)
        {
            // 직업 조회
            finalJobIndex = SaveManager.Instance.GetSavedJob(myUserId) ?? -1;

            // 위치 조회
            var myData = SaveManager.Instance.GetCurrentSave().players.FirstOrDefault(p => p.playerId == myUserId);
            if (myData != null && myData.position != null)
            {
                Vector3 loadedPos = myData.position.ToVector3();
                if (loadedPos != Vector3.zero)
                {
                    spawnPos = loadedPos;
                }
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

                if (SaveManager.Instance.isGameLoadedFromSave)
                {
                    // SaveManager에 내 데이터가 있는지 확인
                    var myData = SaveManager.Instance.GetCurrentSave().players.FirstOrDefault(p => p.playerId == myUserId);
                    if (myData != null)
                    {
                        // [수정] 인벤토리 로드와 상태 로드를 서로 독립적으로 실행하게 분리
                        if (myData.items != null)
                        {
                            Inventory myInventory = FindAnyObjectByType<Inventory>();
                            if (myInventory != null) myInventory.ApplyLoadedData(myData.items);
                        }
                        
                        if (myData.conditionData != null && myData.conditionData.isSaved)
                        {
                            // 방금 생성된 내 플레이어의 Condition 컴포넌트에 데이터 덮어씌우기
                            player.condition.ApplyLoadedData(myData.conditionData);
                        } 
                    }
                }

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

            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.RegisterLocalPlayer(player);

                if (SaveManager.Instance.isGameLoadedFromSave)
                {
                    Debug.Log("[InGameManager] 저장된 게임 감지 -> 퀘스트 데이터 로드 요청");
                    // 저장된 데이터를 QuestManager에 주입
                    SaveManager.Instance.LoadQuestDataToManager();
                }
                else
                {
                    Debug.Log("[InGameManager] 새 게임 감지 -> 기본 퀘스트 초기화");
                    // 새 게임용 기본 퀘스트 시작
                    QuestManager.Instance.InitStartingQuests();
                }
            }
        }
        else
        {
            Debug.LogError($"[InGameManager] 스폰 실패. 유효하지 않은 JobIndex: {finalJobIndex}");
        }
    }

    private void EnsureTestSaveData()
    {
        if (SaveManager.Instance == null) return;
        if (SaveManager.Instance.IsDataReady) return;

        string roomName = PhotonNetwork.CurrentRoom?.Name;
        if (string.IsNullOrEmpty(roomName))
        {
            roomName = "OfflineRoom";
        }

        SaveManager.Instance.SetCurrentSave(new SaveData(roomName), false);
        Debug.Log("[InGameManager] 테스트 모드용 기본 SaveData를 생성했습니다.");
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

    private void RestoreWorldObjects()
    {
        if (!PhotonNetwork.IsMasterClient) return; // 방장만 복원 권한을 가짐

        SaveData savedData = SaveManager.Instance.GetCurrentSave();
        if (savedData == null || savedData.worldEntities == null) return;

        Debug.Log($"[InGameManager] 월드 엔티티 {savedData.worldEntities.Count}개 세이브 데이터에서 탐지 완료");

        var sceneSavables = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).OfType<ISavable>().ToList();

        foreach (var entityData in savedData.worldEntities)
        {
            string prefabPath = entityData.prefabPath;

            //이미 씬에 존재하는 프리팹인 경우 -> 경로에 SceneObject_ 접두어를 붙여서 구분함
            if (prefabPath.StartsWith("SceneObject_"))
            {
                ISavable targetObj = sceneSavables.FirstOrDefault(s => s.PrefabPath == prefabPath);
                if (targetObj != null)
                {
                    MonoBehaviour mb = targetObj as MonoBehaviour;
                    if (mb != null)
                    {
                        // 위치 및 회전 강제 이동 (잠수함은 Rigidbody를 쓰므로 rb.position 우선)
                        // 회전은 오브젝트 별로 회전 방향이 다를 수 있어서 일단 보류
                        Rigidbody objRb = mb.GetComponent<Rigidbody>();
                        if (objRb != null)
                        {
                            objRb.position = entityData.position.ToVector3();
                            //objRb.rotation = Quaternion.Euler(0, entityData.yRotation, 0);
                        }
                        else
                        {
                            mb.transform.position = entityData.position.ToVector3();
                            //mb.transform.rotation = Quaternion.Euler(0, entityData.yRotation, 0);
                        }
                    }

                    // 내부 데이터 파싱 및 RPC 동기화
                    targetObj.RestoreSaveData(entityData.customDataJson);
                    Debug.Log($"[RestoreWorld] 씬 오브젝트 복원 완료: {prefabPath}");
                }
                else
                {
                    Debug.LogWarning($"[RestoreWorld] 씬에서 {prefabPath} 객체를 찾을 수 없습니다.");
                }

                continue; // 씬 오브젝트는 스폰(Instantiate)하지 않고 다음으로 넘어갑니다.
            }

            //씬에 배치되어 있지 않거나 소멸형 프리팹인 경우
            if (Resources.Load(prefabPath) == null)
            {
                Debug.LogWarning($"[RestoreWorld] 프리팹을 찾을 수 없습니다: {prefabPath}");
                continue;
            }

            // 1. 방장이 네트워크 스폰
            GameObject spawnedObj = PhotonNetwork.Instantiate(
                prefabPath,
                entityData.position.ToVector3(),
                Quaternion.Euler(0, entityData.yRotation, 0)
            );

            // 2. ISavable 인터페이스를 찾아 JSON 데이터 주입
            if (spawnedObj != null)
            {
                ISavable savable = spawnedObj.GetComponent<ISavable>();
                if (savable != null)
                {
                    // RestoreSaveData 안에서 객체 스스로 파싱 및 RPC 동기화를 진행합니다.
                    savable.RestoreSaveData(entityData.customDataJson);
                }
            }
        }

        Debug.Log($"[InGameManager] 월드 엔티티 {savedData.worldEntities.Count}개 복구 완료");
    }
}

