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
        LoginBtn.onClick.AddListener(() => NetworkManager.Instance.Login(EmailInput.text, PasswordInput.text));
        RegisterBtn.onClick.AddListener(ShowRegisterPanel);
        RegisterConfirmBtn.onClick.AddListener(() =>
            NetworkManager.Instance.Register(RegisterEmailInput.text, RegisterPasswordInput.text, RegisterPasswordConfirmInput.text));
        BackToLoginBtn.onClick.AddListener(ShowLoginPanel);

        NicknameConfirmBtn.onClick.AddListener(() =>
            NetworkManager.Instance.ConfirmNickname(NicknameInput.text));

        SettingsBtn.onClick.AddListener(() => SettingsBtn.gameObject.SetActive(!SettingsBtn.gameObject.activeSelf));
        LogoutBtn.onClick.AddListener(() => NetworkManager.Instance.Logout());
        ExitGameBtn.onClick.AddListener(() => Application.Quit());

        ProfileBtn.onClick.AddListener(() =>
        {
            ProfilePanel.SetActive(true);
            ProfileNicknameInput.text = NetworkManager.Instance.currentNickname;
        });
        ProfileSaveBtn.onClick.AddListener(() =>
            NetworkManager.Instance.SaveProfileNickname(ProfileNicknameInput.text));

        LoadGameBtn.onClick.AddListener(() => NetworkManager.Instance.LoadGame());
    }

    #region UI Control Methods
    public void ShowLoginPanel()
    {
        LoginPanel.SetActive(true);
        RegisterPanel.SetActive(false);
        NicknamePanel.SetActive(false);
        LobbyPanel.SetActive(false);
    }

    public void ShowRegisterPanel()
    {
        RegisterPanel.SetActive(true);
        LoginPanel.SetActive(false);
    }

    public void ShowNicknamePanel()
    {
        NicknamePanel.SetActive(true);
        LoginPanel.SetActive(false);
    }

    public void HideNicknamePanel() => NicknamePanel.SetActive(false);
    public void ShowLobbyPanel()
    {
        LobbyPanel.SetActive(true);
        LoginPanel.SetActive(false);
        NicknamePanel.SetActive(false);
    }

    public void HideProfilePanel() => ProfilePanel.SetActive(false);
    public void UpdateWelcomeText(string name) => WelcomeText.text = $"{name}님 환영합니다.";
    #endregion

    #region Save List
    public void RefreshSaveList()
    {
        if (SaveManager.Instance == null) return;
        foreach (Transform child in SaveListContent)
            Destroy(child.gameObject);

        string userId = NetworkManager.Instance.currentUserId;
        if (string.IsNullOrEmpty(userId)) return;

        List<string> saves = SaveSystem.GetRoomNames(userId);
        foreach (var roomName in saves)
        {
            var btnObj = Instantiate(SaveBtnPrefab, SaveListContent);
            btnObj.GetComponentInChildren<Text>().text = roomName;
            string captured = roomName;
            btnObj.GetComponent<Button>().onClick.AddListener(() => OnClick_SelectSave(captured));
        }
    }

    public void OnClick_SelectSave(string roomName)
    {
        SaveSelectText.text = $"선택된 저장: {roomName}";
        SaveListPanel.SetActive(false);
    }
    #endregion
}
*/