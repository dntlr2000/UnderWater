using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using DG.Tweening;

public class OutgameCanvasManager : MonoBehaviour
{
    public static OutgameCanvasManager Instance;

    #region Login UI
    [Header("Login UI")]
    public GameObject LoginPanel;
    public TMP_InputField EmailInput;
    public TMP_InputField PasswordInput;
    public Button LoginBtn;
    public Button RegisterBtn;
    public TMP_Text LoginStatusText;

    public Button ShowPasswordBtn;
    [SerializeField] private Image _showPasswordIcon;
    [SerializeField] private Sprite _eyeOpenSprite;
    [SerializeField] private Sprite _eyeClosedSprite;
    private bool isPasswordVisible = false;
    [SerializeField] private TMP_InputField[] _loginFocusChain;
    #endregion

    #region Common Visual
    [Header("Common Visual")]
    [SerializeField] private Image _backgroundImage;
    [SerializeField] private Image _logoImage;
    [SerializeField] private TMP_Text _currentTabText;
    [SerializeField] private Color _statusNormalColor = Color.white;
    [SerializeField] private Color _statusErrorColor = Color.red;

    [Header("Background Sprites")]
    [SerializeField] private Sprite _authBackground;
    [SerializeField] private Sprite _lobbyBackground;
    [SerializeField] private Sprite _listBackground;
    [SerializeField] private Sprite _roomBackground;
    #endregion

    #region Register UI
    [Header("Register UI")]
    public GameObject RegisterPanel;
    public TMP_InputField RegisterEmailInput;
    public TMP_InputField RegisterNicknameInput;
    public TMP_InputField RegisterPasswordInput;
    public TMP_InputField RegisterPasswordConfirmInput;
    public Button RegisterConfirmBtn;
    public Button BackToLoginBtn;
    public TMP_Text RegisterStatusText;
    [SerializeField] private TMP_InputField[] _registerFocusChain;
    private int _lastFocusedIndex = -1;
    #endregion

    /*#region Nickname UI
    [Header("Nickname UI")]
    public GameObject NicknamePanel;
    public TMP_InputField NicknameInput;
    public Button NicknameConfirmBtn;
    public Button NicknameBackBtn;
    public TMP_Text NicknameStatusText;
    #endregion*/

    #region Lobby UI
    [Header("Lobby Menu")]
    public GameObject LobbyPanel;
    public Button JoinGameBtn;
    public Button CreateRoomBtn;
    public Button LoadGameBtn;
    public Button SettingsBtn;
    public Button CreditsBtn;
    public Button ExitGameBtn;
    [SerializeField] private Image _lobbyLogo;

    [Header("Lobby Legacy (이관 예정)")]
    public TMP_InputField RoomInput;
    public TMP_Text WelcomeText;
    public TMP_Text LobbyInfoText;
    public Button PreviousBtn;
    public Button NextBtn;
    public Button JoinRoomBtn;
    public TMP_Text SaveSelectText;
    public GameObject SaveListPanel;
    public Transform SaveListContent;
    public GameObject SaveBtnPrefab;
    #endregion

    #region Room List Panel
    [Header("Room List Panel")]
    public GameObject RoomListPanel;
    public TMP_Text RoomListNicknameText;
    public Button RoomListBackBtn;
    public Button RoomListSettingsBtn;
    public TMP_InputField RoomSearchInput;
    public Button RoomSearchBtn;
    public Button RoomJoinBtn;
    public Button RoomRandomJoinBtn;
    public Button RoomPrevBtn;
    public Button RoomNextBtn;
    public RoomEntry[] RoomEntries;
    [SerializeField] private Transform _pageIndicator;
    [SerializeField] private Color _dotActiveColor = Color.white;
    [SerializeField] private Color _dotInactiveColor = new Color(1f, 1f, 1f, 0.3f);
    #endregion

    #region Load Game Panel
    [Header("Load Game Panel")]
    public GameObject LoadGamePanel;
    public TMP_Text LoadGameNicknameText;
    public Button LoadGameBackBtn;
    public Button LoadGameSettingsBtn;
    public TMP_InputField SaveSearchInput;
    public Button SaveSearchBtn;
    public Button SaveLoadBtn;
    public Button SavePrevBtn;
    public Button SaveNextBtn;
    public RoomEntry[] SaveEntries;
    [SerializeField] private Transform _savePageIndicator;
    #endregion

