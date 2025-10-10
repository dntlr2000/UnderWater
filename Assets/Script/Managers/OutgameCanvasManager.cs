/*using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OutgameCanvasManager : MonoBehaviour
{
    #region UI
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

    //Save Pannel
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

    //Job Pannel
    public GameObject JobSelectPanel;
    public Button[] JobBtns;
    public JobData[] jobDatas;

    //PlayerSlotsPannel
    public GameObject[] PlayerSlots;
    public Image[] PlayerJobIcons;
    public Text[] PlayerSlotNames;
    public Text[] PlayerSlotJobs;

    [Header("Settings UI")]
    public GameObject SettingsPanel;
    public Button LogoutBtn;
    public Button ExitGameBtn;
    public Button ProfileBtn;

    //ProfilePannel
    public GameObject ProfilePanel;
    public InputField ProfileNicknameInput;
    public Button ProfileSaveBtn;
    public Text ProfileStatusText;

    [Header("ETC")]
    public Text StatusText;
    #endregion

    public static OutgameCanvasManager Instance;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        SetupButtonEvents();
    }

    private void SetupButtonEvents()
    {
        LoginBtn.onClick.AddListener(() =>
        {
            NetworkManager.Instance.Login(EmailInput.text, PasswordInput.text);
        });

        RegisterBtn.onClick.AddListener(() =>
        {
            ShowRegisterPanel();
        });

        RegisterConfirmBtn.onClick.AddListener(() =>
        {
            if (RegisterPasswordInput.text != RegisterPasswordConfirmInput.text)
            {
                SetRegisterStatus("비밀번호가 일치하지 않습니다.");
                return;
            }
            NetworkManager.Instance.Register(RegisterEmailInput.text, RegisterPasswordInput.text);
        });

        BackToLoginBtn.onClick.AddListener(() => ShowLoginPanel());

        LogoutBtn.onClick.AddListener(() => NetworkManager.Instance.Logout());
        ExitGameBtn.onClick.AddListener(() => Application.Quit());
        SettingsBtn.onClick.AddListener(() => ShowSettingsPanel());
    }

    public void ShowLoginPanel()
    {
        HideAllPanels();
        LoginPanel.SetActive(true);
    }

    public void ShowRegisterPanel()
    {
        HideAllPanels();
        RegisterPanel.SetActive(true);
    }

    public void ShowNicknamePanel()
    {
        HideAllPanels();
        NicknamePanel.SetActive(true);
    }

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

    public void ShowSettingsPanel()
    {
        SettingsPanel.SetActive(true);
    }

    public void ShowProfilePanel()
    {
        ProfilePanel.SetActive(true);
    }

    public void HideSettingPanel()
    {
        ProfilePanel.SetActive(false);
    }

    public void HideProfilePanel()
    {
        ProfilePanel.SetActive(false);
    }

    private void HideAllPanels()
    {
        LoginPanel.SetActive(false);
        RegisterPanel.SetActive(false);
        NicknamePanel.SetActive(false);
        LobbyPanel.SetActive(false);
        RoomPanel.SetActive(false);
        JobSelectPanel.SetActive(false);
        SettingsPanel.SetActive(false);
        ProfilePanel.SetActive(false);
    }

    public void SetStatus(string msg)
    {
        if (StatusText != null)
            StatusText.text = msg;
    }

    public void SetLoginStatus(string msg)
    {
        if (LoginStatusText != null)
            LoginStatusText.text = msg;
    }

    public void SetRegisterStatus(string msg)
    {
        if (RegisterStatusText != null)
            RegisterStatusText.text = msg;
    }
}
*/