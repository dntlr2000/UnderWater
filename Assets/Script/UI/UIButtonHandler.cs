/*using UnityEngine;
using UnityEngine.UI;

public class UIButtonHandler : MonoBehaviour
{
    [Header("로그인/회원가입")]
    public Button LoginBtn;
    public Button RegisterBtn;
    public Button RegisterConfirmBtn;
    public Button BackToLoginBtn;
    public Button NicknameConfirmBtn;

    [Header("로비/방 선택")]
    public Button[] RoomCells;
    public Button PreviousBtn;
    public Button NextBtn;
    public Button LoadGameBtn;

    [Header("방/게임")]
    public Button DisconnectBtn;
    public Button CreateRoomBtn;
    public Button JoinRandomRoomBtn;
    public Button LeaveRoomBtn;
    public Button StartBtn;
    public Button SendBtn;

    [Header("직업 선택")]
    public Button[] JobButtons;

    [Header("설정")]
    public Button LogoutBtn;
    public Button ExitGameBtn;
    public Button ProfileBtn;
    public Button ProfileSaveBtn;

    public void InitializeButtons()
    {
        // 로그인/회원가입
        LoginBtn.onClick.AddListener(() =>
        {
            if (AuthManager.Instance == null)
            {
                Debug.LogError("AuthManager.Instance가 null입니다!");
                return;
            }

            if (OutgameCanvasManager.Instance == null)
            {
                Debug.LogError("OutgameCanvasManager.Instance가 null입니다!");
                return;
            }

            if (OutgameCanvasManager.Instance.EmailInput == null ||
                OutgameCanvasManager.Instance.PasswordInput == null)
            {
                Debug.LogError("EmailInput 또는 PasswordInput이 Inspector에 연결되지 않았습니다!");
                return;
            }

            AuthManager.Instance.TryLogin(
                OutgameCanvasManager.Instance.EmailInput.text,
                OutgameCanvasManager.Instance.PasswordInput.text
            );
        });

        RegisterBtn.onClick.AddListener(() =>
            OutgameCanvasManager.Instance.ShowRegisterPanel());

        RegisterConfirmBtn.onClick.AddListener(() =>
        {
            var regUI = OutgameCanvasManager.Instance;
            if (regUI.RegisterPasswordInput.text != regUI.RegisterPasswordConfirmInput.text)
            {
                regUI.SetRegisterStatus("비밀번호가 일치하지 않습니다.");
                return;
            }
            AuthManager.Instance.TryRegister(
                regUI.RegisterEmailInput.text,
                regUI.RegisterPasswordInput.text
            );
        });

        BackToLoginBtn.onClick.AddListener(() => OutgameCanvasManager.Instance.ShowLoginPanel());
        NicknameConfirmBtn.onClick.AddListener(() =>
            AuthManager.Instance.TrySetNickname(OutgameCanvasManager.Instance.NicknameInput.text));

        // 로비/방
        for (int i = 0; i < RoomCells.Length; i++)
        {
            int index = i;
            RoomCells[i].onClick.AddListener(() => LobbyManager.Instance.JoinRoomAtCellIndex(index));
        }
        PreviousBtn.onClick.AddListener(() => LobbyManager.Instance.PagePrevious());
        NextBtn.onClick.AddListener(() => LobbyManager.Instance.PageNext());
        LoadGameBtn.onClick.AddListener(() => SaveSynManager.Instance.ShowSaveList());

        // 방/게임
        DisconnectBtn.onClick.AddListener(() => LobbyManager.Instance.Disconnect());
        CreateRoomBtn.onClick.AddListener(() => LobbyManager.Instance.CreateRoom("NewRoom"));
        JoinRandomRoomBtn.onClick.AddListener(() => LobbyManager.Instance.JoinRandomRoom());
        LeaveRoomBtn.onClick.AddListener(() => RoomManager.Instance.LeaveRoom());
        StartBtn.onClick.AddListener(() => RoomManager.Instance.TryStartGame());
        SendBtn.onClick.AddListener(() =>
        {
            RoomManager.Instance.SendChat(OutgameCanvasManager.Instance.ChatInput.text);
            OutgameCanvasManager.Instance.ChatInput.text = "";
        });

        // 직업 선택
        for (int i = 0; i < JobButtons.Length; i++)
        {
            int index = i;
            JobButtons[i].onClick.AddListener(() => RoomManager.Instance.SelectJob(index));
        }

        // 설정
        LogoutBtn.onClick.AddListener(() => AuthManager.Instance.Logout());
        ExitGameBtn.onClick.AddListener(() => Application.Quit());
        ProfileBtn.onClick.AddListener(() => OutgameCanvasManager.Instance.ShowProfilePanel());
        ProfileSaveBtn.onClick.AddListener(() =>
            AuthManager.Instance.TrySetNickname(OutgameCanvasManager.Instance.ProfileNicknameInput.text));
    }
}
*/