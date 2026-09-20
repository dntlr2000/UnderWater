using UnityEngine;
using System.Collections.Generic;
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using Photon.Pun;
using Photon.Realtime;

public class AuthManager : MonoBehaviour
{
    #region Singleton
    private static AuthManager _instance;
    public static AuthManager Instance
    {
        get
        {
            if (_instance == null) 
            {
                _instance = FindFirstObjectByType<AuthManager>();

                if (_instance == null)
                {
                    Debug.LogError($"[AuthManager] Instance를 찾을 수 없습니다. 씬 시작 객체에 {nameof(AuthManager)}를 추가했는지 확인하세요.");
                }
            }
            return _instance;
        }
    }
    #endregion

    #region Fields
    [Header("Firebase")]
    [SerializeField] private string _databaseUrl = "https://theoverflown-5908d-default-rtdb.firebaseio.com/";

    private FirebaseAuth _auth;
    private DatabaseReference _dbRef;

    private bool _isFirebaseReady;
    private bool _lobbyEntered;
    private string _currentUserId;

    public bool IsFirebaseReady => _isFirebaseReady;
    public bool isLoginProcessing = false;
    public string currentNickname;

    public string currentUserId
    {
        get
        {
            if (!string.IsNullOrEmpty(_currentUserId))
                return _currentUserId;

            if (!_isFirebaseReady || _auth == null) 
                return null;

            if (_auth.CurrentUser != null)
                _currentUserId = _auth.CurrentUser.UserId;

            return _currentUserId;
        }
        set => _currentUserId = value;
    }
    private static OutgameCanvasManager UI => OutgameCanvasManager.Instance;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeFirebase();
    }

    private void OnApplicationQuit()
    {
        if (!_isFirebaseReady || _dbRef == null) return;
        if (string.IsNullOrEmpty(_currentUserId)) return;

        _dbRef.Child("users").Child(_currentUserId).Child("isLoggedIn").SetValueAsync(false);
    }
    #endregion

    #region Initialization
    // Firebase 초기화는 Bootstrap에서 호출됩니다.
    public void InitializeFirebase()
    {
        if (_isFirebaseReady) return;

        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled || task.Result != DependencyStatus.Available)
            {
                Debug.LogError($"[AuthManager] Firebase 의존성 확인 실패: {task.Result}");
                if (UI != null) UI.SetLoginStatus(OutgameMessages.AuthNotReady, true);
                return;
            }

            _auth = FirebaseAuth.DefaultInstance;
            _dbRef = FirebaseDatabase
                .GetInstance(FirebaseApp.DefaultInstance, _databaseUrl)
                .RootReference;

            if (_auth.CurrentUser != null)
            {
                _auth.SignOut();
                _currentUserId = null;
            }

            _isFirebaseReady = true;
            Debug.Log("[AuthManager] Firebase 초기화 완료.");
        });
    }
    private bool EnsureReady(bool isRegisterContext = false)
    {
        if (_isFirebaseReady && _auth != null && _dbRef != null) return true;

        if (UI != null)
        {
            if (isRegisterContext) UI.SetRegisterStatus(OutgameMessages.AuthNotReady);
            else UI.SetLoginStatus(OutgameMessages.AuthNotReady, true);
        }
        return false;
    }
    #endregion

    #region Panel Shortcut
    public void OpenRegisterPanel() => UI?.ShowRegisterPanel();
    public void OpenLoginPanel() => UI?.ShowLoginPanel();
    #endregion

    #region Register
    public void TryRegister(string email, string password, string nickname)
    {
        if (isLoginProcessing) return; // 중복 방지
        if (!EnsureReady(true)) return;

        email = email != null ? email.Trim() : string.Empty;
        nickname = nickname != null ? nickname.Trim() : string.Empty;

        if (!ValidateRegister(email, password)) return;

        if (!OutgameMessages.IsValidNickname(nickname))
        {
            UI?.SetRegisterStatus(OutgameMessages.InvalidNickname, true);
            return;
        }

        isLoginProcessing = true;
        UI?.SetRegisterStatus("회원가입 처리 중...");

        CreateAccount(email, password, nickname);
    }
    private void CreateAccount(string email, string password, string nickname)
    {
        _auth.CreateUserWithEmailAndPasswordAsync(email, password)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    isLoginProcessing = false;
                    UI?.SetRegisterStatus(OutgameMessages.ToRegisterMessage(GetAuthError(task.Exception)));
                    return;
                }

                string newUserId = task.Result.User.UserId;
                _currentUserId = newUserId;

                var payload = new Dictionary<string, object>
                {
                    { "email", email },
                    { "nickname", nickname },
                    { "isLoggedIn", false }
                };

                _dbRef.Child("users").Child(newUserId).UpdateChildrenAsync(payload)
                    .ContinueWithOnMainThread(dbTask =>
                    {
                        isLoginProcessing = false;

                        if (dbTask.IsFaulted || dbTask.IsCanceled)
                        {
                            Debug.LogError($"[AuthManager] 유저 레코드 생성 실패: {dbTask.Exception}");
                            UI?.SetRegisterStatus("계정 정보 저장에 실패했습니다.", true);
                            return;
                        }

                        _auth.SignOut();
                        _currentUserId = null;

                        UI?.SetRegisterStatus("회원가입이 완료되었습니다.");
                        UI?.ShowLoginPanel();
                    });
            });
    }

    private bool ValidateRegister(string email, string password)
    {
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            UI?.SetRegisterStatus(OutgameMessages.EmptyField, true);
            return false;
        }
        if (!OutgameMessages.IsValidEmail(email))
        {
            UI?.SetRegisterStatus(OutgameMessages.InvalidEmail, true);
            return false;
        }
        if (!OutgameMessages.IsValidPassword(password))
        {
            UI?.SetRegisterStatus(OutgameMessages.InvalidPassword, true);
            return false;
        }
        return true;
    }
    #endregion

    #region Login
    public void TryLogin(string email, string password)
    {
        if (isLoginProcessing) return;
        if (!EnsureReady()) return;

        email = email != null ? email.Trim() : string.Empty;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            UI?.SetLoginStatus(OutgameMessages.EmptyField, true);
            return;
        }
        if (!OutgameMessages.IsValidEmail(email))
        {
            UI?.SetLoginStatus(OutgameMessages.InvalidEmail, true);
            return;
        }
        if (!OutgameMessages.IsValidPassword(password))
        {
            UI?.SetLoginStatus(OutgameMessages.InvalidPassword, true);
            return;
        }

        isLoginProcessing = true;
        UI?.SetLoginStatus(OutgameMessages.LoginProcessing);

        _auth.SignInWithEmailAndPasswordAsync(email, password)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    isLoginProcessing = false;
                    UI?.SetLoginStatus(OutgameMessages.ToLoginMessage(GetAuthError(task.Exception)), true);
                    return;
                }

                CheckDuplicateSession(task.Result.User.UserId);
            });
    }

    private void CheckDuplicateSession(string userId)
    {
        _dbRef.Child("users").Child(userId).Child("isLoggedIn").GetValueAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    Debug.LogError($"[AuthManager] 접속 상태 확인 실패: {task.Exception}");
                    AbortLogin("접속 상태 확인에 실패했습니다.");
                    return;
                }

                if (ToBool(task.Result))
                {
                    Debug.LogWarning($"[AuthManager] 중복 로그인 차단: {userId}");
                    AbortLogin(OutgameMessages.DuplicateLogin);
                    return;
                }

                _currentUserId = userId;
                PhotonNetwork.AuthValues = new AuthenticationValues(userId);

                SetUserOnlineStatus(userId, true);
                LoadNickname();
            });
    }

    private void AbortLogin(string message)
    {
        if (_auth != null) _auth.SignOut();
        _currentUserId = null;
        isLoginProcessing = false;
        UI?.SetLoginStatus(message, true);
    }

    private void LoadNickname()
    {
        _dbRef.Child("users").Child(_currentUserId).Child("nickname").GetValueAsync()
            .ContinueWithOnMainThread(task =>
            {
                isLoginProcessing = false;

                if (task.IsFaulted || task.IsCanceled)
                {
                    Debug.LogError($"[AuthManager] 닉네임 조회 실패: {task.Exception}");
                    SetUserOnlineStatus(_currentUserId, false);
                    AbortLogin("사용자 정보를 불러오지 못했습니다.");
                    return;
                }

                DataSnapshot snapshot = task.Result;
                if (snapshot != null && snapshot.Exists && snapshot.Value != null)
                {
                    currentNickname = snapshot.Value.ToString();
                    PhotonNetwork.NickName = currentNickname;
                    UI?.SetStatus($"환영합니다, {currentNickname}");
                    GoToLobby();
                }
                else
                {
                    Debug.LogWarning($"[AuthManager] 닉네임 없는 계정: {_currentUserId}");
                    SetUserOnlineStatus(_currentUserId, false);
                    AbortLogin("계정 정보가 올바르지 않습니다. 다시 가입해주세요.");
                }
            });
    }
    #endregion

    #region Online Status
    private void SetUserOnlineStatus(string userId, bool isOnline)
    {
        if (!_isFirebaseReady || _dbRef == null) return;
        if (string.IsNullOrEmpty(userId)) return;

        DatabaseReference flagRef = _dbRef.Child("users").Child(userId).Child("isLoggedIn");
        flagRef.SetValueAsync(isOnline);

        if (isOnline) flagRef.OnDisconnect().SetValue(false);
        else flagRef.OnDisconnect().Cancel();
    }
    #endregion

    #region Nickname
    public void TrySetNickname(string nickname)
    {
        if (!EnsureReady()) return;

        nickname = nickname != null ? nickname.Trim() : string.Empty;

        if (!OutgameMessages.IsValidNickname(nickname))
        {
            UI?.SetProfileStatus(OutgameMessages.InvalidNickname, true);
            return;
        }
        if (string.IsNullOrEmpty(_currentUserId))
        {
            UI?.SetProfileStatus("로그인 정보가 없습니다. 다시 로그인해주세요.", true);
            return;
        }

        _dbRef.Child("users").Child(_currentUserId).Child("nickname").SetValueAsync(nickname)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    Debug.LogError($"[AuthManager] 닉네임 저장 실패: {task.Exception}");
                    UI?.SetProfileStatus("닉네임 저장에 실패했습니다.");
                    return;
                }

                currentNickname = nickname;
                PhotonNetwork.NickName = nickname;

                UI?.SetProfileStatus("닉네임이 변경되었습니다.");
                UI?.UpdateNicknameUI(nickname);
                UI?.HideProfilePanel();
            });
    }

    /*private void ApplyNickname(string nickname)
    {
        string previous = currentNickname;

        _dbRef.Child("nicknames").Child(nickname).SetValueAsync(_currentUserId)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    Debug.LogError($"[AuthManager] 닉네임 등록 실패: {task.Exception}");
                    UI?.SetNicknameStatus("닉네임 저장에 실패했습니다.");
                    return;
                }

                _dbRef.Child("users").Child(_currentUserId).Child("nickname").SetValueAsync(nickname);

                if (!string.IsNullOrEmpty(previous) && previous != nickname)
                    _dbRef.Child("nicknames").Child(previous).RemoveValueAsync();

                currentNickname = nickname;
                PhotonNetwork.NickName = nickname;

                UI?.SetNicknameStatus("닉네임 설정이 완료되었습니다.");
                UI?.UpdateNicknameUI(nickname);

                bool fromProfile = UI != null
                    && UI.ProfilePanel != null
                    && UI.ProfilePanel.activeSelf;

                if (fromProfile) UI.ProfilePanel.SetActive(false);
                else GoToLobby();
            });
    }*/
    #endregion

    #region Lobby & Logout
    public void GoToLobby()
    {
        if (_lobbyEntered) return;
        _lobbyEntered = true;

        if (string.IsNullOrEmpty(_currentUserId))
        {
            Debug.LogWarning("[AuthManager] UserId가 없어 로비 진입을 중단합니다.");
            _lobbyEntered = false;
            UI?.ShowLoginPanel();
            return;
        }

        UI?.ShowLobbyPanel(currentNickname);
        PhotonNetwork.AuthValues = new AuthenticationValues(_currentUserId);

        if (!PhotonNetwork.IsConnected)
        {
            if (NetworkBootstrap.Instance != null) NetworkBootstrap.Instance.Connect();
            else Debug.LogError("[AuthManager] NetworkBootstrap 인스턴스가 없습니다.");
        }

        if (SaveSyncManager.Instance != null)
            SaveSyncManager.Instance.RefreshSaveList();
    }

    public void Logout()
    {
        if (!string.IsNullOrEmpty(currentUserId))
        {
            SetUserOnlineStatus(currentUserId, false);
        }

        if (_auth != null) _auth.SignOut();

        _currentUserId = null;
        currentNickname = null;
        isLoginProcessing = false;
        _lobbyEntered = false;

        if (PhotonNetwork.IsConnected) PhotonNetwork.Disconnect();

        UI?.SetStatus("로그아웃 완료");
        UI?.ShowLoginPanel();
    }
    #endregion

    #region Helpers
    private static bool ToBool(DataSnapshot snapshot)
    {
        if (snapshot == null || !snapshot.Exists || snapshot.Value == null) return false;

        switch (snapshot.Value)
        {
            case bool b: return b;
            case long l: return l != 0;
            case string s: return bool.TryParse(s, out bool parsed) && parsed;
            default: return false;
        }
    }

    private static AuthError GetAuthError(System.AggregateException exception)
    {
        if (exception == null) return AuthError.Failure;

        FirebaseException firebaseEx = exception.GetBaseException() as FirebaseException;
        if (firebaseEx == null) return AuthError.Failure;

        return (AuthError)firebaseEx.ErrorCode;
    }
    #endregion
}