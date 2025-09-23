using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using System;
using Firebase.Auth;
using Firebase.Extensions;
using UnityEngine.SceneManagement;
using Firebase.Database;
using Firebase;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    #region UI
    [Header("Login/Register UI")]
    public GameObject LoginPanel;
    public InputField EmailInput;
    public InputField PasswordInput;
    public Text LoginStatusText;
    public Button LoginBtn;
    public Button RegisterBtn;

    public GameObject RegisterPanel;
    public InputField RegisterEmailInput;
    public InputField RegisterPasswordInput;
    public InputField RegisterPasswordConfirmInput;
    public Text RegisterStatusText;
    public Button RegisterConfirmBtn;
    public Button BackToLoginBtn;

    [Header("Nickname UI")]
    public GameObject NicknamePanel;
    public InputField NicknameInput;
    public Button NicknameConfirmBtn;
    public Text NicknameStatusText;

    [Header("Lobby UI")]
    public GameObject LobbyPanel;
    public InputField RoomInput;
    public Text WelcomeText;
    public Text LobbyInfoText;
    public Button[] CellBtn;
    public Button PreviousBtn;
    public Button NextBtn;
    public Button SettingsBtn;

    [Header("Settings UI")]
    public GameObject SettingsPanel;
    public Button LogoutBtn;
    public Button ExitGameBtn;
    public Button ProfileBtn;

    [Header("Profile UI")]
    public GameObject ProfilePanel;
    public InputField ProfileNicknameInput;
    public Button ProfileSaveBtn;
    public Text ProfileStatusText;

    [Header("Room UI")]
    public GameObject RoomPanel;
    public Text ListText;
    public Text RoomInfoText;
    public Text[] ChatText;
    public InputField ChatInput;
    public Button StartBtn;

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

    [Header("ETC")]
    public Text StatusText;
    public PhotonView PV;
    #endregion
    public static NetworkManager Instance;

    private FirebaseAuth auth;
    private DatabaseReference dbRef;
    public string currentUserId;
    public string currentNickname;
    private string selectedSaveRoomName = null;
    private List<RoomInfo> myList = new List<RoomInfo>();
    private int currentPage = 1, maxPage, multiple;

    #region 서버연결
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        auth = FirebaseAuth.DefaultInstance;
        dbRef = FirebaseDatabase.GetInstance(FirebaseApp.DefaultInstance, "https://theoverflown-5908d-default-rtdb.firebaseio.com/").RootReference;
        PhotonNetwork.AutomaticallySyncScene = true;
        Screen.SetResolution(960, 540, false);

        // Button Listeners
        LoginBtn.onClick.AddListener(Login);
        RegisterBtn.onClick.AddListener(OpenRegisterPanel);
        RegisterConfirmBtn.onClick.AddListener(Register);
        BackToLoginBtn.onClick.AddListener(OpenLoginPanel);
        NicknameConfirmBtn.onClick.AddListener(ConfirmNickname);
        SettingsBtn.onClick.AddListener(() => SettingsPanel.SetActive(!SettingsPanel.activeSelf));
        LogoutBtn.onClick.AddListener(Logout);
        ExitGameBtn.onClick.AddListener(() => Application.Quit());
        ProfileBtn.onClick.AddListener(() =>
        {
            ProfilePanel.SetActive(true);
            ProfileNicknameInput.text = currentNickname;
        });
        ProfileSaveBtn.onClick.AddListener(SaveProfileNickname);

        if (SaveManager.Instance == null)
        {
            GameObject go = new GameObject("SaveManager");
            go.AddComponent<SaveManager>();
        }
    }

    private void Update()
    {
        StatusText.text = PhotonNetwork.NetworkClientState.ToString();
        if (LobbyInfoText != null)
            LobbyInfoText.text = (PhotonNetwork.CountOfPlayers - PhotonNetwork.CountOfPlayersInRooms) + "로비 / " + PhotonNetwork.CountOfPlayers + "접속";
    }

    public void Disconnect() => PhotonNetwork.Disconnect();

    public override void OnDisconnected(DisconnectCause cause)
    {
        if (LobbyPanel != null) LobbyPanel.SetActive(false);
        if (RoomPanel != null) RoomPanel.SetActive(false);
    }
    #endregion 서버연결

    #region Firebase 로그인/회원가입
    public void OpenRegisterPanel()
    {
        LoginPanel.SetActive(false);
        RegisterPanel.SetActive(true);
    }

    public void OpenLoginPanel()
    {
        RegisterPanel.SetActive(false);
        LoginPanel.SetActive(true);
    }

    public void Register()
    {
        string email = RegisterEmailInput.text;
        string password = RegisterPasswordInput.text;
        string confirm = RegisterPasswordConfirmInput.text;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            RegisterStatusText.text = "이메일과 비밀번호를 입력하세요.";
            return;
        }

        if (!email.Contains("@") || !email.Contains("."))
        {
            RegisterStatusText.text = "유효한 이메일을 입력하세요.";
            return;
        }

        if (password.Length < 6)
        {
            RegisterStatusText.text = "비밀번호는 6자리 이상이어야 합니다.";
            return;
        }

        if (password != confirm)
        {
            RegisterStatusText.text = "비밀번호가 일치하지 않습니다.";
            return;
        }

        auth.CreateUserWithEmailAndPasswordAsync(email, password)
        .ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                string errorMsg = task.Exception?.GetBaseException().Message;
                Debug.LogWarning("회원가입 실패: " + task.Exception);
                RegisterStatusText.text = "회원가입 실패: " + errorMsg;
            }
            else
            {
                FirebaseUser user = task.Result.User;
                currentUserId = user.UserId;

                dbRef.Child("users").Child(currentUserId).SetRawJsonValueAsync("{\"email\":\"" + email + "\"}")
                .ContinueWithOnMainThread(dbTask =>
                {
                    if (dbTask.IsCompleted)
                    {
                        RegisterStatusText.text = "회원가입 성공!";
                        RegisterPanel.SetActive(false);
                        LoginPanel.SetActive(true);
                    }
                    else
                    {
                        Debug.LogError("Firebase 저장 실패: " + dbTask.Exception);
                    }
                });
            }
        });
    }

    public void Login()
    {
        string email = EmailInput.text;
        string password = PasswordInput.text;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            StatusText.text = "이메일과 비밀번호를 입력하세요.";
            return;
        }

        auth.SignInWithEmailAndPasswordAsync(email, password)
        .ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                string errorMsg = task.Exception?.GetBaseException().Message;
                Debug.LogWarning("로그인 실패: " + errorMsg);
                StatusText.text = $"로그인 실패: {errorMsg}";
            }
            else
            {
                FirebaseUser user = task.Result.User;
                currentUserId = user.UserId;

                // 닉네임 확인
                dbRef.Child("users").Child(currentUserId).Child("nickname")
                    .GetValueAsync().ContinueWithOnMainThread(nickTask =>
                    {
                        if (nickTask.IsCompleted)
                        {
                            if (nickTask.Result.Exists)
                            {
                                currentNickname = nickTask.Result.Value.ToString();
                                PhotonNetwork.LocalPlayer.NickName = currentNickname;
                                StatusText.text = $"환영합니다, {currentNickname}";
                                GoToLobby();
                            }
                            else
                            {
                                NicknamePanel.SetActive(true);
                            }
                        }
                        else
                        {
                            string errorMsg = nickTask.Exception?.GetBaseException().Message;
                            Debug.LogWarning("닉네임 로드 실패: " + errorMsg);
                            StatusText.text = "닉네임 로드 실패";
                        }
                    });
            }
        });
    }
    #endregion

    #region 닉네임 입력 처리
    private void ConfirmNickname()
    {
        string nickname = NicknameInput.text.Trim();
        if (string.IsNullOrEmpty(nickname))
        {
            StatusText.text = "닉네임을 입력하세요.";
            return;
        }

        dbRef.Child("nicknames").Child(nickname).GetValueAsync()
            .ContinueWithOnMainThread(checkTask =>
            {
                if (checkTask.IsCompleted)
                {
                    if (checkTask.Result.Exists)
                    {
                        StatusText.text = "이미 사용 중인 닉네임입니다.";
                        return;
                    }
                    else
                    {
                        dbRef.Child("users").Child(currentUserId).Child("nickname").SetValueAsync(nickname);
                        dbRef.Child("nicknames").Child(nickname).SetValueAsync(currentUserId);

                        currentNickname = nickname;
                        PhotonNetwork.LocalPlayer.NickName = currentNickname;
                        StatusText.text = $"닉네임 설정 완료: {nickname}";

                        NicknamePanel.SetActive(false);
                        GoToLobby();
                    }
                }
                else
                {
                    Debug.LogError("닉네임 체크 실패: " + checkTask.Exception);
                }
            });
    }

    private void SaveProfileNickname()
    {
        string nickname = ProfileNicknameInput.text.Trim();
        if (string.IsNullOrEmpty(nickname))
        {
            ProfileStatusText.text = "닉네임을 입력하세요.";
            return;
        }

        dbRef.Child("nicknames").Child(nickname).GetValueAsync()
            .ContinueWithOnMainThread(checkTask =>
            {
                if (checkTask.IsCompleted)
                {
                    if (checkTask.Result.Exists)
                    {
                        ProfileStatusText.text = "이미 사용 중인 닉네임입니다.";
                        return;
                    }
                    else
                    {
                        dbRef.Child("users").Child(currentUserId).Child("nickname").SetValueAsync(nickname);
                        dbRef.Child("nicknames").Child(nickname).SetValueAsync(currentUserId);

                        currentNickname = nickname;
                        PhotonNetwork.LocalPlayer.NickName = currentNickname;
                        ProfileStatusText.text = "닉네임 변경 완료!";
                        ProfilePanel.SetActive(false);
                        WelcomeText.text = currentNickname + "님 환영합니다.";
                    }
                }
                else
                {
                    Debug.LogError("닉네임 체크 실패: " + checkTask.Exception);
                }
            });
    }
    #endregion

    #region 공통
    private void GoToLobby()
    {
        LoginPanel.SetActive(false);
        NicknamePanel.SetActive(false);
        LobbyPanel.SetActive(true);

        PhotonNetwork.ConnectUsingSettings();
        RefreshSaveList();
    }

    public void Logout()
    {
        auth.SignOut();
        currentUserId = null;
        currentNickname = null;
        StatusText.text = "로그아웃 완료";

        if (PhotonNetwork.IsConnected)
            PhotonNetwork.Disconnect();

        LobbyPanel.SetActive(false);
        LoginPanel.SetActive(true);
    }

    public void RefreshSaveList()
    {
        if (SaveManager.Instance == null) return;

        foreach (Transform child in SaveListContent)
            Destroy(child.gameObject);

        string userId = currentUserId; // 수정
        if (string.IsNullOrEmpty(userId))
        {
            Debug.LogWarning("[SaveManager] 로그인된 유저가 없습니다.");
            return;
        }

        List<string> saves = SaveSystem.GetRoomNames(userId);
        if (saves.Count == 0)
        {
            Debug.Log("[SaveManager] 저장된 게임 없음");
            return;
        }

        foreach (var roomName in saves)
        {
            GameObject btnObj = Instantiate(SaveBtnPrefab, SaveListContent);
            btnObj.GetComponentInChildren<Text>().text = roomName;

            string capturedRoomName = roomName;
            btnObj.GetComponent<Button>().onClick.AddListener(() => OnClick_SelectSave(capturedRoomName));
        }
    }

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
    #endregion

    #region 방 리스트 갱신
    public void Connect() => PhotonNetwork.ConnectUsingSettings();

    public override void OnConnectedToMaster() => PhotonNetwork.JoinLobby();

    public override void OnJoinedLobby()
    {
        LobbyPanel.SetActive(true);
        RoomPanel.SetActive(false);

        currentUserId = string.IsNullOrEmpty(currentUserId) ? "Guest" : currentUserId;
        WelcomeText.text = (string.IsNullOrEmpty(currentNickname) ? "Guest" : currentNickname) + "님 환영합니다.";
        myList.Clear();
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

    public void MyListClick(int num)
    {
        if (num == -2) --currentPage;
        else if (num == -1) ++currentPage;
        else PhotonNetwork.JoinRoom(myList[multiple + num].Name);
        MyListRenewal();
    }

    void MyListRenewal()
    {
        maxPage = (myList.Count % CellBtn.Length == 0) ? myList.Count / CellBtn.Length : myList.Count / CellBtn.Length + 1;
        PreviousBtn.interactable = (currentPage <= 1) ? false : true;
        NextBtn.interactable = (currentPage >= maxPage) ? false : true;

        multiple = (currentPage - 1) * CellBtn.Length;
        for (int i = 0; i < CellBtn.Length; i++)
        {
            CellBtn[i].interactable = (multiple + i < myList.Count) ? true : false;
            CellBtn[i].transform.GetChild(0).GetComponent<Text>().text = (multiple + i < myList.Count) ? myList[multiple + i].Name : "";
            CellBtn[i].transform.GetChild(1).GetComponent<Text>().text = (multiple + i < myList.Count) ? myList[multiple + i].PlayerCount + "/" + myList[multiple + i].MaxPlayers : "";
        }
    }
    #endregion

    #region 방 생성/입장
    public void NewGame()
    {
        if (string.IsNullOrEmpty(currentUserId))
        {
            Debug.LogWarning("로그인 후 이용해주세요.");
            return;
        }

        SaveData data;
        if (!string.IsNullOrEmpty(selectedSaveRoomName))
        {
            data = SaveSystem.Load(currentUserId, selectedSaveRoomName);
            if (data == null)
                data = CreateNewSave(RoomInput.text);
        }
        else
        {
            data = CreateNewSave(RoomInput.text);
        }

        CreateRoom(data);
    }

    public void LoadGame()
    {
        if (string.IsNullOrEmpty(currentUserId) || string.IsNullOrEmpty(selectedSaveRoomName)) return;

        SaveData loaded = SaveSystem.Load(currentUserId, selectedSaveRoomName);
        if (loaded == null) return;

        CreateRoom(loaded);
    }

    private SaveData CreateNewSave(string roomName)
    {
        string finalRoomName = string.IsNullOrEmpty(roomName) ? "Room" + UnityEngine.Random.Range(0, 100) : roomName;
        SaveData newSave = new SaveData(finalRoomName)
        {
            saveId = Guid.NewGuid().ToString(),
            dayCount = 0,
            jobAssignments = new Dictionary<string, int>()
        };
        SaveSystem.Save(newSave, currentUserId);
        Debug.Log("[SaveManager] 새로운 저장 생성: " + finalRoomName);
        return newSave;
    }

    private void CreateRoom(SaveData data)
    {
        SaveSystem.Save(data, currentUserId);

        string roomName = string.IsNullOrEmpty(RoomInput.text) ? "Room" + UnityEngine.Random.Range(0, 100) : RoomInput.text;
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

    public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
    {
        RoomRenewal();
        ChatRPC("System", "<color=yellow>" + newPlayer.NickName + "님이 참가하셨습니다.</color>");
        RefreshPlayerSlots();
    }

    public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
    {
        RoomRenewal();
        ChatRPC("System", "<color=yellow>" + otherPlayer.NickName + "님이 퇴장하셨습니다.</color>");
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
        RoomInfoText.text = PhotonNetwork.CurrentRoom.Name + " / " + PhotonNetwork.CurrentRoom.PlayerCount + "/" + PhotonNetwork.CurrentRoom.MaxPlayers;
    }

    public void StartGame()
    {
        // 마스터 클라이언트만 실행 가능
        if (!PhotonNetwork.IsMasterClient)
        {
            Debug.LogWarning("게임 시작은 마스터 클라이언트만 가능합니다.");
            return;
        }

        // 모든 플레이어가 직업을 선택했는지 확인
        foreach (Photon.Realtime.Player player in PhotonNetwork.PlayerList)
        {
            if (!player.CustomProperties.ContainsKey("JobIndex"))
            {
                Debug.LogWarning($"플레이어 {player.NickName}이(가) 직업을 선택하지 않았습니다.");
                return;
            }
        }

        // 모든 체크 완료 후 게임 씬 로드
        PhotonNetwork.LoadLevel("SampleScene"); // 실제 게임 씬 이름으로 바꿔주세요
    }
    #endregion

    #region 채팅
    public void Send()
    {
        if (!string.IsNullOrEmpty(ChatInput.text))
        {
            PV.RPC("ChatRPC", RpcTarget.All, PhotonNetwork.NickName, ChatInput.text);
            ChatInput.text = "";
        }
    }

    [PunRPC]
    public void ChatRPC(string user, string message)
    {
        for (int i = ChatText.Length - 1; i > 0; i--)
            ChatText[i].text = ChatText[i - 1].text;
        ChatText[0].text = user + " : " + message;
    }
    #endregion

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
        if (JobBtns == null || PlayerSlots == null) return;
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

                PlayerSlotNames[i].text = p.NickName;

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
                PlayerSlots[i].SetActive(true);
                PlayerSlotNames[i].text = "빈 슬롯";
                PlayerSlotJobs[i].text = "";
                PlayerJobIcons[i].enabled = false;
            }
        }
    }

    #endregion
}
