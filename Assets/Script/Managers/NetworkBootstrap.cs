using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System;
using UnityEngine.UI;
using System.Collections.Generic; // List<RoomInfo>를 위해 추가

public class NetworkBootstrap : MonoBehaviourPunCallbacks
{
    #region Singleton
    public static NetworkBootstrap Instance;

    // 다른 매니저 인스턴스
    public AuthManager AuthMngr;
    public LobbyManager LobbyMngr;
    public RoomManager RoomMngr;
    public SaveSyncManager SaveSyncMngr;

    // OutgameCanvasManager는 싱글톤 인스턴스로 접근한다고 가정합니다.
    private OutgameCanvasManager CanvasMngr => OutgameCanvasManager.Instance; // 편의를 위한 접근자 추가

    [Header("ETC")]
    public Text StatusText;
    public PhotonView PV;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }
        DontDestroyOnLoad(gameObject);
        // 컴포넌트 자동 초기화
        InitializeManagers();
        InitializePhoton();
    }
    private void InitializeManagers()
    {
        // 1. AuthManager 초기화
        AuthMngr.InitializeFirebase();

        // 2. SaveManager가 씬에 없다면 생성 (가장 중요)
        if (SaveManager.Instance == null)
        {
            GameObject go = new GameObject("SaveManager");
            go.AddComponent<SaveManager>();
            Debug.Log("[Bootstrap] SaveManager 생성됨.");
        }

        // 3. SaveSyncManager 초기화
        if (SaveSyncMngr != null)
        {
            SaveSyncMngr.InitializeSaveManager();
        }
    }
    #endregion

    #region Photon Initialization & Connection
    private void InitializePhoton()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
        Screen.SetResolution(960, 540, false);
    }

    public void Connect()
    {
        // 1. AuthManager에서 안전하게 ID 가져오기
        string myId = AuthMngr.currentUserId;

        // 2. ID가 없으면(로그인 안 함) 연결 막기 or 임시 ID
        if (string.IsNullOrEmpty(myId))
        {
            Debug.LogWarning("[Bootstrap] ID가 없어 임시 ID를 생성합니다.");
            myId = "Guest_" + Guid.NewGuid().ToString().Substring(0, 8);
        }

        // 3. [핵심] 포톤 인증 정보 설정
        PhotonNetwork.AuthValues = new AuthenticationValues(myId);

        // 4. 닉네임 설정
        if (!string.IsNullOrEmpty(AuthMngr.currentNickname))
        {
            PhotonNetwork.NickName = AuthMngr.currentNickname;
        }

        Debug.Log($"[Bootstrap] 포톤 연결 시도 (AuthID: {myId})");
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("[Bootstrap] 마스터 서버 연결됨.");
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("[Bootstrap] 로비 참가됨.");
        AuthMngr.GoToLobby(); // 인증 상태에 따라 로비 또는 닉네임 패널로 이동
        LobbyMngr.myList.Clear(); // 방 목록 초기화

        // 로비 UI 초기화 (필요하다면 CanvasMngr를 통해 호출)
        CanvasMngr?.ShowLobbyPanel(PhotonNetwork.NickName);
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        LobbyMngr.OnRoomListUpdate(roomList);
    }
    #endregion

    #region Update
    private void Update()
    {
        // 상태 텍스트 업데이트
        if (StatusText != null)
            StatusText.text = PhotonNetwork.NetworkClientState.ToString();

        // 로비 정보 텍스트 업데이트 (LobbyMngr 대신 OutgameCanvasManager 참조)
        if (CanvasMngr != null && CanvasMngr.LobbyInfoText != null)
        {
            CanvasMngr.LobbyInfoText.text =
                $"{PhotonNetwork.CountOfPlayers - PhotonNetwork.CountOfPlayersInRooms}로비 / {PhotonNetwork.CountOfPlayers}접속";
        }
    }
    #endregion
}