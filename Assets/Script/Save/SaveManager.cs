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

    public void UpdateLocalPlayerJob(string userId, string nickname, int newJobIndex)
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
            ProcessJobUpdate(userId, nickname, newJobIndex);
        }
        // 2. 게스트면 -> 방장에게 RPC 요청
        else
        {
            if (photonView != null)
            {
                photonView.RPC(nameof(RPC_RequestJobChange), RpcTarget.MasterClient, userId, nickname, newJobIndex);
            }
            else
            {
                Debug.LogError("[SaveManager] PhotonView가 컴포넌트에 없습니다! RPC 실패.");
            }
        }
    }

    public void UpdatePlayerCache(PlayerData pd)
    {
        if (pd == null || string.IsNullOrEmpty(pd.playerId)) return;

        if (currentSave == null)
            currentSave = new SaveData(PhotonNetwork.CurrentRoom?.Name ?? "Room");

        var existing = currentSave.players.FirstOrDefault(p => p.playerId == pd.playerId);

        if (existing != null)
        {
            if (pd.items != null) existing.items = pd.items;
            else Debug.LogWarning("pd.items가 존재하지 않습니다!");
            if (pd.conditionData != null) existing.conditionData = pd.conditionData;
            else Debug.LogWarning("pd.conditionData가 존재하지 않습니다!");
            if (pd.jobIndex != -1)
            {
                existing.position = pd.position;
                //existing.items = pd.items;
                if (pd.jobIndex != -1) existing.jobIndex = pd.jobIndex;

                if (pd.completedQuestIds != null && pd.completedQuestIds.Count > 0)
                    existing.completedQuestIds = pd.completedQuestIds;
                if (pd.activeQuests != null && pd.activeQuests.Count > 0)
                    existing.activeQuests = pd.activeQuests;
            }
            else
            {
                currentSave.players.Add(pd);
                existing = pd; // 신규 추가된 객체 참조
            }
            runtimePlayerCache[pd.playerId] = existing;
        }
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

    public void SaveGame()
    {
        if (SceneManager.GetActiveScene().name != inGameSceneName)
        {
            Debug.Log("[SaveManager] 대기실이므로 디스크 저장을 생략하여 원본 훼손을 방지합니다.");
            return;
        }

        if (!PhotonNetwork.IsMasterClient) return;
        if (AuthMngr == null || string.IsNullOrEmpty(AuthMngr.currentUserId)) return;

        if (QuestManager.Instance != null)
        {
            var questData = QuestManager.Instance.GetQuestSaveData();

            PlayerData myData = runtimePlayerCache.ContainsKey(AuthMngr.currentUserId)
                ? runtimePlayerCache[AuthMngr.currentUserId]
                : new PlayerData { playerId = AuthMngr.currentUserId };

            myData.completedQuestIds = questData.completed;
            myData.activeQuests = questData.active;

            UpdatePlayerCache(myData);
        }

        SaveData data = CollectSaveData();
        if (data != null)
        {
            SaveSystem.Save(data, AuthMngr.currentUserId);
        }
    }

    private SaveData CollectSaveData()
    {
        if (currentSave == null) return null;

        currentSave.players = runtimePlayerCache.Values.ToList();
        currentSave.jobAssignments = currentSave.players.ToDictionary(p => p.playerId, p => p.jobIndex);
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

    public int? GetSavedJob(string userId)
    {
        if (string.IsNullOrEmpty(userId) || currentSave == null) return -1;
        var pd = currentSave.players.FirstOrDefault(p => p.playerId == userId);
        return pd?.jobIndex ?? -1;
    }

    public bool CanChangeJob(string userId)
    {
        if (isGameLoadedFromSave) return false;
        var pd = currentSave?.players.FirstOrDefault(p => p.playerId == userId);
        return pd == null || pd.jobIndex < 0;
    }

    #endregion

    #region Photon Event Callback

    public void OnEvent(EventData photonEvent)
    {
        if (photonEvent.Code != 101) return;

        object[] data = (object[])photonEvent.CustomData;
        string playerId = (string)data[0];
        Vector3 pos = (Vector3)data[1];
        int jobIndex = (int)data[2];
        string inventoryJson = data.Length > 3 ? (string)data[3] : "";
        string conditionJson = data.Length > 4 ? (string)data[4] : "";

        int? savedJob = GetSavedJob(playerId);
        if (savedJob.HasValue && savedJob.Value != -1 && jobIndex == 0)
        {
            jobIndex = savedJob.Value; // 기존 저장된 직업 유지
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
                jobIndex = jobIndex,
                items = receivedInventory, // [추가] PlayerData에 인벤토리 할당!
                conditionData = receivedCondition
            };

        if (PhotonNetwork.IsMasterClient)
        {
            UpdatePlayerCache(pd);
        }
    }

    #endregion

    public void HandleBroadcastedSaveData(string json)
    {
        SaveData loadedData = JsonUtility.FromJson<SaveData>(json);

        SetCurrentSave(loadedData,  true);
        isGameLoadedFromSave = true;

        if (AuthMngr != null && !string.IsNullOrEmpty(AuthMngr.currentUserId))
        {
            string myId = AuthMngr.currentUserId;

            int loadedJob = GetSavedJob(AuthMngr.currentUserId) ?? -1;
            if (RoomManager.Instance != null)
            {
                RoomManager.Instance.ApplyLoadedJobToPhoton(loadedJob);
            }
        }
    }

    public void LoadQuestDataToManager()
    {
        if (QuestManager.Instance == null || RoomManager.Instance == null) return;
        if (AuthMngr == null || string.IsNullOrEmpty(AuthMngr.currentUserId)) return;

        string myId = AuthMngr.currentUserId;

        // 내 데이터 찾기
        PlayerData myData = null;
        if (runtimePlayerCache.ContainsKey(myId))
        {
            myData = runtimePlayerCache[myId];
        }
        // 만약 캐시에 없으면 원본 save에서 찾기 시도
        else if (currentSave != null)
        {
            myData = currentSave.players.FirstOrDefault(p => p.playerId == myId);
        }

        if (myData != null)
        {
            // 직업 정보 (퀘스트 해금 조건 체크용)
            JobData myJobData = null;
            int jobIndex = GetSavedJob(myId) ?? -1;
            if (jobIndex >= 0 && jobIndex < RoomManager.Instance.jobDatas.Length)
            {
                myJobData = RoomManager.Instance.jobDatas[jobIndex];
            }

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
    private void RPC_RequestJobChange(string userId, string nickname, int newJobIndex)
    {
        Debug.Log($"[SaveManager] RPC 수신: {nickname}님이 직업 {newJobIndex} 선택");
        ProcessJobUpdate(userId, nickname, newJobIndex);
    }

    // 내부 처리 함수
    private void ProcessJobUpdate(string userId, string nickname, int newJobIndex)
    {
        if (currentSave == null)
        {
            currentSave = new SaveData(PhotonNetwork.CurrentRoom?.Name ?? "Room");
            currentSave.players = new List<PlayerData>();
            currentSave.jobAssignments = new Dictionary<string, int>();
        }

        if (currentSave.players == null) currentSave.players = new List<PlayerData>();
        if (currentSave.jobAssignments == null) currentSave.jobAssignments = new Dictionary<string, int>();

        PlayerData pd = currentSave.players.FirstOrDefault(p => p.playerId == userId);
        if (pd == null)
        {
            pd = new PlayerData { playerId = userId, playerName = nickname, position = new PlayerLocation(Vector3.zero) };
            currentSave.players.Add(pd);
        }
        pd.jobIndex = newJobIndex;
        runtimePlayerCache[userId] = pd;

        if (newJobIndex >= 0)
        {
            if (currentSave.jobAssignments.ContainsKey(userId))
                currentSave.jobAssignments[userId] = newJobIndex;
            else
                currentSave.jobAssignments.Add(userId, newJobIndex);
        }
        else if (currentSave.jobAssignments.ContainsKey(userId))
        {
            currentSave.jobAssignments.Remove(userId);
        }

        // 방장이 변경 사항을 모두에게 알림
        BroadcastSaveData();
    }
}