    #region Popup
    [Header("Create Room Popup")]
    public GameObject PopupLayer;
    public Button DimBackgroundBtn;
    public GameObject CreateRoomPopup;
    public TMP_InputField CreateRoomNameInput;
    public TMP_InputField CreateRoomPasswordInput;
    public TMP_Text CreateRoomHintText;
    public Button CreateRoomConfirmBtn;
    public Button CreateRoomCloseBtn;

    [Header("Password Popup")]
    public GameObject PasswordPopup;
    public TMP_Text PasswordPopupTitle;
    public TMP_InputField PasswordInputField;
    public TMP_Text PasswordStatusText;
    public Button PasswordConfirmBtn;
    public Button PasswordCancelBtn;
    #endregion

    #region Room UI
    [Header("Room UI")]
    public GameObject RoomPanel;
    public GameObject RoomNameSplash;
    public TMP_Text RoomNameSplashText;
    public TMP_Text ListText;
    public TMP_Text RoomInfoText;
    public Button LeaveRoomBtn;
    public Button JobInfoBtn;

    [Header("Room - Ready Gauge")]
    public Image ReadyRingFill;
    public TMP_Text ActionLabelText;

    [Header("Room - Chat")]
    public GameObject ChatScrollView;
    [SerializeField] private RectTransform _chatToggleBtnRect;
    [SerializeField] private Vector2 _togglePosClosed = new Vector2(-40, -938);
    [SerializeField] private Vector2 _togglePosOpen = new Vector2(-40, -746);
    public Button ChatToggleBtn;
    public TMP_Text ChatToggleLabel;
    public TMP_Text[] ChatText;
    public TMP_InputField ChatInput;
    public Button ChatSendBtn;
    public Button StartBtn;

    [Header("Room - Job Info Popup")]
    public GameObject JobInfoPopup;
    public Button JobInfoCloseBtn;
    #endregion

    #region Job Select UI
    [Header("Job Select UI")]
    public GameObject JobSelectPanel;
    public Button[] JobBtns;
    public Sprite[] JobIcons;
    #endregion

    #region Player Slots UI
    [Header("Player Slots UI")]
    public PlayerSlot[] PlayerSlots;
    #endregion

    #region Settings Popup
    [Header("Settings Popup")]
    public Button[] SettingsTabBtns;            // 그래픽/사운드/게임/계정 순서
    public GameObject[] SettingsDetailPanels;   // 위와 같은 순서
    public Button SettingsCloseBtn;
    public TMP_Text AccountNicknameText;
    [SerializeField] private Color _settingsTabActiveColor = new Color(0.24f, 0.76f, 0.87f);
    [SerializeField] private Color _settingsTabInactiveColor = Color.white;
    #endregion

    #region Settings & Profile UI
    [Header("Settings UI")]
    public GameObject SettingsPanel;
    public Button LogoutBtn;
    public Button ProfileBtn;

    [Header("Profile UI")]
    public GameObject ProfilePanel;
    public TMP_InputField ProfileNicknameInput;
    public Button ProfileSaveBtn;
    public TMP_Text ProfileStatusText;
    #endregion

    [Header("ETC")]
    public TMP_Text StatusText;

    private void Start()
    {
        SetupButtonEvents();

        ApplyPasswordVisibility();
        EnforcePasswordFields();
    }

    private void Update()
    {
        // 로그인 패널이 활성화되어 있을 때만 동작
        if (LoginPanel != null && LoginPanel.activeSelf)
        {
            TrackFocus(_loginFocusChain);

            // 1. Tab 키: 이메일 -> 비밀번호 포커스 이동
            if (Input.GetKeyDown(KeyCode.Tab)) FocusNext(_loginFocusChain);

            // 2. Enter 키: 로그인 시도
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                // 엔터키 누르면 로그인 함수 호출
                if (AuthManager.Instance != null && !AuthManager.Instance.isLoginProcessing)
                {
                    AuthManager.Instance.TryLogin(EmailInput.text, PasswordInput.text);
                }
            }
            return;
        }

