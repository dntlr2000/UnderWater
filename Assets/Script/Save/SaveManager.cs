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

    public float autoSaveInterval = 1f;
    private float timer;
    private AuthManager AuthMngr => AuthManager.Instance;

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

        timer += Time.deltaTime;
        if (timer >= autoSaveInterval)
        {
            timer = 0f;
            SaveGame();
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

    public void SetCurrentSave(SaveData save)
    {
        currentSave = save;
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
            existing.position = pd.position;
            existing.items = pd.items;
            if (pd.jobIndex != -1)
            {
                existing.jobIndex = pd.jobIndex;
            }
        }
        else
        {
            currentSave.players.Add(pd);
        }

        runtimePlayerCache[pd.playerId] = existing ?? pd;
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
        if (!PhotonNetwork.IsMasterClient) return;
        if (AuthMngr == null || string.IsNullOrEmpty(AuthMngr.currentUserId)) return;

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

        return currentSave;
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

        int? savedJob = GetSavedJob(playerId);
        if (savedJob.HasValue && savedJob.Value != -1 && jobIndex == 0)
        {
            jobIndex = savedJob.Value; // 기존 저장된 직업 유지
        }

        PlayerData pd = new PlayerData
        {
            playerId = playerId,
            position = new PlayerLocation(pos),
            jobIndex = jobIndex
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

        SetCurrentSave(loadedData);
        isGameLoadedFromSave = true;

        if (AuthMngr != null && !string.IsNullOrEmpty(AuthMngr.currentUserId))
        {
            int loadedJob = GetSavedJob(AuthMngr.currentUserId) ?? -1;
            if (RoomManager.Instance != null)
            {
                RoomManager.Instance.ApplyLoadedJobToPhoton(loadedJob);
            }
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