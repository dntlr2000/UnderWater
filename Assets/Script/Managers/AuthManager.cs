using UnityEngine;
using UnityEngine.UI;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using Photon.Pun;
using Photon.Realtime;
using Firebase;

public class AuthManager : MonoBehaviour
{
    public static AuthManager _instance;
    public static AuthManager Instance
    {
        get
        {
            if (_instance == null) // s_Instance는 private static SaveManager s_Instance; 로 정의해야 함.
            {
                // DDOL 영역에서 객체를 찾아 복구 시도
                _instance = FindFirstObjectByType<AuthManager>();

                if (_instance == null)
                {
                    Debug.LogError($"[AuthManager] Instance를 찾을 수 없습니다. 씬 시작 객체에 {nameof(AuthManager)}를 추가했는지 확인하세요.");
                }
            }
            return _instance;
        }
    }
    private string _currentUserId;
    public string currentUserId
    {
        get
        {
            // 1. 변수에 값이 있으면 그거 씀
            if (!string.IsNullOrEmpty(_currentUserId))
                return _currentUserId;

            // 2. 변수가 비어있는데 Firebase에는 로그인 되어 있다면? -> 다시 가져옴
            if (FirebaseAuth.DefaultInstance.CurrentUser != null)
            {
                _currentUserId = FirebaseAuth.DefaultInstance.CurrentUser.UserId;
                return _currentUserId;
            }

            // 3. 진짜 아무것도 없음 (로그인 안 한 상태)
            return null;
        }
        set => _currentUserId = value;
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;

        InitializeFirebase();

        DontDestroyOnLoad(gameObject);
        Debug.Log($"[AuthManager] DDOL 설정 완료. UserID: {currentUserId}");

        if (FirebaseAuth.DefaultInstance.CurrentUser != null)
        {
            _currentUserId = FirebaseAuth.DefaultInstance.CurrentUser.UserId;
            Debug.Log($"[AuthManager] 기존 로그인 정보 복구됨: {_currentUserId}");
        }
    }

    private DatabaseReference dbRef;
    private FirebaseAuth auth;

    // AuthManager는 UI 참조를 가지지 않습니다.
    // 이전 UI 참조 변수 (LoginPanel, EmailInput 등)는 OutgameCanvasManager로 이동했습니다.

    
    public string currentNickname;

    // Firebase 초기화는 Bootstrap에서 호출됩니다.
    public void InitializeFirebase()
    {
        auth = FirebaseAuth.DefaultInstance;
        dbRef = FirebaseDatabase.GetInstance(FirebaseApp.DefaultInstance,
            "https://theoverflown-5908d-default-rtdb.firebaseio.com/").RootReference;
    }

    #region Login / Register

    public void OpenRegisterPanel() => OutgameCanvasManager.Instance.ShowRegisterPanel();
    public void OpenLoginPanel() => OutgameCanvasManager.Instance.ShowLoginPanel();

