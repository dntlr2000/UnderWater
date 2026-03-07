using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using System;
using UnityEngine.UI;

public class LobbyManager : MonoBehaviourPunCallbacks
{
    public static LobbyManager Instance;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private AuthManager AuthMngr => AuthManager._instance;
    private RoomManager RoomMngr => RoomManager.Instance;
    private SaveSyncManager SaveSynMngr => SaveSyncManager.Instance;
    private OutgameCanvasManager CanvasMngr => OutgameCanvasManager.Instance;

    #region Photon Room List
    [HideInInspector] public List<RoomInfo> myList = new List<RoomInfo>();
    private int currentPage = 1, maxPage, multiple;
    #endregion

    #region Room List Logic

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        foreach (var room in roomList)
        {
            if (!room.RemovedFromList)
            {
                if (!myList.Contains(room)) myList.Add(room);
                else myList[myList.IndexOf(room)] = room;
            }
            else if (myList.Contains(room)) myList.Remove(room);
        }
        RefreshRoomListUI();
    }

    public void PagePrevious() => MyListClick(-2);
    public void PageNext() => MyListClick(-1);

    public void MyListClick(int num)
    {
        if (num == -2) --currentPage;
        else if (num == -1) ++currentPage;
        else PhotonNetwork.JoinRoom(myList[multiple + num].Name);

        RefreshRoomListUI();
    }

    private void RefreshRoomListUI()
    {
        OutgameCanvasManager canvas = OutgameCanvasManager.Instance;

        maxPage = (myList.Count % canvas.CellBtn.Length == 0)
            ? myList.Count / canvas.CellBtn.Length
            : myList.Count / canvas.CellBtn.Length + 1;

        // UI 상태는 CanvasManager의 Button을 직접 제어합니다.
        canvas.PreviousBtn.interactable = currentPage > 1;
        canvas.NextBtn.interactable = currentPage < maxPage;

        multiple = (currentPage - 1) * canvas.CellBtn.Length;
        for (int i = 0; i < canvas.CellBtn.Length; i++)
        {
            bool valid = multiple + i < myList.Count;
            canvas.CellBtn[i].interactable = valid;

            Text nameText = canvas.CellBtn[i].transform.GetChild(0).GetComponent<Text>();
            Text countText = canvas.CellBtn[i].transform.GetChild(1).GetComponent<Text>();

            nameText.text = valid ? myList[multiple + i].Name : "";
            countText.text = valid ? $"{myList[multiple + i].PlayerCount}/{myList[multiple + i].MaxPlayers}" : "";

            // 리스너가 SetupButtonEvents에서 등록되었으므로, 여기서 추가 등록은 하지 않습니다.
            // MyListClick(index)를 호출하는 리스너가 이미 존재합니다.
        }
        CanvasMngr.LobbyInfoText.text = $"총 방 개수: {myList.Count}개 ({currentPage}/{maxPage} 페이지)";
    }
    #endregion

    #region Create / Load / Join Game
    /// <summary>
    /// 새로운 방 이름으로 게임을 생성합니다. (새 게임 시작)
    /// </summary>
    /// <param name="roomName">사용자가 입력한 방 이름</param>
    public void NewGame(string roomName)
    {
        if (string.IsNullOrEmpty(AuthMngr.currentUserId))
        {
            CanvasMngr.SetStatus("로그인 정보가 유효하지 않습니다.");
            return;
        }

        // 1. 새로운 SaveData를 생성하고 로컬에 임시 저장
        SaveData newSave = SaveSynMngr.CreateNewSave(roomName, AuthMngr.currentUserId);

        // 2. 해당 SaveData로 방 생성 로직 실행
        CreateRoomWithSaveData(newSave);
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

        // 저장 목록 패널을 닫습니다.
        CanvasMngr.SaveListPanel.SetActive(false);
    }

    /// <summary>
    /// SaveData를 기반으로 포톤 방을 생성하는 공통 로직
    /// </summary>
    /// <param name="data">방 생성에 사용할 SaveData</param>
    /// <param name="isLoaded">저장된 게임에서 불러왔는지 여부</param>
    private void CreateRoomWithSaveData(SaveData data, bool isLoaded = false)
    {
        // 1. SaveSyncManager를 통해 현재 SaveData 설정 및 로컬 저장
        SaveSynMngr.SetCurrentSaveData(data, isLoaded);

        // 2. Photon Room 생성 옵션 설정
        string roomName = string.IsNullOrEmpty(data.roomName) ? "Room" + UnityEngine.Random.Range(0, 10000) : data.roomName;

        RoomOptions options = new RoomOptions
        {
            MaxPlayers = 2, // 필요에 따라 MaxPlayers 설정
            IsVisible = true,
            IsOpen = true,

            PublishUserId = true,

            CustomRoomProperties = new ExitGames.Client.Photon.Hashtable
            {
                { "SaveOwner", AuthMngr.currentUserId },
                { "CreatedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") },
                { "IsLoadedGame", isLoaded } // 로드된 게임인지 Custom Room Property에 명시
            }
        };

        // 3. 방 생성 요청
        PhotonNetwork.CreateRoom(roomName, options);
        CanvasMngr.SetStatus($"방 생성 요청: {roomName} (로드: {isLoaded})");
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
        CanvasMngr.SetStatus("참가 가능한 방이 없습니다. 새 방을 생성합니다.");

        // 참가 실패 시 자동으로 새 게임을 생성하는 옵션
        NewGame(null);
    }

    // Photon 콜백: 방 생성 실패 시
    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        CanvasMngr.SetStatus($"방 생성 실패: {message}");
    }
    #endregion
    public void OnClickRoomButton(int index)
    {
        // 현재 페이지와 버튼 인덱스를 계산하여 실제 방 리스트의 인덱스를 구함
        int actualIndex = ((currentPage - 1) * OutgameCanvasManager.Instance.CellBtn.Length) + index;

        Debug.Log($"[Lobby] 방 버튼 클릭: 버튼Index={index}, 실제Index={actualIndex}, 총 방 개수={myList.Count}");

        if (actualIndex >= 0 && actualIndex < myList.Count)
        {
            RoomInfo info = myList[actualIndex];
            Debug.Log($"[Lobby] 방 참가 요청: {info.Name}");
            PhotonNetwork.JoinRoom(info.Name);
        }
        else
        {
            Debug.LogWarning("[Lobby] 유효하지 않은 방 인덱스입니다.");
        }
    }
}