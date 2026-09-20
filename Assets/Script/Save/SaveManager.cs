using Photon.Pun;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Firebase.Database;
using Firebase;
using ExitGames.Client.Photon;
using Photon.Realtime;
using UnityEngine.SceneManagement;

public class SaveManager : MonoBehaviourPun, IOnEventCallback
{
    private static SaveManager _instance;
    public static SaveManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<SaveManager>();
            }
            if (_instance == null)
            {
                Debug.LogWarning("[SaveManager] 인스턴스가 없어 자동으로 생성합니다.");
                GameObject go = new GameObject("SaveManager");
                _instance = go.AddComponent<SaveManager>();

                // 생성 시 PhotonView가 없으면 RPC가 불가능하므로 경고
                if (go.GetComponent<PhotonView>() == null)
                    Debug.LogError("[SaveManager] 자동 생성된 객체에 PhotonView가 없습니다! 에디터에서 확인하세요.");
            }
            return _instance;
        }
    }

    private string GetMyCurrentId()
    {
        // 1. 포톤에 등록된 ID 확인
        if (PhotonNetwork.AuthValues != null && !string.IsNullOrEmpty(PhotonNetwork.AuthValues.UserId))
        {
            return PhotonNetwork.AuthValues.UserId;
        }

        // 2. 포톤에 없으면 AuthManager에게 확인
        if (AuthManager.Instance != null && !string.IsNullOrEmpty(AuthManager.Instance.currentUserId))
        {
            return AuthManager.Instance.currentUserId;
        }

        return null;
    }

    public DatabaseReference dbRef;
    private SaveData currentSave;

    public bool IsDataReady => currentSave != null;
    [HideInInspector] public bool isGameLoadedFromSave = false;

    public static event Action<string> OnSaveDataChanged;
    private Dictionary<string, PlayerData> runtimePlayerCache = new();
    private Dictionary<string, InventoryData> runtimeBoxCache = new();

    public float autoSaveInterval = 5f;
    private float timer;
    private AuthManager AuthMngr => AuthManager.Instance;

    public string inGameSceneName = "SampleScene";

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        if (FirebaseApp.DefaultInstance != null)
        {
            dbRef = FirebaseDatabase.GetInstance(FirebaseApp.DefaultInstance,
                "https://theoverflown-5908d-default-rtdb.firebaseio.com/").RootReference;
        }
    }

    private void Update()
    {
        if (!PhotonNetwork.IsMasterClient || !PhotonNetwork.InRoom) return;

        if (SceneManager.GetActiveScene().name == inGameSceneName)
        {
            timer += Time.deltaTime;
            if (timer >= autoSaveInterval)
            {
                timer = 0f;
                if (FindAnyObjectByType<AuthManager>() == null)
                {
                    return; // 에러를 뿜기 전에 조용히 돌아갑니다.
                }

                SaveGame();
            }
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        PhotonNetwork.AddCallbackTarget(this);
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        PhotonNetwork.RemoveCallbackTarget(this);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (_instance == null)
        {
            _instance = this;
        }
    }

    #region SaveData Get/Set Logic

    public void SetCurrentSave(SaveData save, bool isLoaded = true)
    {
        currentSave = save;
        Debug.Log($"_isLoaded 값 : {isLoaded}");
        isGameLoadedFromSave = isLoaded;

        RefreshRuntimeCache();
        Debug.Log($"[SaveManager] SaveData 설정 완료. (Room: {save?.roomName ?? "NULL"})");
    }

    public SaveData GetCurrentSave() => currentSave;

    private void RefreshRuntimeCache()
    {
        runtimePlayerCache.Clear();
        if (currentSave?.players != null)
        {
            foreach (var pd in currentSave.players)
            {
                if (pd != null && !string.IsNullOrEmpty(pd.playerId))
                    runtimePlayerCache[pd.playerId] = pd;
            }
        }

        runtimeBoxCache.Clear();
        if (currentSave?.storageBoxes != null)
        {
            foreach (var box in currentSave.storageBoxes)
            {
                if (box != null && !string.IsNullOrEmpty(box.boxId))
                    runtimeBoxCache[box.boxId] = box.items;
            }
        }
    }

    public void UpdateLocalPlayerJob(string userId, string nickname, string newJobType)
    {
        if (string.IsNullOrEmpty(userId)) userId = GetMyCurrentId();

        if (string.IsNullOrEmpty(userId))
        {
            Debug.LogError($"[SaveManager] ID가 없어 직업 변경 실패. (Nick: {nickname})");
            return;
        }

        // 1. 방장이면 -> 즉시 처리
        if (PhotonNetwork.IsMasterClient)
        {
            ProcessJobUpdate(userId, nickname, newJobType);
        }
        // 2. 게스트면 -> 방장에게 RPC 요청
        else
        {
            if (photonView != null)
            {
                photonView.RPC(nameof(RPC_RequestJobChange), RpcTarget.MasterClient, userId, nickname, newJobType);
            }
            else
            {
                Debug.LogError("[SaveManager] PhotonView가 컴포넌트에 없습니다! RPC 실패.");
            }
        }
    }

    // 기본 플레이어 상태를 병합하고 오래된 인벤토리 패킷과 퀘스트 덮어쓰기를 차단합니다.
    public void UpdatePlayerCache(PlayerData pd)
    {
        if (pd == null || string.IsNullOrEmpty(pd.playerId)) return;
        currentSave ??= new SaveData(PhotonNetwork.CurrentRoom?.Name ?? "Room");
        currentSave.players ??= new List<PlayerData>();
        var existing = currentSave.players.FirstOrDefault(p => p.playerId == pd.playerId);
        if (existing == null)
        {
            existing = new PlayerData { playerId = pd.playerId };
            currentSave.players.Add(existing);
        }
        if (pd.items != null && (pd.inventoryActor != existing.inventoryActor || pd.inventorySequence >= existing.inventorySequence))
        {
            existing.items = pd.items;
            existing.inventoryActor = pd.inventoryActor;
            existing.inventorySequence = pd.inventorySequence;
        }
        if (pd.position != null) existing.position = pd.position;
        if (pd.conditionData != null) existing.conditionData = pd.conditionData;
        if (!string.IsNullOrEmpty(pd.playerName)) existing.playerName = pd.playerName;
        if (!string.IsNullOrEmpty(pd.jobType)) existing.jobType = pd.jobType;
        runtimePlayerCache[pd.playerId] = existing;
    }

    // 개인 퀘스트는 전용 호출로 저장하여 빈 완료/활성 목록도 정확히 반영합니다.
    public void UpdatePlayerQuestCache(PlayerQuestState state)
    {
        if (state == null || currentSave == null || state.saveId != currentSave.saveId || string.IsNullOrEmpty(state.playerId)) return;
        UpdatePlayerCache(new PlayerData { playerId = state.playerId });
        var data = runtimePlayerCache[state.playerId];
        data.completedQuestIds = state.completed ?? new List<string>();
        data.activeQuests = state.active ?? new List<QuestProgressData>();
        data.rewardClaims = state.rewardClaims ?? new List<QuestRewardClaim>();
    }

    // 공유 메인 상태를 개인 데이터와 분리하여 저장 캐시에 보관합니다.
    public void StoreSharedQuestState(SharedQuestState state)
    {
        if (currentSave == null || state == null || !state.initialized) return;
        currentSave.worldProgress ??= new WorldProgress();
        currentSave.worldProgress.mainQuests = JsonUtility.FromJson<SharedQuestState>(JsonUtility.ToJson(state));
        currentSave.questSaveVersion = 1;
    }

    private void BroadcastSaveData()
    {
        try
        {
            string json = JsonUtility.ToJson(currentSave);
            OnSaveDataChanged?.Invoke(json);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SaveManager] 데이터 직렬화 오류: {ex.Message}");
        }
    }

    #endregion

    #region Game Save Logic

    // 방장만 최신 공용/개인 진행과 월드 상태를 함께 저장합니다.
    public void SaveGame()
    {
        if (SceneManager.GetActiveScene().name != inGameSceneName)
        {
            Debug.Log("[SaveManager] 대기실이므로 디스크 저장을 생략하여 원본 훼손을 방지합니다.");
            return;
        }

        if (!PhotonNetwork.IsMasterClient) return;
        var saveAuth = FindAnyObjectByType<AuthManager>();
        if (saveAuth == null || string.IsNullOrEmpty(saveAuth.currentUserId)) return;
        // 방장 교체 중에는 참가자의 최신 인벤토리를 받은 뒤 저장합니다.
        if (QuestManager.Instance != null && QuestManager.Instance.GetComponent<QuestNetworkBridge>().IsRecoveringPlayerStates) return;

        if (QuestManager.Instance != null && QuestManager.Instance.IsInitialized)
        {
            StoreSharedQuestState(QuestManager.Instance.CaptureSharedState());
            var quests = QuestManager.Instance.GetQuestSaveData();
            UpdatePlayerQuestCache(new PlayerQuestState { saveId = currentSave.saveId,
                playerId = QuestNetworkBridge.LocalPlayerId, completed = quests.completed, active = quests.active,
                rewardClaims = QuestManager.Instance.GetJobRewardClaims() });
        }

        SaveData data = CollectSaveData();
        if (data != null)
        {
            SaveSystem.Save(data, AuthMngr.currentUserId);
        }
    }

    // 활성 필드 아이템을 포함한 월드 오브젝트와 최신 플레이어 상태를 저장합니다.
    private SaveData CollectSaveData()
    {
        if (currentSave == null) return null;

        currentSave.players = runtimePlayerCache.Values.ToList();
        currentSave.jobAssignments = currentSave.players.ToDictionary(p => p.playerId, p => p.jobType);
        currentSave.createdDate = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");

        currentSave.storageBoxes = new List<BoxSaveData>();
        //currentSave.fieldItems = new List<FieldItemSaveData>();

        foreach (var kvp in runtimeBoxCache)
        {
            currentSave.storageBoxes.Add(new BoxSaveData { boxId = kvp.Key, items = kvp.Value });
        }

        currentSave.worldEntities = new List<EntitySaveData>();
        if (SceneManager.GetActiveScene().name == inGameSceneName)
        {
            currentSave.worldEntities = new List<EntitySaveData>();

            var allSavables = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).OfType<ISavable>();

            foreach (var savable in allSavables)
            {
                MonoBehaviour mb = savable as MonoBehaviour;
                if (mb != null && mb.gameObject.scene.isLoaded)
                {
                    currentSave.worldEntities.Add(new EntitySaveData
                    {
                        prefabPath = savable.PrefabPath,
                        position = new PlayerLocation(mb.transform.position),
                        yRotation = mb.transform.eulerAngles.y,
                        customDataJson = savable.GetSaveDataJson()
                    });
                }
            }
            Debug.Log($"[SaveManager] 필드의 아이템 탐지 완료 완료 : {allSavables.Count()}개");
        }
        

        return currentSave;
    }

    public InventoryData GetBoxData(string boxId)
    {
        if (runtimeBoxCache.ContainsKey(boxId))
            return runtimeBoxCache[boxId];
        return null;
    }

    public void UpdateBoxCache(string boxId, InventoryData data)
    {
        if (string.IsNullOrEmpty(boxId) || data == null) return;
        runtimeBoxCache[boxId] = data; // 캐시에 덮어쓰기 (나중에 자동 저장 시 파일에 기록됨)
    }

    #endregion

    #region Utility & Accessors

    public string GetSavedJobType(string userId)
    {
        if (string.IsNullOrEmpty(userId) || currentSave == null) return "";
        var pd = currentSave.players.FirstOrDefault(p => p.playerId == userId);
        return pd?.jobType ?? "";
    }

    public bool CanChangeJob(string userId)
    {
        if (isGameLoadedFromSave) return false;
        var pd = currentSave?.players.FirstOrDefault(p => p.playerId == userId);
        return pd == null || string.IsNullOrEmpty(pd.jobType);
    }

    #endregion

    #region Photon Event Callback

    // 기존 플레이어 패킷을 수신하며 새 인벤토리 순번도 함께 병합합니다.
    public void OnEvent(EventData photonEvent)
    {
        if (photonEvent.Code != 101) return;

        object[] data = (object[])photonEvent.CustomData;
        string playerId = (string)data[0];
        Vector3 pos = (Vector3)data[1];
        string jobType = (string)data[2];
        string inventoryJson = data.Length > 3 ? (string)data[3] : "";
        string conditionJson = data.Length > 4 ? (string)data[4] : "";

        string savedJobType = GetSavedJobType(playerId);
        if (!string.IsNullOrEmpty(savedJobType) && string.IsNullOrEmpty(jobType))
        {
            jobType = savedJobType;
        }

        InventoryData receivedInventory = null;
        if (!string.IsNullOrEmpty(inventoryJson))
        {
            receivedInventory = JsonUtility.FromJson<InventoryData>(inventoryJson);
        }

        ConditionData receivedCondition = null;
        if (!string.IsNullOrEmpty(conditionJson))
        {
            receivedCondition = JsonUtility.FromJson<ConditionData>(conditionJson);
            //Debug.Log($"[데이터 해독] {playerId}님의 체력 해독 결과: {receivedCondition.health}");
        }
        else Debug.LogWarning("recivedCondition를 받지 못했습니다!");

        PlayerData pd = new PlayerData
        {
            playerId = playerId,
            position = new PlayerLocation(pos),
            jobType = jobType,
            items = receivedInventory,
            inventoryActor = photonEvent.Sender,
            inventorySequence = data.Length > 6 ? Convert.ToInt64(data[6]) : 0,
            conditionData = receivedCondition
        };

        if (PhotonNetwork.IsMasterClient)
        {
            UpdatePlayerCache(pd);
        }
    }

    #endregion

    // 진행 전 저장 방송만 수락하며 새 게임/불러오기 구분을 유지합니다.
    public void HandleBroadcastedSaveData(string json)
    {
        SaveData loadedData = JsonUtility.FromJson<SaveData>(json);

        // 진행 중에는 로비에서 늦게 도착한 저장 방송으로 최신 캐시를 교체하지 않습니다.
        if (QuestManager.Instance != null && QuestManager.Instance.IsInitialized) return;
        bool loadedSession = isGameLoadedFromSave;
        if (PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("IsLoadedGame", out var loadedFlag) && loadedFlag is bool value)
            loadedSession = value;
        SetCurrentSave(loadedData, loadedSession);

        if (AuthMngr != null && !string.IsNullOrEmpty(AuthMngr.currentUserId))
        {
            string myId = AuthMngr.currentUserId;

            string loadedJobType = GetSavedJobType(AuthMngr.currentUserId);
            if (!string.IsNullOrEmpty(loadedJobType) && RoomManager.Instance != null)
            {
                RoomManager.Instance.ApplyLoadedJobToPhoton(loadedJobType);
            }
        }
    }

    public void LoadQuestDataToManager()
    {
        if (QuestManager.Instance == null || RoomManager.Instance == null) return;
        if (AuthMngr == null || string.IsNullOrEmpty(AuthMngr.currentUserId)) return;

        string myId = AuthMngr.currentUserId;
        PlayerData myData = runtimePlayerCache.ContainsKey(myId)
        ? runtimePlayerCache[myId]
        : currentSave?.players.FirstOrDefault(p => p.playerId == myId);

        if (myData != null)
        {
            string savedJobType = GetSavedJobType(myId);
            JobData myJobData = RoomManager.Instance.jobDatas
                .FirstOrDefault(j => j.jobType.ToString() == savedJobType);

            Debug.Log($"[SaveManager] 저장된 퀘스트 데이터 복구 시작 (ID: {myId})");
            QuestManager.Instance.LoadQuestSaveData(myData.completedQuestIds, myData.activeQuests, myJobData);
        }
        else
        {
            Debug.LogWarning("[SaveManager] 복구할 퀘스트 플레이어 데이터를 찾지 못했습니다.");
        }
    }

    // RPC 함수 (방장만 수신)
    [PunRPC]
    private void RPC_RequestJobChange(string userId, string nickname, string newJobType)
    {
        Debug.Log($"[SaveManager] RPC 수신: {nickname}님이 직업 {newJobType} 선택");
        ProcessJobUpdate(userId, nickname, newJobType);
    }

    // 내부 처리 함수
    private void ProcessJobUpdate(string userId, string nickname, string newJobType)
    {
        if (currentSave == null)
        {
            currentSave = new SaveData(PhotonNetwork.CurrentRoom?.Name ?? "Room");
            currentSave.players = new List<PlayerData>();
            currentSave.jobAssignments = new Dictionary<string, string>();
        }

        if (currentSave.players == null) currentSave.players = new List<PlayerData>();
        if (currentSave.jobAssignments == null) currentSave.jobAssignments = new Dictionary<string, string>();

        PlayerData pd = currentSave.players.FirstOrDefault(p => p.playerId == userId);
        if (pd == null)
        {
            pd = new PlayerData { playerId = userId, playerName = nickname, position = new PlayerLocation(Vector3.zero) };
            currentSave.players.Add(pd);
        }
        pd.jobType = newJobType;
        runtimePlayerCache[userId] = pd;

        if (!string.IsNullOrEmpty(newJobType))
        {
            if (currentSave.jobAssignments.ContainsKey(userId))
                currentSave.jobAssignments[userId] = newJobType;
            else
                currentSave.jobAssignments.Add(userId, newJobType);
        }
        else if (currentSave.jobAssignments.ContainsKey(userId))
        {
            currentSave.jobAssignments.Remove(userId);
        }

        // 방장이 변경 사항을 모두에게 알림
        BroadcastSaveData();
    }
}
