/*using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using Firebase.Auth;
using Firebase.Extensions;
using Firebase.Database;
using Firebase;
using UnityEngine.SceneManagement;
using System.Linq;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    #region Singleton
    public static NetworkManager Instance;
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        InitializeFirebase();
        InitializeUI();
        InitializePhoton();
        InitializeSaveManager();

        if (SaveManager.Instance != null)
        {
            SaveManager.OnSaveDataChanged -= OnSaveDataChangedHandler;
            SaveManager.OnSaveDataChanged += OnSaveDataChangedHandler;
        }
    }

    #endregion

    #region UI References
    [Header("Login UI")]
    public GameObject LoginPanel;
    public InputField EmailInput;
    public InputField PasswordInput;
    public Button LoginBtn;
    public Button RegisterBtn;
    public Text LoginStatusText;

    [Header("Register UI")]
    public GameObject RegisterPanel;
    public InputField RegisterEmailInput;
    public InputField RegisterPasswordInput;
    public InputField RegisterPasswordConfirmInput;
    public Button RegisterConfirmBtn;
    public Button BackToLoginBtn;
    public Text RegisterStatusText;

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

    [Header("Save UI")]
    public Button LoadGameBtn;
    public Text SaveSelectText;
    public GameObject SaveListPanel;
    public Transform SaveListContent;
    public GameObject SaveBtnPrefab;

    [Header("Room UI")]
    public GameObject RoomPanel;
    public Text ListText;
    public Text RoomInfoText;
    public Text[] ChatText;
    public InputField ChatInput;
    public Button StartBtn;

    [Header("Job UI")]
    public GameObject JobSelectPanel;
    public Button[] JobBtns;
    public JobData[] jobDatas;

    [Header("Player Slots UI")]
    public GameObject[] PlayerSlots;
    public Image[] PlayerJobIcons;
    public Text[] PlayerSlotNames;
    public Text[] PlayerSlotJobs;

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

    [Header("ETC")]
    public Text StatusText;
    public PhotonView PV;
    public bool isLoadedFromSave = false;
    #endregion

    #region Firebase
    private FirebaseAuth auth;
    private DatabaseReference dbRef;
    public string currentUserId;
    public string currentNickname;

    private void InitializeFirebase()
    {
        auth = FirebaseAuth.DefaultInstance;
        dbRef = FirebaseDatabase.GetInstance(FirebaseApp.DefaultInstance,
            "https://theoverflown-5908d-default-rtdb.firebaseio.com/").RootReference;
    }
    #endregion

    #region Photon
    private List<RoomInfo> myList = new List<RoomInfo>();
    private int currentPage = 1, maxPage, multiple;
    public RoomData currentRoomData = new RoomData();

    private void InitializePhoton()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
        Screen.SetResolution(960, 540, false);
    }

    public void Connect() => PhotonNetwork.ConnectUsingSettings();
    public override void OnConnectedToMaster() => PhotonNetwork.JoinLobby();

    public override void OnJoinedLobby()
    {
        LobbyPanel.SetActive(true);
        RoomPanel.SetActive(false);
        currentUserId ??= "Guest";
        WelcomeText.text = (string.IsNullOrEmpty(currentNickname) ? "Guest" : currentNickname) + "님 환영합니다.";
        myList.Clear();
    }

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
    #endregion

    #region UI Initialization
    private void InitializeUI()
    {
        // 로그인 / 회원가입
        LoginBtn.onClick.AddListener(Login);
        RegisterBtn.onClick.AddListener(OpenRegisterPanel);
        RegisterConfirmBtn.onClick.AddListener(Register);
        BackToLoginBtn.onClick.AddListener(OpenLoginPanel);

        // 닉네임
        NicknameConfirmBtn.onClick.AddListener(ConfirmNickname);

        // 설정
        SettingsBtn.onClick.AddListener(() => SettingsPanel.SetActive(!SettingsPanel.activeSelf));
        LogoutBtn.onClick.AddListener(Logout);
        ExitGameBtn.onClick.AddListener(() => Application.Quit());
        ProfileBtn.onClick.AddListener(() =>
        {
            ProfilePanel.SetActive(true);
            ProfileNicknameInput.text = currentNickname;
        });
        ProfileSaveBtn.onClick.AddListener(SaveProfileNickname);
    }
    #endregion

    #region SaveManager
    private void InitializeSaveManager()
    {
        if (SaveManager.Instance == null)
        {
            GameObject go = new GameObject("SaveManager");
            go.AddComponent<SaveManager>();
        }
    }

    private string selectedSaveRoomName = null;

    public void RefreshSaveList()
    {
        if (SaveManager.Instance == null) return;

        foreach (Transform child in SaveListContent) Destroy(child.gameObject);

        if (string.IsNullOrEmpty(currentUserId))
        {
            Debug.LogWarning("[SaveManager] 로그인된 유저가 없습니다.");
            return;
        }

        var saves = SaveSystem.GetRoomNames(currentUserId);
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
        Debug.Log($"[OnClick_SelectSave] roomName = {roomName}");
        Debug.Log($"[OnClick_SelectSave] SaveSelectText = {SaveSelectText}");
        Debug.Log($"[OnClick_SelectSave] SaveListPanel = {SaveListPanel}");

        if (SaveSelectText == null)
        {
            Debug.LogError("SaveSelectText가 Inspector에 연결되지 않았습니다!");
            return;
        }

        if (SaveListPanel == null)
        {
            Debug.LogError("SaveListPanel이 Inspector에 연결되지 않았습니다!");
            return;
        }

        if (roomName == null)
        {
            Debug.LogError("roomName이 null입니다. RefreshSaveList()에서 잘못 전달된 것 같습니다.");
            return;
        }

        selectedSaveRoomName = roomName;
        SaveSelectText.text = $"선택된 저장: {roomName}";
        SaveListPanel.SetActive(false);
    }

    public void ToggleSaveList() => SaveListPanel.SetActive(!SaveListPanel.activeSelf);

    // 이벤트 핸들러: SaveManager에서 변경된 SaveData JSON을 받아 Photon RPC로 전체 전송
    private void OnSaveDataChangedHandler(string saveJson)
    {
        if (!PhotonNetwork.IsMasterClient) return; // 마스터만 브로드캐스트
        if (PV == null)
        {
            Debug.LogWarning("[NetworkManager] PV(PhotonView)가 없음 - 브로드캐스트 실패");
            return;
        }

        try
        {
            PV.RPC("RPC_BroadcastSaveData", RpcTarget.All, saveJson);
            Debug.Log("[NetworkManager] SaveData 전체 브로드캐스트 실행");
        }
        catch (Exception ex)
        {
            Debug.LogError("[NetworkManager] SaveData 브로드캐스트 실패: " + ex);
        }
    }

    // RPC: 모든 클라이언트가 수신 -> 로컬 SaveManager에 적용
    [PunRPC]
    public void RPC_BroadcastSaveData(string saveJson)
    {
        SaveData data = null;
        try
        {
            data = JsonUtility.FromJson<SaveData>(saveJson);
        }
        catch (Exception ex)
        {
            Debug.LogError("[NetworkManager] SaveData 역직렬화 실패: " + ex);
            return;
        }

        if (SaveManager.Instance == null)
        {
            Debug.LogWarning("[NetworkManager] SaveManager가 없어 SaveData 적용 불가");
            return;
        }

        SaveManager.Instance.SetCurrentSave(data);
        SaveManager.Instance.ApplySaveData(data);

        Debug.Log("[NetworkManager] 수신된 SaveData를 로컬에 적용 완료");
    }

    // OnDestroy 또는 OnDisable에서 이벤트 해제
    private void OnDestroy()
    {
        SaveManager.OnSaveDataChanged -= OnSaveDataChangedHandler;
    }
    #endregion

    #region Login / Register
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

        if (!ValidateRegister(email, password, confirm)) return;

        auth.CreateUserWithEmailAndPasswordAsync(email, password)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    RegisterStatusText.text = "회원가입 실패: " + task.Exception?.GetBaseException().Message;
                }
                else
                {
                    currentUserId = task.Result.User.UserId;
                    dbRef.Child("users").Child(currentUserId).SetRawJsonValueAsync("{\"email\":\"" + email + "\"}")
                        .ContinueWithOnMainThread(dbTask =>
                        {
                            if (dbTask.IsCompleted)
                            {
                                RegisterStatusText.text = "회원가입 성공!";
                                RegisterPanel.SetActive(false);
                                LoginPanel.SetActive(true);
                            }
                        });
                }
            });
    }

    private bool ValidateRegister(string email, string password, string confirm)
    {
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            RegisterStatusText.text = "이메일과 비밀번호를 입력하세요.";
            return false;
        }

        if (!email.Contains("@") || !email.Contains("."))
        {
            RegisterStatusText.text = "유효한 이메일을 입력하세요.";
            return false;
        }

        if (password.Length < 6)
        {
            RegisterStatusText.text = "비밀번호는 6자리 이상이어야 합니다.";
            return false;
        }

        if (password != confirm)
        {
            RegisterStatusText.text = "비밀번호가 일치하지 않습니다.";
            return false;
        }
        return true;
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
                    StatusText.text = $"로그인 실패: {task.Exception?.GetBaseException().Message}";
                }
                else
                {
                    currentUserId = task.Result.User.UserId;
                    PhotonNetwork.AuthValues = new AuthenticationValues { UserId = currentUserId };

                    LoadNickname();
                }
            });
    }

    private void LoadNickname()
    {
        dbRef.Child("users").Child(currentUserId).Child("nickname")
            .GetValueAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted && task.Result.Exists)
                {
                    currentNickname = task.Result.Value.ToString();
                    PhotonNetwork.LocalPlayer.NickName = currentNickname;
                    StatusText.text = $"환영합니다, {currentNickname}";
                    GoToLobby();
                }
                else
                {
                    NicknamePanel.SetActive(true);
                }
            });
    }
    #endregion

    #region Nickname
    private void ConfirmNickname() => SetNickname(NicknameInput.text.Trim(), StatusText, NicknamePanel);
    private void SaveProfileNickname() => SetNickname(ProfileNicknameInput.text.Trim(), ProfileStatusText, ProfilePanel, true);

    private void SetNickname(string nickname, Text statusText, GameObject panel, bool updateWelcome = false)
    {
        if (string.IsNullOrEmpty(nickname))
        {
            statusText.text = "닉네임을 입력하세요.";
            return;
        }

        dbRef.Child("nicknames").Child(nickname).GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                if (task.Result.Exists)
                {
                    statusText.text = "이미 사용 중인 닉네임입니다.";
                }
                else
                {
                    dbRef.Child("users").Child(currentUserId).Child("nickname").SetValueAsync(nickname);
                    dbRef.Child("nicknames").Child(nickname).SetValueAsync(currentUserId);

                    currentNickname = nickname;
                    PhotonNetwork.LocalPlayer.NickName = currentNickname;

                    statusText.text = "닉네임 설정 완료!";
                    panel.SetActive(false);

                    if (updateWelcome) WelcomeText.text = currentNickname + "님 환영합니다.";
                    else GoToLobby();
                }
            }
            else
            {
                Debug.LogError("닉네임 체크 실패: " + task.Exception);
            }
        });
    }
    #endregion

    #region Lobby
    private void GoToLobby()
    {
        LoginPanel.SetActive(false);
        NicknamePanel.SetActive(false);
        LobbyPanel.SetActive(true);

        if (!PhotonNetwork.IsConnected) PhotonNetwork.ConnectUsingSettings();
        RefreshSaveList();
    }

    public void Logout()
    {
        auth.SignOut();
        currentUserId = null;
        currentNickname = null;
        StatusText.text = "로그아웃 완료";

        if (PhotonNetwork.IsConnected) PhotonNetwork.Disconnect();

        LobbyPanel.SetActive(false);
        LoginPanel.SetActive(true);
    }
    #endregion

    #region Room Management
    public void NewGame()
    {
        if (string.IsNullOrEmpty(currentUserId)) return;

        SaveData data = string.IsNullOrEmpty(selectedSaveRoomName)
            ? CreateNewSave(RoomInput.text)
            : SaveSystem.Load(currentUserId, selectedSaveRoomName) ?? CreateNewSave(RoomInput.text);

        CreateRoom(data);
    }

    public void LoadGame()
    {
        if (string.IsNullOrEmpty(currentUserId) || string.IsNullOrEmpty(selectedSaveRoomName)) return;

        var data = SaveSystem.Load(currentUserId, selectedSaveRoomName);
        if (data != null)
        {
            // 저장 데이터를 SaveManager에 세팅
            if (SaveManager.Instance != null)
                SaveManager.Instance.SetCurrentSave(data);

            CreateRoom(data);
        }
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
        return newSave;
    }

    private void CreateRoom(SaveData data)
    {
        SaveSystem.Save(data, currentUserId);
        currentRoomData = new RoomData();
        currentRoomData.LoadFromSaveData(data);

        string roomName = string.IsNullOrEmpty(data.roomName) ? "Room" + UnityEngine.Random.Range(0, 100) : data.roomName;

        RoomOptions options = new RoomOptions
        {
            MaxPlayers = 2,
            IsVisible = true,
            IsOpen = true,
            CustomRoomProperties = new ExitGames.Client.Photon.Hashtable
            {
                { "SaveOwner", currentUserId },
                { "CreatedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") }
            }
        };

        PhotonNetwork.CreateRoom(roomName, options);
        Debug.Log($"[NetworkManager] 방 생성 요청: {roomName}");
    }

    public override void OnCreatedRoom()
    {
        Debug.Log($"[NetworkManager] 방 생성 완료: {PhotonNetwork.CurrentRoom.Name}");

        // SaveManager가 있으면 현재 Save 세팅
        if (SaveManager.Instance != null && SaveManager.Instance.GetCurrentSave() == null)
        {
            SaveData data = new SaveData(PhotonNetwork.CurrentRoom.Name);
            SaveManager.Instance.SetCurrentSave(data);
        }
    }
    #endregion

    #region Chat
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

    #region Job Management
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

        if (isLoadedFromSave)
        {
            // 저장에서 불러온 경우 직업 변경/취소 금지
            Debug.Log("[NetworkManager] 저장된 직업 불러오기 상태: 직업 변경 불가");
            return;
        }

        if (hasJob && (int)props["JobIndex"] == index) // 취소
        {
            props["JobIndex"] = null;
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);
            JobBtns[index].GetComponent<Image>().color = Color.white;
            RefreshJobButtons();
            return;
        }

        if (hasJob) return;

        foreach (var player in PhotonNetwork.PlayerList)
        {
            if (player != PhotonNetwork.LocalPlayer &&
                player.CustomProperties.TryGetValue("JobIndex", out object taken) &&
                taken != null && (int)taken == index)
                return;
        }

        props["JobIndex"] = index;
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        currentRoomData.jobIndices[GetSlotIndex(currentUserId)] = index;

        JobBtns[index].GetComponent<Image>().color = Color.green;
        RefreshJobButtons();
    }

    public void CancelJob()
    {
        if (!PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("JobIndex")) return;

        var props = new ExitGames.Client.Photon.Hashtable { ["JobIndex"] = null };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        currentRoomData.ResetJob(currentUserId);

        RefreshJobButtons();
        RefreshPlayerSlots();
    }

    private int GetSlotIndex(string userId)
    {
        for (int i = 0; i < currentRoomData.playerIds.Length; i++)
            if (currentRoomData.playerIds[i] == userId) return i;
        return -1;
    }

    public override void OnPlayerPropertiesUpdate(Photon.Realtime.Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        if (JobBtns != null && PlayerSlots != null && changedProps.ContainsKey("JobIndex"))
            RefreshJobButtons();

        RefreshPlayerSlots();
    }

    public void RefreshJobButtons()
    {
        for (int i = 0; i < JobBtns.Length; i++)
        {
            bool isTakenByOther = false;
            foreach (var player in PhotonNetwork.PlayerList)
            {
                if (player != PhotonNetwork.LocalPlayer &&
                    player.CustomProperties.TryGetValue("JobIndex", out object job) &&
                    job != null && (int)job == i)
                {
                    isTakenByOther = true;
                    break;
                }
            }

            bool isMyJob = PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("JobIndex", out object myJob) &&
                           myJob != null && (int)myJob == i;

            JobBtns[i].interactable = !isTakenByOther && (!PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("JobIndex") || isMyJob) && !isLoadedFromSave;

            var img = JobBtns[i].GetComponent<Image>();
            if (img != null) img.color = isMyJob ? Color.green : Color.white;
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

    private void ApplySavedJobs()
    {
        var save = SaveManager.Instance.GetCurrentSave();
        if (save == null) return;

        string userId = NetworkManager.Instance.currentUserId;
        var myPlayer = save.players.FirstOrDefault(p => p.playerId == userId);

        if (myPlayer != null)
        {
            Debug.Log($"LocalPlayer.UserId: {PhotonNetwork.LocalPlayer.UserId}");
            Debug.Log($"Saved playerId: {myPlayer.playerId}");

            // 내 저장된 직업 적용
            var props = PhotonNetwork.LocalPlayer.CustomProperties;
            props["JobIndex"] = myPlayer.jobIndex;
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);

            // 저장에서 불러온 경우, 취소/선택 제한
            isLoadedFromSave = myPlayer.jobIndex >= 0;
        }

        // 다른 플레이어 직업 반영
        foreach (var pd in save.players)
        {
            if (pd.playerId == userId) continue;
            var photonPlayer = PhotonNetwork.PlayerList.FirstOrDefault(p => p.UserId == pd.playerId);
            if (photonPlayer != null)
            {
                var props = photonPlayer.CustomProperties;
                props["JobIndex"] = pd.jobIndex;
                photonPlayer.SetCustomProperties(props);
            }
        }

        RefreshJobButtons();
        RefreshPlayerSlots();
    }
    #endregion
    #region Room Join / Leave / Start
    public void JoinRandomRoom() => PhotonNetwork.JoinRandomRoom();
    public void LeaveRoom() => PhotonNetwork.LeaveRoom();

    public override void OnJoinedRoom()
    {
        Debug.Log("[NetworkManager] OnJoinedRoom 호출됨");

        // RoomPanel / LobbyPanel 처리
        LobbyPanel.SetActive(false);
        RoomPanel.SetActive(true);

        // SaveManager 존재 확인
        if (SaveManager.Instance == null)
        {
            Debug.LogWarning("[NetworkManager] SaveManager 없음 → 생성 중");
            GameObject go = new GameObject("SaveManager");
            go.AddComponent<SaveManager>();
        }

        var save = SaveManager.Instance.GetCurrentSave();
        if (save == null)
        {
            // 마스터는 방 생성 시 SaveData를 만든 상태이므로 세팅
            if (PhotonNetwork.IsMasterClient)
            {
                save = new SaveData(PhotonNetwork.CurrentRoom.Name);
                SaveManager.Instance.SetCurrentSave(save);
                Debug.Log("[NetworkManager] Master가 새로운 SaveData를 생성했습니다.");
            }
            else
            {
                Debug.Log("[NetworkManager] 비마스터는 SaveData를 아직 수신하지 않았습니다.");
            }
        }

        // 마스터는 전체 Save 브로드캐스트 시작
        if (PhotonNetwork.IsMasterClient)
        {
            string json = JsonUtility.ToJson(save);
            PV.RPC("RPC_BroadcastSaveData", RpcTarget.All, json);
            Debug.Log("[NetworkManager] Master가 SaveData를 전체에 브로드캐스트함");
        }
    }

    public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
    {
        currentRoomData.LoadFromRoomProperties();

        if (!currentRoomData.AddPlayer(newPlayer.UserId, -1, out _))
        {
            Debug.LogWarning($"{newPlayer.NickName} 방에 들어올 수 없음");
            PhotonNetwork.CloseConnection(newPlayer);
            return;
        }

        currentRoomData.SaveToRoomProperties();

        RoomRenewal();
        ChatRPC("System", $"<color=yellow>{newPlayer.NickName}님이 참가하셨습니다.</color>");
        RefreshPlayerSlots();
    }

    public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
    {
        currentRoomData.RemovePlayer(otherPlayer.UserId);
        currentRoomData.SaveToRoomProperties();

        RoomRenewal();
        ChatRPC("System", $"<color=yellow>{otherPlayer.NickName}님이 퇴장하셨습니다.</color>");
        RefreshPlayerSlots();
    }

    public override void OnMasterClientSwitched(Photon.Realtime.Player newMasterClient)
    {
        StartBtn.interactable = PhotonNetwork.IsMasterClient;
    }

    private void RoomRenewal()
    {
        ListText.text = string.Join(", ", Array.ConvertAll(PhotonNetwork.PlayerList, p => p.NickName));
        RoomInfoText.text = $"{PhotonNetwork.CurrentRoom.Name} / {PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers}";
    }

    private void ClearChat()
    {
        ChatInput.text = "";
        foreach (var chat in ChatText) chat.text = "";
    }

    public void StartGame()
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            Debug.LogWarning("게임 시작은 마스터 클라이언트만 가능합니다.");
            return;
        }

        foreach (var player in PhotonNetwork.PlayerList)
        {
            if (!player.CustomProperties.ContainsKey("JobIndex"))
            {
                Debug.LogWarning($"{player.NickName}이 직업을 선택하지 않았습니다.");
                return;
            }
        }

        PhotonNetwork.LoadLevel("SampleScene"); // 실제 게임 씬 이름
    }
    #endregion

    #region Room List Pagination
    public void MyListClick(int num)
    {
        if (num == -2) --currentPage;
        else if (num == -1) ++currentPage;
        else PhotonNetwork.JoinRoom(myList[multiple + num].Name);

        RefreshRoomListUI();
    }

    private void RefreshRoomListUI()
    {
        maxPage = (myList.Count % CellBtn.Length == 0) ? myList.Count / CellBtn.Length : myList.Count / CellBtn.Length + 1;
        PreviousBtn.interactable = currentPage > 1;
        NextBtn.interactable = currentPage < maxPage;

        multiple = (currentPage - 1) * CellBtn.Length;
        for (int i = 0; i < CellBtn.Length; i++)
        {
            bool valid = multiple + i < myList.Count;
            CellBtn[i].interactable = valid;
            CellBtn[i].transform.GetChild(0).GetComponent<Text>().text = valid ? myList[multiple + i].Name : "";
            CellBtn[i].transform.GetChild(1).GetComponent<Text>().text = valid ? $"{myList[multiple + i].PlayerCount}/{myList[multiple + i].MaxPlayers}" : "";
        }
    }
    #endregion

    #region Update
    private void Update()
    {
        StatusText.text = PhotonNetwork.NetworkClientState.ToString();
        if (LobbyInfoText != null)
            LobbyInfoText.text = $"{PhotonNetwork.CountOfPlayers - PhotonNetwork.CountOfPlayersInRooms}로비 / {PhotonNetwork.CountOfPlayers}접속";
    }
    #endregion
}*/