    public void TryRegister(string email, string password)
    {
        if (!ValidateRegister(email, password)) return;

        auth.CreateUserWithEmailAndPasswordAsync(email, password)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    OutgameCanvasManager.Instance.SetRegisterStatus(
                        "회원가입 실패: " + task.Exception?.GetBaseException().Message);
                }
                else
                {
                    currentUserId = task.Result.User.UserId;
                    dbRef.Child("users").Child(currentUserId).SetRawJsonValueAsync("{\"email\":\"" + email + "\"}")
                        .ContinueWithOnMainThread(dbTask =>
                        {
                            if (dbTask.IsCompleted)
                            {
                                OutgameCanvasManager.Instance.SetRegisterStatus("회원가입 성공!");
                                OutgameCanvasManager.Instance.ShowLoginPanel();
                            }
                        });
                }
            });
    }

    private bool ValidateRegister(string email, string password)
    {
        // OutgameCanvasManager에서 비밀번호 일치 여부는 이미 확인했다고 가정합니다.
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            OutgameCanvasManager.Instance.SetRegisterStatus("이메일과 비밀번호를 입력하세요.");
            return false;
        }
        if (!email.Contains("@") || !email.Contains("."))
        {
            OutgameCanvasManager.Instance.SetRegisterStatus("유효한 이메일을 입력하세요.");
            return false;
        }
        if (password.Length < 6)
        {
            OutgameCanvasManager.Instance.SetRegisterStatus("비밀번호는 6자리 이상이어야 합니다.");
            return false;
        }
        return true;
    }

    public void TryLogin(string email, string password)
    {
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            OutgameCanvasManager.Instance.SetLoginStatus("이메일과 비밀번호를 입력하세요.");
            return;
        }

        auth.SignInWithEmailAndPasswordAsync(email, password)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    OutgameCanvasManager.Instance.SetLoginStatus(
                        $"로그인 실패: {task.Exception?.GetBaseException().Message}");
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
                    OutgameCanvasManager.Instance.SetStatus($"환영합니다, {currentNickname}");
                    GoToLobby();
                }
                else
                {
                    OutgameCanvasManager.Instance.ShowNicknamePanel();
                }
            });
    }
    #endregion

    #region Nickname
    public void TrySetNickname(string nickname)
    {
        if (string.IsNullOrEmpty(nickname))
        {
            OutgameCanvasManager.Instance.SetNicknameStatus("닉네임을 입력하세요.");
            return;
        }

        dbRef.Child("nicknames").Child(nickname).GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                if (task.Result.Exists)
                {
                    OutgameCanvasManager.Instance.SetNicknameStatus("이미 사용 중인 닉네임입니다.");
                }
                else
                {
                    // 기존 닉네임 삭제 로직 (선택 사항)
                    if (!string.IsNullOrEmpty(currentNickname))
                        dbRef.Child("nicknames").Child(currentNickname).RemoveValueAsync();

                    dbRef.Child("users").Child(currentUserId).Child("nickname").SetValueAsync(nickname);
                    dbRef.Child("nicknames").Child(nickname).SetValueAsync(currentUserId);

                    currentNickname = nickname;
                    PhotonNetwork.LocalPlayer.NickName = currentNickname;

                    OutgameCanvasManager.Instance.SetNicknameStatus("닉네임 설정 완료!");
                    OutgameCanvasManager.Instance.UpdateNicknameUI(currentNickname);

                    if (OutgameCanvasManager.Instance.ProfilePanel.activeSelf)
                    {
                        OutgameCanvasManager.Instance.ProfilePanel.SetActive(false);
                    }
                    else
                    {
                        GoToLobby();
                    }
                }
            }
            else
            {
                Debug.LogError("닉네임 체크 실패: " + task.Exception);
            }
        });
    }
    #endregion

    #region Logout & GoToLobby
    public void GoToLobby()
    {
        OutgameCanvasManager.Instance.ShowLobbyPanel(currentNickname);

        if (string.IsNullOrEmpty(currentUserId))
        {
            Debug.LogWarning("[AuthManager] UserId가 없어 임시 ID를 생성합니다.");
            currentUserId = System.Guid.NewGuid().ToString();
        }
        // 포톤에 "내 ID는 이것이다"라고 알려주는 핵심 코드
        PhotonNetwork.AuthValues = new AuthenticationValues(currentUserId);
        Debug.Log($"[AuthManager] 포톤 인증 ID 설정 완료: {PhotonNetwork.AuthValues.UserId}");

        // 그 다음 연결을 시도합니다.
        if (!PhotonNetwork.IsConnected)
        {
            NetworkBootstrap.Instance.Connect();
        }

        // LobbyManager 대신 SaveSynManager를 통해 갱신
        if (SaveSyncManager.Instance != null)
        {
            SaveSyncManager.Instance.RefreshSaveList();
        }
    }

    public void Logout()
    {
        auth.SignOut();
        currentUserId = null;
        currentNickname = null;
        OutgameCanvasManager.Instance.SetStatus("로그아웃 완료");

        if (PhotonNetwork.IsConnected) PhotonNetwork.Disconnect();

        OutgameCanvasManager.Instance.ShowLoginPanel();
    }
    #endregion
}