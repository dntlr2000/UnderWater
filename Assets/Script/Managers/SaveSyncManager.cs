using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using System;
using System.Collections.Generic;

public class SaveSyncManager : MonoBehaviourPunCallbacks
{
    public static SaveSyncManager Instance; // 추가

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        DontDestroyOnLoad(gameObject);
    }

    private AuthManager AuthMngr => AuthManager.Instance;
    private RoomManager RoomMngr => RoomManager.Instance;
    private LobbyManager LobbyMngr => LobbyManager.Instance;

    [HideInInspector] public string selectedSaveRoomName = null;

    #region SaveManager Initialization
    public void InitializeSaveManager()
    {
        if (SaveManager.Instance != null)
        {
            // 이전에 구독했을 수도 있으니 한 번 제거 후 추가
            SaveManager.OnSaveDataChanged -= OnSaveDataChangedHandler;
            SaveManager.OnSaveDataChanged += OnSaveDataChangedHandler;
        }
    }

    private void OnDestroy()
    {
        // 이벤트 해제는 NetworkBootstrap이 아닌 SaveSynManager에서 처리하는 것이 더 안전합니다.
        if (SaveManager.Instance != null)
            SaveManager.OnSaveDataChanged -= OnSaveDataChangedHandler;
    }
    #endregion

    #region SaveData Management for Lobby/Room Creation

    /// <summary>
    /// 새로운 SaveData 객체를 생성하고 로컬에 저장합니다. (LobbyManager.NewGame에서 사용)
    /// </summary>
    /// <param name="roomName">사용자 입력 방 이름</param>
    /// <param name="userId">현재 사용자 ID</param>
    /// <returns>새로 생성된 SaveData</returns>
    // 신규 저장의 퀘스트 버전과 소유자를 명시합니다.
    public SaveData CreateNewSave(string roomName, string userId)
    {
        string finalRoomName = string.IsNullOrEmpty(roomName) ? "Room" + UnityEngine.Random.Range(0, 10000) : roomName;

        // 동일한 방 이름이 있으면 이름을 변경하는 로직이 필요할 수 있으나, 여기서는 단순화합니다.

        SaveData newSave = new SaveData(finalRoomName) // SaveData 생성자에 roomName을 전달한다고 가정
        {
            saveId = Guid.NewGuid().ToString(),
            questSaveVersion = 1,
            saveOwnerId = userId,
            dayCount = 0,
            players = new List<PlayerData>
            {
                new PlayerData
                {
                    playerId = userId,
                    playerName = AuthMngr.currentNickname,
                    jobType = "",
                    // 기타 초기화 데이터
                }
            }
        };

        // 로컬에 저장
        SaveSystem.Save(newSave, userId);

        OutgameCanvasManager.Instance.SetStatus($"새 게임 저장소 생성: {finalRoomName}");
        return newSave;
    }

    /// <summary>
    /// RoomManager의 방 생성 전에 SaveManager에 현재 SaveData를 설정합니다.
    /// </summary>
    /// <param name="data">설정할 SaveData</param>
    /// <param name="isLoaded">저장된 게임에서 불러왔는지 여부</param>
    // 새 게임 여부를 최초 저장 주입부터 일관되게 전달합니다.
    public void SetCurrentSaveData(SaveData data, bool isLoaded)
    {
        if (SaveManager.Instance == null) return;

        // SaveManager에 SaveData를 설정하고 로드 상태를 플래그합니다.
        SaveManager.Instance.SetCurrentSave(data, isLoaded);

        // RoomManager의 상태 갱신 (선택적)
        RoomMngr.isLoadedFromSave = isLoaded;
    }
    #endregion

    #region Save List UI
    public void ToggleSaveList()
    {
        OutgameCanvasManager canvas = OutgameCanvasManager.Instance;
        bool newState = !canvas.SaveListPanel.activeSelf;
        canvas.SaveListPanel.SetActive(newState);

        if (newState)
        {
            RefreshSaveList();
            canvas.SaveSelectText.text = "선택된 저장: 없음";
            selectedSaveRoomName = null;
        }
    }

    public void RefreshSaveList()
    {
        if (SaveManager.Instance == null || string.IsNullOrEmpty(AuthMngr.currentUserId)) return;

        // SaveListContent를 OutgameCanvasManager에서 가져옵니다.
        Transform content = OutgameCanvasManager.Instance.SaveListContent;
        foreach (Transform child in content) Destroy(child.gameObject);

        var saves = SaveSystem.GetRoomNames(AuthMngr.currentUserId);
        foreach (var roomName in saves)
        {
            // SaveBtnPrefab을 OutgameCanvasManager에서 가져옵니다.
            GameObject btnObj = Instantiate(OutgameCanvasManager.Instance.SaveBtnPrefab, content);
            btnObj.GetComponentInChildren<Text>().text = roomName;

            string capturedRoomName = roomName;
            btnObj.GetComponent<Button>().onClick.AddListener(() => OnClick_LoadGame(capturedRoomName));
        }
    }

    /// <summary>
    /// 저장 목록 버튼 클릭 시, 해당 저장 파일로 게임 로드를 시도합니다.
    /// </summary>
    public void OnClick_LoadGame(string roomName)
    {
        OutgameCanvasManager canvas = OutgameCanvasManager.Instance;

        selectedSaveRoomName = roomName;
        canvas.SaveSelectText.text = $"선택된 저장: {roomName} (클릭 시 로드)";

        // 중요: 저장 목록 선택 시 바로 방을 생성하도록 로직 변경
        LobbyMngr.LoadGame(roomName);
    }
    #endregion

    #region SaveData Synchronization

    // 입장 또는 방장 교체 시 현재 저장의 최신 스냅샷을 방 속성에 게시합니다.
    public void PublishCurrentSave()
    {
        if (SaveManager.Instance?.GetCurrentSave() is SaveData data)
            OnSaveDataChangedHandler(JsonUtility.ToJson(data));
    }

    // 변경 이력을 RPC에 누적하지 않고 신규 참가자도 읽을 수 있는 최신 저장 하나만 유지합니다.
    public void OnSaveDataChangedHandler(string saveJson)
    {
        if (!PhotonNetwork.InRoom || !PhotonNetwork.IsMasterClient || string.IsNullOrEmpty(saveJson)) return;
        if (QuestManager.Instance != null && QuestManager.Instance.IsInitialized) return;
        var room = PhotonNetwork.CurrentRoom;
        if (room.CustomProperties["SaveData"] as string == saveJson) return;
        room.SetCustomProperties(new ExitGames.Client.Photon.Hashtable { { "SaveData", saveJson } });
    }

    // 구형 버퍼 RPC가 남아 있어도 그 과거 내용 대신 방의 최신 저장만 적용합니다.
    [PunRPC]
    public void RPC_BroadcastSaveData(string saveJson)
    {
        if (!PhotonNetwork.InRoom || PhotonNetwork.IsMasterClient) return;
        if (QuestManager.Instance != null && QuestManager.Instance.IsInitialized) return;
        if (PhotonNetwork.CurrentRoom.CustomProperties["SaveData"] is string latestJson)
        {
            SaveManager.Instance?.HandleBroadcastedSaveData(latestJson);
            RoomMngr?.ApplySavedJobs();
        }
    }

    #endregion
}