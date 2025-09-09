using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using System;
using Unity.VisualScripting;
using System.IO;

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

    [Header("PlayerSlotPanel")]
    public GameObject[] PlayerSlots;
    public Image[] PlayerJobIcons;
    public Text[] PlayerSlotNames;
    public Text[] PlayerSlotJobs;

    [Header("Save System UI")]
    public Button LoadGameBtn;
    public Text SelectedSaveText;
    public GameObject SaveListPanel;
    public Transform SaveListContent;
    public GameObject SaveBtnPrefab;

    private string selectedSaveRoomName = null;
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
    /*public void CreateRoom() => PhotonNetwork.CreateRoom(RoomInput.text == "" ? "Room" + Random.Range(0,100) : RoomInput.text, new RoomOptions { MaxPlayers = 2});*/

    public void NewGame()
    {
        SaveData data;

        if (!string.IsNullOrEmpty(selectedSaveRoomName))
        {
            // 선택된 저장 불러오기
            data = SaveSystem.LoadByRoomName(selectedSaveRoomName);
            if (data == null)
            {
                Debug.LogWarning("선택된 저장이 존재하지 않습니다. 새로 생성합니다.");
                data = CreateNewSave(RoomInput.text);
            }
        }
        else
        {
            data = CreateNewSave(RoomInput.text);
        }

        CreateRoom(data);
    }

    public void LoadGame()
    {
        if (string.IsNullOrEmpty(selectedFilePath)) return;

        SaveData loaded = SaveSystem.LoadByRoomName(selectedFilePath);
        if (loaded == null) return;

        CreateRoom(loaded);
    }

    private SaveData CreateNewSave(string roomName)
    {
        string finalRoomName = string.IsNullOrEmpty(roomName) ? "Room" + UnityEngine.Random.Range(0, 100) : roomName;
        SaveData newSave = new SaveData(finalRoomName);
        newSave.saveId = Guid.NewGuid().ToString();
        newSave.dayCount = 0;
        newSave.jobAssignments = new Dictionary<string, int>();
        SaveSystem.Save(newSave);
        return newSave;
    }

    public void RefreshSaveList()
    {
        foreach (Transform child in SaveListContent)
            Destroy(child.gameObject);

        List<string> saves = SaveSystem.GetRoomNames();
        foreach (var roomName in saves)
        {
            GameObject btnObj = Instantiate(SaveBtnPrefab, SaveListContent);
            btnObj.GetComponentInChildren<Text>().text = roomName;
            btnObj.GetComponent<Button>().onClick.AddListener(() => OnClick_SelectSave(roomName));
        }
    }

    private string selectedFilePath;

    public void OnClick_SelectSave(string roomName)
    {
        selectedSaveRoomName = roomName;
        SelectedSaveText.text = $"선택된 저장: {roomName}";
        SaveListPanel.SetActive(false);
    }

    public void ToggleSaveList()
    {
        SaveListPanel.SetActive(!SaveListPanel.activeSelf);
    }

    private void CreateRoom(SaveData data)
    {
        SaveSystem.Save(data);

        string roomName = string.IsNullOrEmpty(RoomInput.text)
            ? "Room" + UnityEngine.Random.Range(0, 100)
            : RoomInput.text;

        RoomOptions options = new RoomOptions { MaxPlayers = 2 };

        ExitGames.Client.Photon.Hashtable roomProps = new ExitGames.Client.Photon.Hashtable();
        roomProps["SaveData"] = JsonUtility.ToJson(data);
        options.CustomRoomProperties = roomProps;
        options.CustomRoomPropertiesForLobby = new string[] { "SaveData" };

        PhotonNetwork.CreateRoom(roomName, options);
    }

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

        RefreshPlayerSlots();
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogWarning("방 생성 실패: " + message);

        string roomName = string.IsNullOrEmpty(RoomInput.text)
            ? "Room" + UnityEngine.Random.Range(0, 100)
            : RoomInput.text;

        // 새 게임 기준으로 기본 SaveData 생성
        SaveData newSave = new SaveData(roomName);
        newSave.saveId = Guid.NewGuid().ToString();
        newSave.dayCount = 0;
        newSave.jobAssignments = new Dictionary<string, int>();

        // SaveData 저장 후 방 생성
        SaveSystem.Save(newSave);
        CreateRoom(newSave);
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.LogWarning("랜덤 방 입장 실패: " + message);

        string roomName = "Room" + UnityEngine.Random.Range(0, 100);

        // 새 게임 기준으로 기본 SaveData 생성
        SaveData newSave = new SaveData(roomName);
        newSave.saveId = Guid.NewGuid().ToString();
        newSave.dayCount = 0;
        newSave.jobAssignments = new Dictionary<string, int>();

        SaveSystem.Save(newSave);
        CreateRoom(newSave);
    }

    public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
    {
        RoomRenewal();
        ChatRPC("<color=yellow>" + newPlayer.NickName + "님이 참가하셨습니다.</color>");
        RefreshPlayerSlots();
    }

    public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
    {
        RoomRenewal();
        ChatRPC("<color=yellow>" + otherPlayer.NickName + "님이 퇴장하셨습니다.</color>");
        RefreshPlayerSlots();
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
            JobBtns[i].onClick.RemoveAllListeners();
            JobBtns[i].onClick.AddListener(() => SelectJob(index));
        }

        RefreshJobButtons();
    }

    public void SelectJob(int index)
    {
        var props = PhotonNetwork.LocalPlayer.CustomProperties ?? new ExitGames.Client.Photon.Hashtable();

        bool hasJob = props.ContainsKey("JobIndex") && props["JobIndex"] != null;

        // 이미 선택한 버튼 클릭 → 취소
        if (hasJob && (int)props["JobIndex"] == index)
        {
            props["JobIndex"] = null;
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);

            // UI 즉시 갱신
            JobBtns[index].GetComponent<Image>().color = Color.white;
            RefreshJobButtons();
            return;
        }

        // 다른 직업 이미 선택됨 → 선택 불가
        if (hasJob)
        {
            Debug.Log("이미 직업이 선택되어 있습니다.");
            return;
        }

        // 다른 플레이어가 선택했는지 확인
        foreach (var player in PhotonNetwork.PlayerList)
        {
            if (player != PhotonNetwork.LocalPlayer &&
                player.CustomProperties.TryGetValue("JobIndex", out object taken) &&
                taken != null &&
                (int)taken == index)
            {
                Debug.Log("다른 플레이어가 이미 선택한 직업입니다.");
                return;
            }
        }

        // 직업 선택
        props["JobIndex"] = index;
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);

        // 선택 버튼 즉시 초록색
        JobBtns[index].GetComponent<Image>().color = Color.green;

        RefreshJobButtons();
    }

    public void CancelJob()
    {
        if (!PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("JobIndex")) return;

        ExitGames.Client.Photon.Hashtable props = new();
        props["JobIndex"] = null;
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        RefreshJobButtons();
        RefreshPlayerSlots();
    }

    public override void OnPlayerPropertiesUpdate(Photon.Realtime.Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        if (JobBtns == null || PlayerSlots == null) return; // UI가 준비되지 않았으면 무시
        if (changedProps.ContainsKey("JobIndex"))
            RefreshJobButtons();

        RefreshPlayerSlots();
    }

    public void RefreshJobButtons()
    {
        if (JobBtns == null) return;

        for (int i = 0; i < JobBtns.Length; i++)
        {
            if (JobBtns[i] == null) continue;

            bool isTakenByOther = false;
            foreach (var player in PhotonNetwork.PlayerList)
            {
                if (player != PhotonNetwork.LocalPlayer &&
                    player.CustomProperties.TryGetValue("JobIndex", out object job) &&
                    job != null &&
                    (int)job == i)
                {
                    isTakenByOther = true;
                    break;
                }
            }

            bool isMyJob = PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("JobIndex", out object myJob) && myJob != null && (int)myJob == i;

            JobBtns[i].interactable = !isTakenByOther && (!PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("JobIndex") || isMyJob);

            var img = JobBtns[i].GetComponent<Image>();
            if (img != null)
                img.color = isMyJob ? Color.green : Color.white;
        }
    }

    private void RefreshPlayerSlots()
    {
        var players = PhotonNetwork.PlayerList;

        for (int i = 0; i < PlayerSlots.Length; i++)
        {
            if (i < players.Length)
            {
                PlayerSlots[i].SetActive(true);
                var p = players[i];

                // 이름
                PlayerSlotNames[i].text = p.NickName;

                // 직업
                int jobIndex = -1;
                if (p.CustomProperties.ContainsKey("JobIndex"))
                    jobIndex = (int)p.CustomProperties["JobIndex"];

                if (jobIndex >= 0 && jobIndex < jobDatas.Length)
                {
                    PlayerSlotJobs[i].text = jobDatas[jobIndex].jobName;
                    PlayerJobIcons[i].sprite = jobDatas[jobIndex].jobIcon;
                    PlayerJobIcons[i].enabled = true;
                }
                else
                {
                    PlayerSlotJobs[i].text = "직업 없음";
                    PlayerJobIcons[i].enabled = false;
                }
            }
            else
            {
                // 남는 슬롯은 비활성
                PlayerSlots[i].SetActive(true);  // 항상 4개 슬롯 활성
                PlayerSlotNames[i].text = "빈 슬롯";
                PlayerSlotJobs[i].text = "";
                PlayerJobIcons[i].enabled = false;
            }
        }
    }

    #endregion
}