        if (RegisterPanel != null && RegisterPanel.activeSelf)
        {
            TrackFocus(_registerFocusChain);

            if (Input.GetKeyDown(KeyCode.Tab)) FocusNext(_registerFocusChain);
        }
    }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private static void Bind(Button btn, UnityEngine.Events.UnityAction action)
    {
        if (btn == null || action == null) return;
        btn.onClick.RemoveListener(action);
        btn.onClick.AddListener(action);
    }

    private void SetupButtonEvents()
    {
        // 로그인 / 회원가입
        LoginBtn.onClick.AddListener(() =>
        {
            if (!AuthManager.Instance.isLoginProcessing)
                AuthManager.Instance.TryLogin(EmailInput.text, PasswordInput.text);
        });
        RegisterBtn.onClick.AddListener(() => ShowRegisterPanel());
        RegisterConfirmBtn.onClick.AddListener(() =>
        {
            if (RegisterPasswordInput.text != RegisterPasswordConfirmInput.text)
            {
                SetRegisterStatus("비밀번호가 일치하지 않습니다.", true);
                return;
            }
            AuthManager.Instance.TryRegister(RegisterEmailInput.text, RegisterPasswordInput.text, RegisterNicknameInput.text);
        });
        Bind(BackToLoginBtn, OnRegisterBack);

        //비밀번호 보이기/숨기기 버튼 리스너
        if (ShowPasswordBtn != null)
        {
            ShowPasswordBtn.onClick.AddListener(TogglePasswordVisibility);
        }

        /*// 닉네임
        NicknameConfirmBtn.onClick.AddListener(() =>
            AuthManager._instance.TrySetNickname(NicknameInput.text));
        Bind(NicknameBackBtn, OnNicknameBack);*/

        // 로비
        // 로비 메인 메뉴
        Bind(JoinGameBtn, ShowRoomListPanel);
        Bind(CreateRoomBtn, ShowCreateRoomPopup);
        Bind(LoadGameBtn, ShowLoadGamePanel);
        Bind(SettingsBtn, ShowSettingsPanel);
        Bind(CreditsBtn, ShowCreditsPanel);
        Bind(ExitGameBtn, QuitGame);

        // 이관 예정
        if (PreviousBtn != null) PreviousBtn.onClick.AddListener(() => LobbyManager.Instance.PagePrevious());
        if (NextBtn != null) NextBtn.onClick.AddListener(() => LobbyManager.Instance.PageNext());
        if (JoinRoomBtn != null) JoinRoomBtn.onClick.AddListener(() => LobbyManager.Instance.TryJoinRandomRoom());

        Bind(RoomListBackBtn, ShowLobbyPanelFromList);
        Bind(RoomListSettingsBtn, ShowSettingsPanel);
        Bind(RoomSearchBtn, ApplyRoomSearch);
        Bind(RoomJoinBtn, JoinSelectedRoom);
        Bind(RoomRandomJoinBtn, JoinRandomRoom);
        Bind(RoomPrevBtn, RoomPagePrevious);
        Bind(RoomNextBtn, RoomPageNext);

        if (RoomEntries != null)
        {
            for (int i = 0; i < RoomEntries.Length; i++)
            {
                if (RoomEntries[i] == null || RoomEntries[i].Button == null) continue;

                int slotIndex = i;
                RoomEntries[i].Button.onClick.AddListener(
                    () => LobbyManager.Instance?.OnClickRoomButton(slotIndex));
            }
        }

        //불러오기
        Bind(LoadGameBackBtn, ShowLobbyPanelFromList);
        Bind(LoadGameSettingsBtn, ShowSettingsPanel);
        Bind(SaveSearchBtn, ApplySaveSearch);
        Bind(SaveLoadBtn, LoadSelectedSave);
        Bind(SavePrevBtn, SavePagePrevious);
        Bind(SaveNextBtn, SavePageNext);

        if (SaveEntries != null)
        {
            for (int i = 0; i < SaveEntries.Length; i++)
            {
                if (SaveEntries[i] == null || SaveEntries[i].Button == null) continue;

                int slotIndex = i;
                SaveEntries[i].Button.onClick.AddListener(
                    () => LobbyManager.Instance?.OnClickSaveButton(slotIndex));
            }
        }

        // 방
        ChatSendBtn.onClick.AddListener(() =>
        {
            RoomManager.Instance.SendChat(ChatInput.text);
            ChatInput.text = "";
        });
        StartBtn.onClick.AddListener(() => RoomManager.Instance.TryStartGame());

        Bind(LeaveRoomBtn, LeaveRoom);
        Bind(JobInfoBtn, ShowJobInfoPopup);
        Bind(JobInfoCloseBtn, HideJobInfoPopup);
        Bind(ChatToggleBtn, ToggleChatLog);

        // 5. 직업 선택 UI 누락된 직업 선택 로직 추가
        for (int i = 0; i < JobBtns.Length; i++)
        {
            int jobIndex = i; // 클로저 문제 방지
            JobBtns[i].onClick.AddListener(() => RoomManager.Instance.SelectJob(jobIndex));
        }

        // 팝업
        Bind(CreateRoomConfirmBtn, ConfirmCreateRoom);
        Bind(CreateRoomCloseBtn, HideAllPopups);
        Bind(DimBackgroundBtn, HideAllPopups);
        Bind(PasswordConfirmBtn, ConfirmPassword);
        Bind(PasswordCancelBtn, CancelPassword);
        Bind(SettingsCloseBtn, HideAllPopups);

        // 세팅
        LogoutBtn.onClick.AddListener(() => AuthManager.Instance.Logout());
        ProfileBtn.onClick.AddListener(() => ShowProfilePanel());
        ProfileSaveBtn.onClick.AddListener(() =>
            AuthManager.Instance.TrySetNickname(ProfileNicknameInput.text));

        if (SettingsTabBtns != null)
        {
            for (int i = 0; i < SettingsTabBtns.Length; i++)
            {
                if (SettingsTabBtns[i] == null) continue;
                int tabIndex = i;
                SettingsTabBtns[i].onClick.AddListener(() => SelectSettingsTab(tabIndex));
            }
        }
    }

    private void OnRegisterBack()
    {
        if (RegisterEmailInput != null) RegisterEmailInput.text = string.Empty;
        if (RegisterPasswordInput != null) RegisterPasswordInput.text = string.Empty;
        if (RegisterPasswordConfirmInput != null) RegisterPasswordConfirmInput.text = string.Empty;
        if (RegisterNicknameInput != null) RegisterNicknameInput.text = string.Empty;

        SetRegisterStatus(string.Empty);
        ShowLoginPanel();
    }

    /*private void OnNicknameBack()
    {
        if (NicknameInput != null) NicknameInput.text = string.Empty;
        SetNicknameStatus(string.Empty);

        if (AuthManager.Instance != null) AuthManager.Instance.Logout();
        else ShowLoginPanel();
    }*/

    public void TogglePasswordVisibility()
    {
        if (PasswordInput == null) return;
        isPasswordVisible = !isPasswordVisible;
        ApplyPasswordVisibility();
    }

    private void ApplyPasswordVisibility()
    {
        if (PasswordInput == null) return;

        int caret = PasswordInput.stringPosition;
        PasswordInput.contentType = isPasswordVisible
            ? TMP_InputField.ContentType.Standard
            : TMP_InputField.ContentType.Password;
        PasswordInput.ForceLabelUpdate();
        PasswordInput.stringPosition = Mathf.Clamp(caret, 0, PasswordInput.text.Length);

        if (_showPasswordIcon == null) return;

        Sprite target = isPasswordVisible ? _eyeOpenSprite : _eyeClosedSprite;
        _showPasswordIcon.sprite = target;
        _showPasswordIcon.enabled = target != null;
    }

    private void EnforcePasswordFields()
    {
        SetAsPassword(RegisterPasswordInput);
        SetAsPassword(RegisterPasswordConfirmInput);
        SetAsPassword(CreateRoomPasswordInput);
        SetAsPassword(PasswordInputField);
    }

    private static void SetAsPassword(TMP_InputField field)
    {
        if (field == null) return;

        field.contentType = TMP_InputField.ContentType.Password;
        field.ForceLabelUpdate();
    }

    #region Panel Control
    public void HideAllPanels()
    {
        LoginPanel.SetActive(false);
        RegisterPanel.SetActive(false);
        /*NicknamePanel.SetActive(false);*/
        LobbyPanel.SetActive(false);
        if (RoomListPanel != null) RoomListPanel.SetActive(false);
        if (LoadGamePanel != null) LoadGamePanel.SetActive(false);
        RoomPanel.SetActive(false);
        SettingsPanel.SetActive(false);
        ProfilePanel.SetActive(false);
        if (PopupLayer != null) PopupLayer.SetActive(false);
    }

    private void SetCurrentTab(string tabName)
    {
        if (_currentTabText == null) return;

        bool visible = !string.IsNullOrEmpty(tabName);
        _currentTabText.gameObject.SetActive(visible);
        if (visible) _currentTabText.text = tabName;
    }

    private void SetCommonLogo(bool visible)
    {
        if (_logoImage == null) return;
        _logoImage.gameObject.SetActive(visible);
    }

    private void SetBackground(Sprite sprite)
    {
        if (_backgroundImage == null || sprite == null) return;
        if (_backgroundImage.sprite == sprite) return;

        _backgroundImage.sprite = sprite;
        _backgroundImage.color = Color.white;
    }

    public void ShowLoginPanel()
    {
        HideAllPanels();
        LoginPanel.SetActive(true);
        SetBackground(_authBackground);
        SetCommonLogo(true);
        SetCurrentTab(OutgameMessages.TabLogin);

        // 로그인 패널 열릴 때 비밀번호 초기화
        _lastFocusedIndex = 0;
        isPasswordVisible = false;
        if (PasswordInput != null) PasswordInput.text = string.Empty;
        ApplyPasswordVisibility();

        SetLoginStatus(string.Empty);
        if (EmailInput != null) EmailInput.ActivateInputField();
    }
    public void ShowRegisterPanel()
    {
        HideAllPanels();
        RegisterPanel.SetActive(true);
        SetBackground(_authBackground);
        SetCommonLogo(true);
        SetCurrentTab(OutgameMessages.TabRegister);

        _lastFocusedIndex = 0;
        EnforcePasswordFields();
        SetRegisterStatus(string.Empty);
        if (RegisterEmailInput != null) RegisterEmailInput.ActivateInputField();
    }

    /*public void ShowNicknamePanel()
    {
        HideAllPanels();
        *//*NicknamePanel.SetActive(true);*//*
        SetCurrentTab(OutgameMessages.TabNickname);
    }*/

    public void ShowLobbyPanel(string nickname)
    {
        HideAllPanels();
        LobbyPanel.SetActive(true);
        SetBackground(_lobbyBackground);
        SetCommonLogo(false);
        SetCurrentTab(string.Empty);

        if (WelcomeText != null)
            WelcomeText.text = $"{nickname}님 환영합니다!";
    }

    public void ShowCreateRoomPopup()
    {
        if (PopupLayer == null || CreateRoomPopup == null) return;

        PopupLayer.SetActive(true);
        CreateRoomPopup.SetActive(true);
        if (PasswordPopup != null) PasswordPopup.SetActive(false);

        if (CreateRoomNameInput != null) CreateRoomNameInput.text = string.Empty;
        if (CreateRoomPasswordInput != null) CreateRoomPasswordInput.text = string.Empty;
        if (CreateRoomHintText != null)
            CreateRoomHintText.text = "비밀번호를 설정하지 않으면 누구나 참가할 수 있습니다.";

        if (CreateRoomNameInput != null) CreateRoomNameInput.ActivateInputField();
    }

    private void ConfirmCreateRoom()
    {
        if (LobbyManager.Instance == null) return;

        string roomName = CreateRoomNameInput != null ? CreateRoomNameInput.text.Trim() : string.Empty;
        string password = CreateRoomPasswordInput != null ? CreateRoomPasswordInput.text : string.Empty;

        if (string.IsNullOrEmpty(roomName))
        {
            SetCreateRoomHint("방 이름을 입력해주세요.", true);
            return;
        }

        if (!string.IsNullOrEmpty(password) && !RoomPassword.IsValid(password))
        {
            SetCreateRoomHint($"비밀번호는 {RoomPassword.MinLength}~{RoomPassword.MaxLength}자로 입력해주세요.", true);
            return;
        }

        HideAllPopups();
        LobbyManager.Instance.NewGame(roomName, password);
    }

    private void SetCreateRoomHint(string msg, bool isError = false)
    {
        if (CreateRoomHintText == null) return;
        CreateRoomHintText.text = msg;
        CreateRoomHintText.color = isError ? _statusErrorColor : _statusNormalColor;
    }

    public void ShowPasswordPopup(string roomName)
    {
        if (PopupLayer == null || PasswordPopup == null) return;

        PopupLayer.SetActive(true);
        PasswordPopup.SetActive(true);
        if (CreateRoomPopup != null) CreateRoomPopup.SetActive(false);

        if (PasswordPopupTitle != null) PasswordPopupTitle.text = roomName;
        if (PasswordInputField != null) PasswordInputField.text = string.Empty;
        SetPasswordStatus(string.Empty);

        if (PasswordInputField != null) PasswordInputField.ActivateInputField();
    }

    public void HidePasswordPopup()
    {
        if (PasswordPopup != null) PasswordPopup.SetActive(false);
        if (PopupLayer != null) PopupLayer.SetActive(false);
    }

    public void SetPasswordStatus(string msg, bool isError = false)
    {
        if (PasswordStatusText == null) return;
        PasswordStatusText.text = msg;
        PasswordStatusText.color = isError ? _statusErrorColor : _statusNormalColor;
    }

    private void ConfirmPassword()
    {
        LobbyManager.Instance?.ConfirmPassword(
            PasswordInputField != null ? PasswordInputField.text : string.Empty);
    }

    private void CancelPassword() => LobbyManager.Instance?.CancelPassword();

    private void HideAllPopups()
    {
        if (CreateRoomPopup != null) CreateRoomPopup.SetActive(false);
        if (PasswordPopup != null) PasswordPopup.SetActive(false);
        if (JobInfoPopup != null) JobInfoPopup.SetActive(false);
        if (SettingsPanel != null) SettingsPanel.SetActive(false);
        if (PopupLayer != null) PopupLayer.SetActive(false);
    }

    private void SelectSettingsTab(int index)
    {
        if (SettingsDetailPanels != null)
        {
            for (int i = 0; i < SettingsDetailPanels.Length; i++)
                if (SettingsDetailPanels[i] != null)
                    SettingsDetailPanels[i].SetActive(i == index);
        }

        if (SettingsTabBtns != null)
        {
            for (int i = 0; i < SettingsTabBtns.Length; i++)
            {
                if (SettingsTabBtns[i] == null) continue;
                Image img = SettingsTabBtns[i].GetComponent<Image>();
                if (img != null)
                    img.color = (i == index) ? _settingsTabActiveColor : _settingsTabInactiveColor;
            }
        }
    }

    private void ShowLobbyPanelFromList()
    {
        ShowLobbyPanel(AuthManager.Instance != null ? AuthManager.Instance.currentNickname : string.Empty);
    }

    private void ApplyRoomSearch()
    {
        if (LobbyManager.Instance == null) return;
        LobbyManager.Instance.ApplySearch(RoomSearchInput != null ? RoomSearchInput.text : string.Empty);
    }

    private void JoinSelectedRoom() => LobbyManager.Instance?.JoinSelectedRoom();
    private void JoinRandomRoom() => LobbyManager.Instance?.TryJoinRandomRoom();
    private void RoomPagePrevious() => LobbyManager.Instance?.PagePrevious();
    private void RoomPageNext() => LobbyManager.Instance?.PageNext();

    public void UpdatePageIndicator(int currentPage, int maxPage)
    {
        if (_pageIndicator == null) return;

        int count = _pageIndicator.childCount;
        for (int i = 0; i < count; i++)
        {
            Transform dot = _pageIndicator.GetChild(i);
            bool visible = i < maxPage;
            dot.gameObject.SetActive(visible);

            if (!visible) continue;

            Image image = dot.GetComponent<Image>();
            if (image != null)
                image.color = (i == currentPage - 1) ? _dotActiveColor : _dotInactiveColor;
        }
    }

    public void ShowRoomPanel(string roomName)
    {
        HideAllPanels();
        RoomPanel.SetActive(true);
        SetBackground(_roomBackground);
        SetCommonLogo(false);
        SetCurrentTab(string.Empty);
        ShowRoomNameSplash(roomName);

        for (int i = 0; i < ChatText.Length; i++)
            if (ChatText[i] != null) ChatText[i].text = string.Empty;

        if (ChatScrollView != null) ChatScrollView.SetActive(false);
        if (_chatToggleBtnRect != null) _chatToggleBtnRect.anchoredPosition = _togglePosClosed;
        if (ChatToggleLabel != null) ChatToggleLabel.text = "열기";
    }

    public void ShowSettingsPanel()
    {
        if (PopupLayer != null) PopupLayer.SetActive(true);
        if (SettingsPanel != null) SettingsPanel.SetActive(true);

        if (AccountNicknameText != null && AuthManager.Instance != null)
            AccountNicknameText.text = AuthManager.Instance.currentNickname;

        SelectSettingsTab(0);
    }
    public void ShowProfilePanel() => ProfilePanel.SetActive(true);
    public void HideProfilePanel()
    {
        if (ProfilePanel != null) ProfilePanel.SetActive(false);
    }
    private void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void ShowRoomListPanel()
    {
        HideAllPanels();
        RoomListPanel.SetActive(true);
        SetBackground(_listBackground);
        SetCommonLogo(false);
        SetCurrentTab(string.Empty);

        if (RoomListNicknameText != null && AuthManager.Instance != null)
            RoomListNicknameText.text = AuthManager.Instance.currentNickname;

        if (RoomSearchInput != null) RoomSearchInput.text = string.Empty;

        LobbyManager.Instance?.ApplySearch(string.Empty);
    }
    private void LeaveRoom()
    {
        if (Photon.Pun.PhotonNetwork.InRoom)
            Photon.Pun.PhotonNetwork.LeaveRoom();
    }

    public void ShowLoadGamePanel()
    {
        HideAllPanels();
        LoadGamePanel.SetActive(true);
        SetBackground(_listBackground);
        SetCommonLogo(false);
        SetCurrentTab(string.Empty);

        if (LoadGameNicknameText != null && AuthManager.Instance != null)
            LoadGameNicknameText.text = AuthManager.Instance.currentNickname;

        if (SaveSearchInput != null) SaveSearchInput.text = string.Empty;

        LobbyManager.Instance?.OpenSaveList();
    }

    private void ApplySaveSearch()
    {
        if (LobbyManager.Instance == null) return;
        LobbyManager.Instance.ApplySaveSearch(SaveSearchInput != null ? SaveSearchInput.text : string.Empty);
    }

    private void LoadSelectedSave() => LobbyManager.Instance?.LoadSelectedSave();
    private void SavePagePrevious() => LobbyManager.Instance?.SavePagePrevious();
    private void SavePageNext() => LobbyManager.Instance?.SavePageNext();

    public void UpdateSavePageIndicator(int currentPage, int maxPage)
    {
        if (_savePageIndicator == null) return;

        int count = _savePageIndicator.childCount;
        for (int i = 0; i < count; i++)
        {
            Transform dot = _savePageIndicator.GetChild(i);
            bool visible = i < maxPage;
            dot.gameObject.SetActive(visible);

            if (!visible) continue;

            Image image = dot.GetComponent<Image>();
            if (image != null)
                image.color = (i == currentPage - 1) ? _dotActiveColor : _dotInactiveColor;
        }
    }

    private void ShowJobInfoPopup()
    {
        if (PopupLayer != null) PopupLayer.SetActive(true);
        if (JobInfoPopup != null) JobInfoPopup.SetActive(true);
    }

    private void HideJobInfoPopup()
    {
        if (JobInfoPopup != null) JobInfoPopup.SetActive(false);
        if (PopupLayer != null) PopupLayer.SetActive(false);
    }

    private void ShowRoomNameSplash(string roomName)
    {
        if (RoomNameSplashText != null) RoomNameSplashText.text = roomName;
        if (RoomNameSplash == null) return;

        RoomNameSplash.SetActive(true);
        CanvasGroup cg = RoomNameSplash.GetComponent<CanvasGroup>();
        if (cg == null) cg = RoomNameSplash.AddComponent<CanvasGroup>();

        cg.DOKill();
        cg.alpha = 0f;
        cg.DOFade(1f, 0.3f)
          .OnComplete(() => cg.DOFade(0f, 0.5f).SetDelay(1.5f)
              .OnComplete(() => RoomNameSplash.SetActive(false)));
    }

    private void ToggleChatLog()
    {
        if (ChatScrollView == null) return;

        bool next = !ChatScrollView.activeSelf;
        ChatScrollView.SetActive(next);

        if (_chatToggleBtnRect != null)
            _chatToggleBtnRect.anchoredPosition = next ? _togglePosOpen : _togglePosClosed;

        if (ChatToggleLabel != null)
            ChatToggleLabel.text = next ? "닫기" : "열기";
    }

    public void ShowCreditsPanel() => SetStatus("제작진 화면 준비 중입니다.");
    #endregion

    #region Status / UI Updates
    public void SetStatus(string msg) { if (StatusText) StatusText.text = msg; }
    public void SetLoginStatus(string msg, bool isError = false)
    {
        if (isError) SetLoginInteractable(true);

        if (LoginStatusText == null) return;
        LoginStatusText.text = msg;
        LoginStatusText.color = isError ? _statusErrorColor : _statusNormalColor;
    }

    private void SetLoginInteractable(bool value)
    {
        if (LoginBtn != null) LoginBtn.interactable = value;
        if (RegisterBtn != null) RegisterBtn.interactable = value;
        if (EmailInput != null) EmailInput.interactable = value;
        if (PasswordInput != null) PasswordInput.interactable = value;
    }

    public void SetRegisterStatus(string msg, bool isError = false)
    {
        if (RegisterStatusText == null) return;
        RegisterStatusText.text = msg;
        RegisterStatusText.color = isError ? _statusErrorColor : _statusNormalColor;
    }

    /*public void SetNicknameStatus(string msg, bool isError = false)
    {
        if (NicknameStatusText == null) return;
        NicknameStatusText.text = msg;
        NicknameStatusText.color = isError ? _statusErrorColor : _statusNormalColor;
    }*/
    public void SetProfileStatus(string msg, bool isError = false)
    {
        if (ProfileStatusText == null) return;
        ProfileStatusText.text = msg;
        ProfileStatusText.color = isError ? _statusErrorColor : _statusNormalColor;
    }

    public void UpdateNicknameUI(string nickname)
    {
        if (WelcomeText != null)
            WelcomeText.text = $"{nickname}님 환영합니다!";

        if (ProfileNicknameInput != null)
            ProfileNicknameInput.text = nickname;
    }

    public void UpdateChat(string[] chatLines)
    {
        for (int i = 0; i < ChatText.Length; i++)
            ChatText[i].text = (i < chatLines.Length) ? chatLines[i] : "";
    }

    public void UpdatePlayerSlots(List<PlayerInfo> players)
    {
        for (int i = 0; i < PlayerSlots.Length; i++)
        {
            if (PlayerSlots[i] == null) continue;

            if (i < players.Count)
            {
                var info = players[i];
                bool hasJob = !string.IsNullOrEmpty(info.JobName) && info.JobName != "직업 없음";

                if (hasJob)
                    PlayerSlots[i].SetJobSelected(info.Nickname, info.JobName, info.JobIcon);
                else
                    PlayerSlots[i].SetJoinedNoJob(info.Nickname);
            }
            else
            {
                PlayerSlots[i].SetEmpty();
            }
        }
    }

    public void UpdateReadyGauge(int jobSelectedCount, int totalPlayerCount)
    {
        if (ReadyRingFill != null)
            ReadyRingFill.fillAmount = totalPlayerCount <= 0
                ? 0f
                : Mathf.Clamp01((float)jobSelectedCount / totalPlayerCount);

        if (ActionLabelText != null)
            ActionLabelText.text = Photon.Pun.PhotonNetwork.IsMasterClient ? "START" : "READY";
    }
    #endregion

    private void TrackFocus(TMP_InputField[] chain)
    {
        if (chain == null) return;

        GameObject selected = EventSystem.current != null
            ? EventSystem.current.currentSelectedGameObject
            : null;


        for (int i = 0; i < chain.Length; i++)
        {
            if (chain[i] == null) continue;

            if (chain[i].isFocused || (selected != null && chain[i].gameObject == selected))
            {
                _lastFocusedIndex = i;
                return;
            }
        }
    }

    private void FocusNext(TMP_InputField[] chain)
    {
        if (chain == null || chain.Length == 0) return;

        for (int step = 1; step <= chain.Length; step++)
        {
            int next = (_lastFocusedIndex + step) % chain.Length;
            TMP_InputField target = chain[next];

            if (target == null) continue;
            if (!target.IsInteractable()) continue;
            if (!target.gameObject.activeInHierarchy) continue;

            target.ActivateInputField();
            _lastFocusedIndex = next;
            return;
        }
    }
}
