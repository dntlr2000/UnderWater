using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OutgameCanvasManager : MonoBehaviour
{
    public static OutgameCanvasManager Instance;

    #region Login UI
    [Header("Login UI")]
    public GameObject LoginPanel;
    public InputField EmailInput;
    public InputField PasswordInput;
    public Button LoginBtn;
    public Button RegisterBtn;
    public Text LoginStatusText;
    #endregion

    #region Register UI
    [Header("Register UI")]
    public GameObject RegisterPanel;
    public InputField RegisterEmailInput;
    public InputField RegisterPasswordInput;
    public InputField RegisterPasswordConfirmInput;
    public Button RegisterConfirmBtn;
    public Button BackToLoginBtn;
    public Text RegisterStatusText;
    #endregion

    #region Nickname UI
    [Header("Nickname UI")]
    public GameObject NicknamePanel;
    public InputField NicknameInput;
    public Button NicknameConfirmBtn;
    public Text NicknameStatusText;
    #endregion

    #region Lobby UI
    [Header("Lobby UI")]
    public GameObject LobbyPanel;
    public InputField RoomInput;
    public Text WelcomeText;
    public Text LobbyInfoText;
    public Button[] CellBtn;
    public Button PreviousBtn;
    public Button NextBtn;
    public Button CreateRoomBtn;
    public Button JoinRoomBtn;
    public Button SettingsBtn;
    public Button LoadGameBtn;
    public Text SaveSelectText;
    public GameObject SaveListPanel;
    public Transform SaveListContent;
    public GameObject SaveBtnPrefab;
    #endregion

    #region Room UI
    [Header("Room UI")]
    public GameObject RoomPanel;
    public Text ListText;
    public Text RoomInfoText;
    public Text[] ChatText;
    public InputField ChatInput;
    public Button ChatSendBtn;
    public Button StartBtn;
    #endregion

    #region Job Select UI
    [Header("Job Select UI")]
    public GameObject JobSelectPanel;
    public Button[] JobBtns;
    public Sprite[] JobIcons;
    #endregion

    #region Player Slots UI
    [Header("Player Slots UI")]
    public GameObject[] PlayerSlots;
    public Image[] PlayerJobIcons;
    public Text[] PlayerSlotNames;
    public Text[] PlayerSlotJobs;
    #endregion

    #region Settings & Profile UI
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
    #endregion

    [Header("ETC")]
    public Text StatusText;

    private void Start()
    {
        SetupButtonEvents();
    }
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }


    private void SetupButtonEvents()
    {
        // 로그인 / 회원가입
        LoginBtn.onClick.AddListener(() =>
            AuthManager._instance.TryLogin(EmailInput.text, PasswordInput.text));
        RegisterBtn.onClick.AddListener(() => ShowRegisterPanel());
        RegisterConfirmBtn.onClick.AddListener(() =>
        {
            if (RegisterPasswordInput.text != RegisterPasswordConfirmInput.text)
            {
                SetRegisterStatus("비밀번호가 일치하지 않습니다.");
                return;
            }
            AuthManager._instance.TryRegister(RegisterEmailInput.text, RegisterPasswordInput.text);
        });
        BackToLoginBtn.onClick.AddListener(() => ShowLoginPanel());

        // 닉네임
        NicknameConfirmBtn.onClick.AddListener(() =>
            AuthManager._instance.TrySetNickname(NicknameInput.text));

        // 로비
        SettingsBtn.onClick.AddListener(() => ShowSettingsPanel());
        LoadGameBtn.onClick.AddListener(() => SaveSyncManager.Instance.ToggleSaveList());
        PreviousBtn.onClick.AddListener(() => LobbyManager.Instance.PagePrevious());
        NextBtn.onClick.AddListener(() => LobbyManager.Instance.PageNext());

        // 방 생성/참가 로직 추가
        CreateRoomBtn.onClick.AddListener(() => LobbyManager.Instance.NewGame(RoomInput.text));
        JoinRoomBtn?.onClick.AddListener(() => LobbyManager.Instance.TryJoinRandomRoom());

        if (CellBtn != null)
        {
            for (int i = 0; i < CellBtn.Length; i++)
            {
                int index = i; // 클로저 문제 방지를 위해 로컬 변수 사용

                // LobbyManager에 OnClickRoomButton(int index) 함수가 있다고 가정합니다.
                // 해당 함수는 클릭된 버튼 인덱스를 통해 현재 페이지의 방 정보를 찾아 참가해야 합니다.
                CellBtn[i].onClick.AddListener(() => LobbyManager.Instance.OnClickRoomButton(index));
            }
        }

        // 방
        ChatSendBtn.onClick.AddListener(() =>
        {
            RoomManager.Instance.SendChat(ChatInput.text);
            ChatInput.text = "";
        });
        StartBtn.onClick.AddListener(() => RoomManager.Instance.TryStartGame());

        // 5. 직업 선택 UI 누락된 직업 선택 로직 추가
        for (int i = 0; i < JobBtns.Length; i++)
        {
            int jobIndex = i; // 클로저 문제 방지
            JobBtns[i].onClick.AddListener(() => RoomManager.Instance.SelectJob(jobIndex));
        }

        // 세팅
        LogoutBtn.onClick.AddListener(() => AuthManager._instance.Logout());
        ExitGameBtn.onClick.AddListener(() => Application.Quit());
        ProfileBtn.onClick.AddListener(() => ShowProfilePanel());
        ProfileSaveBtn.onClick.AddListener(() =>
            AuthManager._instance.TrySetNickname(ProfileNicknameInput.text));
    }

    #region Panel Control
    public void HideAllPanels()
    {
        LoginPanel.SetActive(false);
        RegisterPanel.SetActive(false);
        NicknamePanel.SetActive(false);
        LobbyPanel.SetActive(false);
        RoomPanel.SetActive(false);
        SettingsPanel.SetActive(false);
        ProfilePanel.SetActive(false);
    }

    public void ShowLoginPanel() { HideAllPanels(); LoginPanel.SetActive(true); }
    public void ShowRegisterPanel() { HideAllPanels(); RegisterPanel.SetActive(true); }
    public void ShowNicknamePanel() { HideAllPanels(); NicknamePanel.SetActive(true); }

    public void ShowLobbyPanel(string nickname)
    {
        HideAllPanels();
        LobbyPanel.SetActive(true);
        WelcomeText.text = $"{nickname}님 환영합니다!";
    }

    public void ShowRoomPanel()
    {
        HideAllPanels();
        RoomPanel.SetActive(true);
    }

    public void ShowSettingsPanel() => SettingsPanel.SetActive(true);
    public void ShowProfilePanel() => ProfilePanel.SetActive(true);
    #endregion

    #region Status / UI Updates
    public void SetStatus(string msg) { if (StatusText) StatusText.text = msg; }
    public void SetLoginStatus(string msg) { if (LoginStatusText) LoginStatusText.text = msg; }
    public void SetRegisterStatus(string msg) { if (RegisterStatusText) RegisterStatusText.text = msg; }
    public void SetNicknameStatus(string msg) { if (NicknameStatusText) NicknameStatusText.text = msg; }
    public void SetProfileStatus(string msg) { if (ProfileStatusText) ProfileStatusText.text = msg; }

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
            //  1. 요구 사항: 모든 슬롯은 항상 활성화
            PlayerSlots[i].SetActive(true);

            if (i < players.Count)
            {
                // 실제 플레이어 정보가 있을 경우
                var info = players[i];
                PlayerSlotNames[i].text = info.Nickname;
                PlayerSlotJobs[i].text = info.JobName;

                // JobIcon이 null일 수 있으므로 null 체크
                PlayerJobIcons[i].sprite = info.JobIcon;
                PlayerJobIcons[i].enabled = info.JobIcon != null; // 아이콘이 있을 때만 Image 컴포넌트 활성화
            }
            else
            {
                //  2. 빈 슬롯 초기화: 텍스트 및 아이콘 리셋
                PlayerSlotNames[i].text = "대기 중..."; // 닉네임
                PlayerSlotJobs[i].text = "직업 선택 대기"; // 직업 상태
                PlayerJobIcons[i].sprite = null; // 아이콘 제거
                PlayerJobIcons[i].enabled = false; // 이미지 컴포넌트 비활성화
            }
        }
    }
    #endregion
}
