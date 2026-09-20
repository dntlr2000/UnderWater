using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using System;

public class LobbyManager : MonoBehaviourPunCallbacks
{
    public static LobbyManager Instance;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private AuthManager AuthMngr => AuthManager.Instance;
    private SaveSyncManager SaveSynMngr => SaveSyncManager.Instance;
    private OutgameCanvasManager CanvasMngr => OutgameCanvasManager.Instance;

    #region Photon Room List
    [HideInInspector] public List<RoomInfo> myList = new List<RoomInfo>();
    private readonly List<RoomInfo> _filtered = new List<RoomInfo>();
    private string _searchKeyword = string.Empty;
    private int _currentPage = 1;
    private int _selectedIndex = -1;
    private string _pendingRoomName = string.Empty;
    #endregion

    #region Room List Logic

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        foreach (var room in roomList)
        {
            int index = myList.FindIndex(r => r.Name == room.Name);

            if (room.RemovedFromList)
            {
                if (index >= 0) myList.RemoveAt(index);
            }
            else
            {
                if (index >= 0) myList[index] = room;
                else myList.Add(room);
            }
        }
        _selectedIndex = -1;
        RefreshRoomListUI();
    }

    public void ApplySearch(string keyword)
    {
        _searchKeyword = keyword != null ? keyword.Trim() : string.Empty;
        _currentPage = 1;
        _selectedIndex = -1;
        RefreshRoomListUI();
    }

    public void PagePrevious()
    {
        if (_currentPage <= 1) return;
        _currentPage--;
        _selectedIndex = -1;
        RefreshRoomListUI();
    }

    public void PageNext()
    {
        if (_currentPage >= GetMaxPage()) return;
        _currentPage++;
        _selectedIndex = -1;
        RefreshRoomListUI();
    }

    private int GetMaxPage()
    {
        if (_filtered.Count == 0) return 1;
        return Mathf.CeilToInt((float)_filtered.Count / RoomConfig.EntriesPerPage);
    }

    private void RebuildFiltered()
    {
        _filtered.Clear();

        for (int i = 0; i < myList.Count; i++)
        {
            RoomInfo info = myList[i];
            if (info == null) continue;

            if (string.IsNullOrEmpty(_searchKeyword) ||
                info.Name.IndexOf(_searchKeyword, System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                _filtered.Add(info);
            }
        }
    }

    public void RefreshRoomListUI()
    {
        OutgameCanvasManager canvas = CanvasMngr;
        if (canvas == null || canvas.RoomEntries == null) return;

        RebuildFiltered();

        int maxPage = GetMaxPage();
        _currentPage = Mathf.Clamp(_currentPage, 1, maxPage);
        int offset = (_currentPage - 1) * RoomConfig.EntriesPerPage;

        for (int i = 0; i < canvas.RoomEntries.Length; i++)
        {
            RoomEntry entry = canvas.RoomEntries[i];
            if (entry == null) continue;

            int dataIndex = offset + i;
            if (dataIndex >= _filtered.Count)
            {
                entry.SetEmpty();
                continue;
            }

            RoomInfo info = _filtered[dataIndex];
            entry.SetData(
                info.Name,
                GetCreatedAt(info),
                info.PlayerCount,
                info.MaxPlayers,
                GetHasPassword(info));

            entry.SetSelected(dataIndex == _selectedIndex);
        }

        if (canvas.RoomPrevBtn != null) canvas.RoomPrevBtn.interactable = _currentPage > 1;
        if (canvas.RoomNextBtn != null) canvas.RoomNextBtn.interactable = _currentPage < maxPage;
        if (canvas.RoomJoinBtn != null) canvas.RoomJoinBtn.interactable = _selectedIndex >= 0;

        canvas.UpdatePageIndicator(_currentPage, maxPage);
    }

    private static string GetCreatedAt(RoomInfo info)
    {
        if (info.CustomProperties != null &&
            info.CustomProperties.TryGetValue(RoomKeys.CreatedAt, out object value) &&
            value is string text)
        {
            return text;
        }
        return "-";
    }

    private static bool GetHasPassword(RoomInfo info)
    {
        if (info.CustomProperties != null &&
            info.CustomProperties.TryGetValue(RoomKeys.HasPassword, out object value) &&
            value is bool flag)
        {
            return flag;
        }
        return false;
    }
    #endregion

    #region Create / Load / Join Game
    /// <summary>
    /// 새로운 방 이름으로 게임을 생성합니다. (새 게임 시작)
    /// </summary>
    /// <param name="roomName">사용자가 입력한 방 이름</param>
    public void NewGame(string roomName, string password = null)
    {
        if (string.IsNullOrEmpty(AuthMngr.currentUserId))
        {
            CanvasMngr.SetStatus("로그인 정보가 유효하지 않습니다.");
            return;
        }

        // 1. 새로운 SaveData를 생성하고 로컬에 임시 저장
        SaveData newSave = SaveSynMngr.CreateNewSave(roomName, AuthMngr.currentUserId);

        // 2. 해당 SaveData로 방 생성 로직 실행
        CreateRoomWithSaveData(newSave, false, password);
    }

    /// <summary>
    /// 선택된 저장 파일 이름으로 게임을 불러와 방을 생성합니다.
    /// </summary>
    /// <param name="saveRoomName">저장된 SaveData의 이름</param>
    public void LoadGame(string saveRoomName)
    {
        if (string.IsNullOrEmpty(AuthMngr.currentUserId))
        {
            CanvasMngr.SetStatus("로그인 정보가 유효하지 않습니다.");
            return;
        }

        // 1. 저장된 SaveData를 불러옵니다.
        SaveData loadedSave = SaveSystem.Load(AuthMngr.currentUserId, saveRoomName);

        if (loadedSave == null)
        {
            CanvasMngr.SetStatus($"저장 파일 '{saveRoomName}'을(를) 찾을 수 없습니다.");
            return;
        }

        // 2. 불러온 SaveData로 방 생성 로직 실행
        CreateRoomWithSaveData(loadedSave, isLoaded: true);
    }

    /// <summary>
    /// SaveData를 기반으로 포톤 방을 생성하는 공통 로직
    /// </summary>
    /// <param name="data">방 생성에 사용할 SaveData</param>
    /// <param name="isLoaded">저장된 게임에서 불러왔는지 여부</param>
    private void CreateRoomWithSaveData(SaveData data, bool isLoaded = false, string password = null)
    {
        // 1. SaveSyncManager를 통해 현재 SaveData 설정 및 로컬 저장
        SaveSynMngr.SetCurrentSaveData(data, isLoaded);

        // 2. Photon Room 생성 옵션 설정
        string roomName = string.IsNullOrEmpty(data.roomName) ? "Room" + UnityEngine.Random.Range(0, 10000) : data.roomName;

        bool hasPassword = !string.IsNullOrEmpty(password);
        string hash = hasPassword ? RoomPassword.Hash(password) : string.Empty;

        RoomOptions options = new RoomOptions
        {
            MaxPlayers = RoomConfig.MaxPlayers, // 필요에 따라 MaxPlayers 설정
            IsVisible = true,
            IsOpen = true,
            PublishUserId = true,

            CustomRoomProperties = new ExitGames.Client.Photon.Hashtable
            {
                { RoomKeys.SaveOwner, AuthMngr.currentUserId },
                { RoomKeys.CreatedAt, DateTime.Now.ToString(RoomConfig.DateFormat) },
                { RoomKeys.IsLoadedGame, isLoaded },
                // 입장자는 선택 이력 RPC 대신 생성 시점부터 제공되는 최신 저장을 읽습니다.
                { "SaveData", JsonUtility.ToJson(data) },
                { RoomKeys.HasPassword, hasPassword  },
                { RoomKeys.PasswordHash, hash }
            },

            CustomRoomPropertiesForLobby = new string[]
            {
                RoomKeys.CreatedAt,
                RoomKeys.HasPassword,
                RoomKeys.PasswordHash
            }
        };

        // 3. 방 생성 요청
        PhotonNetwork.CreateRoom(roomName, options);
        CanvasMngr.SetStatus($"방 생성 요청: {roomName}");
    }

    /// <summary>
    /// 방 목록이 아닌, 활성화된 방 중 랜덤으로 입장합니다.
    /// </summary>
    public void TryJoinRandomRoom()
    {
        PhotonNetwork.JoinRandomRoom();
        CanvasMngr.SetStatus("랜덤 방 참가 요청...");
    }

    // Photon 콜백: 랜덤 방 참가 실패 시
    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        CanvasMngr.SetStatus("참가 가능한 방이 없습니다.");
    }

    // Photon 콜백: 방 생성 실패 시
    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        CanvasMngr.SetStatus($"방 생성 실패: {message}");
    }
    #endregion

    #region Save List
    private readonly List<SaveData> _allSaves = new List<SaveData>();
    private readonly List<SaveData> _filteredSaves = new List<SaveData>();
    private string _saveSearchKeyword = string.Empty;
    private int _saveCurrentPage = 1;
    private int _saveSelectedIndex = -1;

    public void OpenSaveList()
    {
        LoadAllSaves();
        _saveSearchKeyword = string.Empty;
        _saveCurrentPage = 1;
        _saveSelectedIndex = -1;
        RefreshSaveListUI();
    }

    private void LoadAllSaves()
    {
        _allSaves.Clear();
        if (AuthMngr == null || string.IsNullOrEmpty(AuthMngr.currentUserId)) return;

        var names = SaveSystem.GetRoomNames(AuthMngr.currentUserId);
        foreach (var name in names)
        {
            SaveData data = SaveSystem.Load(AuthMngr.currentUserId, name);
            if (data != null) _allSaves.Add(data);
        }
    }

    public void ApplySaveSearch(string keyword)
    {
        _saveSearchKeyword = keyword != null ? keyword.Trim() : string.Empty;
        _saveCurrentPage = 1;
        _saveSelectedIndex = -1;
        RefreshSaveListUI();
    }

    public void SavePagePrevious()
    {
        if (_saveCurrentPage <= 1) return;
        _saveCurrentPage--;
        _saveSelectedIndex = -1;
        RefreshSaveListUI();
    }

    public void SavePageNext()
    {
        if (_saveCurrentPage >= GetSaveMaxPage()) return;
        _saveCurrentPage++;
        _saveSelectedIndex = -1;
        RefreshSaveListUI();
    }

    private int GetSaveMaxPage()
    {
        if (_filteredSaves.Count == 0) return 1;
        return Mathf.CeilToInt((float)_filteredSaves.Count / RoomConfig.EntriesPerPage);
    }

    private void RebuildFilteredSaves()
    {
        _filteredSaves.Clear();

        foreach (var data in _allSaves)
        {
            if (string.IsNullOrEmpty(_saveSearchKeyword) ||
                data.roomName.IndexOf(_saveSearchKeyword, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                _filteredSaves.Add(data);
            }
        }
    }

    private void RefreshSaveListUI()
    {
        OutgameCanvasManager canvas = CanvasMngr;
        if (canvas == null || canvas.SaveEntries == null) return;

        RebuildFilteredSaves();

        int maxPage = GetSaveMaxPage();
        _saveCurrentPage = Mathf.Clamp(_saveCurrentPage, 1, maxPage);
        int offset = (_saveCurrentPage - 1) * RoomConfig.EntriesPerPage;

        for (int i = 0; i < canvas.SaveEntries.Length; i++)
        {
            RoomEntry entry = canvas.SaveEntries[i];
            if (entry == null) continue;

            int dataIndex = offset + i;
            if (dataIndex >= _filteredSaves.Count)
            {
                entry.SetEmpty();
                continue;
            }

            SaveData data = _filteredSaves[dataIndex];
            entry.SetData(
                data.roomName,
                FormatSaveDate(data.createdDate),
                data.players?.Count ?? 0,
                RoomConfig.MaxPlayers,
                false);

            entry.SetSelected(dataIndex == _saveSelectedIndex);
        }

        if (canvas.SavePrevBtn != null) canvas.SavePrevBtn.interactable = _saveCurrentPage > 1;
        if (canvas.SaveNextBtn != null) canvas.SaveNextBtn.interactable = _saveCurrentPage < maxPage;
        if (canvas.SaveLoadBtn != null) canvas.SaveLoadBtn.interactable = _saveSelectedIndex >= 0;

        canvas.UpdateSavePageIndicator(_saveCurrentPage, maxPage);
    }

    public void OnClickSaveButton(int slotIndex)
    {
        int dataIndex = (_saveCurrentPage - 1) * RoomConfig.EntriesPerPage + slotIndex;

        if (dataIndex < 0 || dataIndex >= _filteredSaves.Count)
        {
            _saveSelectedIndex = -1;
            RefreshSaveListUI();
            return;
        }

        _saveSelectedIndex = dataIndex;
        RefreshSaveListUI();
    }

    public void LoadSelectedSave()
    {
        if (_saveSelectedIndex < 0 || _saveSelectedIndex >= _filteredSaves.Count)
        {
            CanvasMngr?.SetStatus("불러올 저장 파일을 선택해주세요.");
            return;
        }

        string roomName = _filteredSaves[_saveSelectedIndex].roomName;
        LoadGame(roomName);
    }

    private static string FormatSaveDate(string rawCreatedDate)
    {
        if (DateTime.TryParseExact(
                rawCreatedDate, "yyyy-MM-dd_HH-mm-ss",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None,
                out DateTime parsed))
        {
            return parsed.ToString(RoomConfig.DateFormat);
        }
        return rawCreatedDate;
    }
    #endregion

    public void OnClickRoomButton(int slotIndex)
    {
        int dataIndex = (_currentPage - 1) * RoomConfig.EntriesPerPage + slotIndex;

        if (dataIndex < 0 || dataIndex >= _filtered.Count)
        {
            _selectedIndex = -1;
            RefreshRoomListUI();
            return;
        }

        _selectedIndex = dataIndex;
        RefreshRoomListUI();
    }

    public void JoinSelectedRoom()
    {
        if (_selectedIndex < 0 || _selectedIndex >= _filtered.Count)
        {
            CanvasMngr?.SetStatus("참가할 방을 선택해주세요.");
            return;
        }

        RoomInfo info = _filtered[_selectedIndex];

        if (info.PlayerCount >= info.MaxPlayers)
        {
            CanvasMngr?.SetStatus("정원이 가득 찬 방입니다.");
            return;
        }

        if (GetHasPassword(info))
        {
            _pendingRoomName = info.Name;
            CanvasMngr?.ShowPasswordPopup(info.Name);
            return;
        }

        PhotonNetwork.JoinRoom(info.Name);
    }
    public void ConfirmPassword(string input)
    {
        if (string.IsNullOrEmpty(_pendingRoomName))
        {
            CanvasMngr?.HidePasswordPopup();
            return;
        }

        int index = _filtered.FindIndex(r => r.Name == _pendingRoomName);
        if (index < 0)
        {
            CanvasMngr?.SetPasswordStatus("방을 찾을 수 없습니다.", true);
            return;
        }

        if (!RoomPassword.Matches(input, GetPasswordHash(_filtered[index])))
        {
            CanvasMngr?.SetPasswordStatus("비밀번호가 일치하지 않습니다.", true);
            return;
        }

        string target = _pendingRoomName;
        _pendingRoomName = string.Empty;

        CanvasMngr?.HidePasswordPopup();
        PhotonNetwork.JoinRoom(target);
    }

    public void CancelPassword()
    {
        _pendingRoomName = string.Empty;
        CanvasMngr?.HidePasswordPopup();
    }

    private static string GetPasswordHash(RoomInfo info)
    {
        if (info.CustomProperties != null &&
            info.CustomProperties.TryGetValue(RoomKeys.PasswordHash, out object value) &&
            value is string text)
        {
            return text;
        }
        return string.Empty;
    }
}