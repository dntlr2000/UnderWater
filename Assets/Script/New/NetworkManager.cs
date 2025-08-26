using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    [Header("DisconnectPanel")]
    public InputField NickNameInput;

    [Header("LobbyPanel")]
    public GameObject LobbyPanel;
    public InputField RoomInput;
    public Text WelcomeText;
    public Text LobbyInfoText;
    public Button[] CellBtn;
    public Button PreviousBtn;
    public Button NextBtn;

    [Header("RoomPanel")]
    public GameObject RoomPanel;
    public Text ListText;
    public Text RoomInfoText;
    public Text[] ChatText;
    public InputField ChatInput;
    public Button StartBtn;

    [Header("ETC")]
    public Text StatusText;
    public PhotonView PV;

    [Header("JobSelectPanel")]
    public GameObject JobSelectPanel;
    public Button[] JobBtns;
    public JobData[] jobDatas;

    List<RoomInfo> myList = new List<RoomInfo>();
    int currentPage = 1, maxPage, multiple;

    #region 방 리스트 갱신
    public void MyListClick(int num)
    {
        if (num == -2) --currentPage;
        else if (num == -1) ++currentPage;
        else PhotonNetwork.JoinRoom(myList[multiple + num].Name);
        MyListRenewal();
    }

    void MyListRenewal()
    {
        //최대 페이지
        maxPage = (myList.Count % CellBtn.Length == 0) ? myList.Count / CellBtn.Length : myList.Count / CellBtn.Length + 1;

        //이전, 다음 버튼
        PreviousBtn.interactable = (currentPage <= 1) ? false : true;
        NextBtn.interactable = (currentPage >= maxPage) ? false : true;

        //페이지에 맞는 리스트 대입
        multiple = (currentPage - 1) * CellBtn.Length;
        for (int i = 0; i < CellBtn.Length; i++)
        {
            CellBtn[i].interactable = (multiple + i < myList.Count) ? true : false;
            CellBtn[i].transform.GetChild(0).GetComponent<Text>().text = (multiple + i < myList.Count) ? myList[multiple + i].Name : "";
            CellBtn[i].transform.GetChild(1).GetComponent<Text>().text = (multiple + i < myList.Count) ? myList[multiple + i].PlayerCount + "/" + myList[multiple + i].MaxPlayers : "";
        }
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        int roomCount = roomList.Count;
        for (int i = 0; i < roomCount; i++)
        {
            if (!roomList[i].RemovedFromList)
            {
                if (!myList.Contains(roomList[i])) myList.Add(roomList[i]);
                else myList[myList.IndexOf(roomList[i])] = roomList[i];
            }
            else if (myList.IndexOf(roomList[i]) != -1) myList.RemoveAt(myList.IndexOf(roomList[i]));
        }
        MyListRenewal();
    }
    #endregion 방 리스트 갱신

    #region 서버연결
    void Awake()
    {
        PhotonNetwork.AutomaticallySyncScene = true; //씬 자동 동기화
        Screen.SetResolution(960, 540, false); //창크기
    }
    void Update()
    {
        StatusText.text = PhotonNetwork.NetworkClientState.ToString();
        LobbyInfoText.text = (PhotonNetwork.CountOfPlayers - PhotonNetwork.CountOfPlayersInRooms) + "로비 / " + PhotonNetwork.CountOfPlayers + "접속";
    }

    public void Connect() => PhotonNetwork.ConnectUsingSettings();

    public override void OnConnectedToMaster() => PhotonNetwork.JoinLobby();
    public override void OnJoinedLobby()
    {
        LobbyPanel.SetActive(true);
        RoomPanel.SetActive(false);
        PhotonNetwork.LocalPlayer.NickName = NickNameInput.text;
        WelcomeText.text = PhotonNetwork.LocalPlayer.NickName + "님 환영합니다.";
        myList.Clear();
    }

    public void Disconnect() => PhotonNetwork.Disconnect();

    public override void OnDisconnected(DisconnectCause cause)
    {
        if (LobbyPanel != null) LobbyPanel.SetActive(false);
        if (RoomPanel != null) RoomPanel.SetActive(false);
    }
    #endregion 서버연결

    #region 방
    public void CreateRoom() => PhotonNetwork.CreateRoom(RoomInput.text == "" ? "Room" + Random.Range(0,100) : RoomInput.text, new RoomOptions { MaxPlayers = 2});

    public void JoinRandomRoom() => PhotonNetwork.JoinRandomRoom();

    public void LeaveRoom() => PhotonNetwork.LeaveRoom();

    public override void OnJoinedRoom()
    {
        RoomPanel.SetActive(true);
        RoomRenewal();
        ChatInput.text = "";
        for (int i = 0; i < ChatText.Length; i++) ChatText[i].text = "";

        StartBtn.interactable = PhotonNetwork.IsMasterClient;
        Debug.Log("Room joined: " + PhotonNetwork.CurrentRoom.Name);
        SetupJobButtons();
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        RoomInput.text = "";
        CreateRoom();
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        RoomInput.text = "";
        CreateRoom();
    }

    public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
    {
        RoomRenewal();
        ChatRPC("<color=yellow>" + newPlayer.NickName + "님이 참가하셨습니다.</color>");
    }

    public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
    {
        RoomRenewal();
        ChatRPC("<color=yellow>" + otherPlayer.NickName + "님이 퇴장하셨습니다.</color>");
    }

    public override void OnMasterClientSwitched(Photon.Realtime.Player newMasterClient)
    {
        StartBtn.interactable = PhotonNetwork.IsMasterClient;
    }

    void RoomRenewal()
    {
        ListText.text = "";
        for (int i = 0; i < PhotonNetwork.PlayerList.Length; i++)
            ListText.text += PhotonNetwork.PlayerList[i].NickName + ((i + 1 == PhotonNetwork.PlayerList.Length) ? "" : ", ");
        RoomInfoText.text = PhotonNetwork.CurrentRoom.Name + " / " + PhotonNetwork.CurrentRoom.PlayerCount + "명 / " + PhotonNetwork.CurrentRoom.MaxPlayers + "최대";
    }

    public void StartGame()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            foreach (var player in PhotonNetwork.PlayerList)
            {
                if (!player.CustomProperties.ContainsKey("JobIndex"))
                {
                    Debug.LogWarning("모든 플레이어가 직업을 선택해야 합니다.");
                    return;
                }
            }

            PhotonNetwork.LoadLevel("SampleScene");
        }
    }
    #endregion 방

    #region 채팅
    public void Send()
    {
        string msg = PhotonNetwork.NickName + " : " + ChatInput.text;
        PV.RPC("ChatRPC", RpcTarget.All, PhotonNetwork.NickName + " : " + ChatInput.text);
        ChatInput.text = "";
    }

    [PunRPC] //RPC는 플레이어가 속해있는 방 모든 인원에게 전달됨
    void ChatRPC(string msg)
    {
        bool isInput = false;
        for(int i = 0; i<ChatText.Length; i++)
            if(ChatText[i].text == "")
            {
                isInput = true;
                ChatText[i].text = msg;
                break;
            }
        if(!isInput) //꽉 차면 한칸씩 위로 올림
        {
            for (int i = 1; i < ChatText.Length; i++) ChatText[i - 1].text = ChatText[i].text;
            ChatText[ChatText.Length - 1].text = msg;
        }
    }
    #endregion 채팅

    #region 직업
    private void SetupJobButtons()
    {
        JobSelectPanel.SetActive(true);

        for (int i = 0; i < JobBtns.Length; i++)
        {
            int index = i;
            JobBtns[i].onClick.RemoveAllListeners(); // 중복 방지
            JobBtns[i].onClick.AddListener(() => SelectJob(index));
        }

        RefreshJobButtons();
    }

    public void SelectJob(int index)
    {
        var props = PhotonNetwork.LocalPlayer.CustomProperties ?? new ExitGames.Client.Photon.Hashtable();

        // 이미 선택한 버튼 클릭 → 취소
        if (props.TryGetValue("JobIndex", out object currentJob) && (int)currentJob == index)
        {
            props.Remove("JobIndex");
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);
            Debug.Log("직업 선택 취소: " + jobDatas[index].jobName);

            RefreshJobButtons();
            return;
        }

        // 다른 플레이어가 선택했는지 확인
        foreach (var player in PhotonNetwork.PlayerList)
        {
            if (player != PhotonNetwork.LocalPlayer &&
                player.CustomProperties.TryGetValue("JobIndex", out object taken) &&
                (int)taken == index)
            {
                Debug.Log("이미 다른 플레이어가 선택한 직업: " + jobDatas[index].jobName);
                return;
            }
        }

        // 선택 적용
        props["JobIndex"] = index;
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        Debug.Log("선택한 직업: " + jobDatas[index].jobName);
    }

    public void RefreshJobButtons()
    {
        bool hasSelectedJob = PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("JobIndex", out object myJobIndex);

        for (int i = 0; i < JobBtns.Length; i++)
        {
            bool takenByOther = false;

            // 다른 플레이어가 선택했는지 확인
            foreach (var player in PhotonNetwork.PlayerList)
            {
                if (player != PhotonNetwork.LocalPlayer &&
                    player.CustomProperties.TryGetValue("JobIndex", out object job) &&
                    (int)job == i)
                {
                    takenByOther = true;
                    break;
                }
            }

            // 버튼 활성화 조건
            if (hasSelectedJob)
            {
                // 내가 선택한 버튼만 활성, 나머지는 비활성
                JobBtns[i].interactable = ((int)myJobIndex == i);
            }
            else
            {
                // 아직 선택 안했으면 다른 사람이 선택한 버튼만 비활성
                JobBtns[i].interactable = !takenByOther;
            }
        }
    }
    #endregion
}
