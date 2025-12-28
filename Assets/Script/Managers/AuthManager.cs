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

    public bool isLoginProcessing = false;

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
        Debug.Log($"[AuthManager] DDOL 설정 완료.");

        if (FirebaseAuth.DefaultInstance.CurrentUser != null)
        {
            Debug.Log($"[AuthManager] 캐시된 세션 정리: {FirebaseAuth.DefaultInstance.CurrentUser.UserId}");
            FirebaseAuth.DefaultInstance.SignOut(); // 확실하게 연결 끊기
            _currentUserId = null;
        }
    }

    private void OnApplicationQuit()
    {
        if (!string.IsNullOrEmpty(currentUserId) && dbRef != null)
        {
            // 동기적으로 처리하기 위해 SetUserOnlineStatus 대신 직접 호출하거나, 
            // 앱 종료 시점이라 비동기가 보장되지 않으므로 최선을 다해 요청 전송
            dbRef.Child("users").Child(currentUserId).Child("isLoggedIn").SetValueAsync(false);
        }
    }

    private DatabaseReference dbRef;
    private FirebaseAuth auth;
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
        if (isLoginProcessing) return; // 중복 방지
        if (!ValidateRegister(email, password)) return;

        isLoginProcessing = true;
        OutgameCanvasManager.Instance.SetRegisterStatus("회원가입 처리 중...");

        auth.CreateUserWithEmailAndPasswordAsync(email, password)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    OutgameCanvasManager.Instance.SetRegisterStatus(
                        "회원가입 실패: " + task.Exception?.GetBaseException().Message);
                    isLoginProcessing = false;
                }
                else
                {
                    currentUserId = task.Result.User.UserId;
                    string json = "{\"email\":\"" + email + "\", \"isLoggedIn\":false}";
                    dbRef.Child("users").Child(currentUserId).SetRawJsonValueAsync(json)
                        .ContinueWithOnMainThread(dbTask =>
                        {
                            isLoginProcessing = false;
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
        if (isLoginProcessing) return;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            OutgameCanvasManager.Instance.SetLoginStatus("이메일과 비밀번호를 입력하세요.");
            return;
        }

        isLoginProcessing = true;
        OutgameCanvasManager.Instance.SetLoginStatus("로그인 중...");

        auth.SignInWithEmailAndPasswordAsync(email, password)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    OutgameCanvasManager.Instance.SetLoginStatus(
                        $"로그인 실패: {task.Exception?.GetBaseException().Message}");
                    isLoginProcessing = false;
                }
                else
                {
                    string tempUserId = task.Result.User.UserId;

                    dbRef.Child("users").Child(tempUserId).Child("isLoggedIn").GetValueAsync()
                        .ContinueWithOnMainThread(checkTask =>
                        {
                            if (checkTask.IsFaulted)
                            {
                                OutgameCanvasManager.Instance.SetLoginStatus("접속 상태 확인 실패");
                                auth.SignOut();
                                isLoginProcessing = false;
                                return;
                            }

                            // DB에 값이 있고, true라면 이미 접속 중
                            if (checkTask.Result.Exists &&
                                checkTask.Result.Value != null &&
                                (bool)checkTask.Result.Value == true)
                            {
                                Debug.LogWarning($"[AuthManager] 중복 로그인 감지: {tempUserId}");
                                auth.SignOut(); // 즉시 로그아웃 시킴
                                OutgameCanvasManager.Instance.SetLoginStatus("다른 기기에서 사용중인 아이디입니다. 다시 시도해주세요. ");
                                isLoginProcessing = false;
                            }
                            else
                            {
                                // 접속 허용
                                currentUserId = tempUserId;
                                PhotonNetwork.AuthValues = new AuthenticationValues { UserId = currentUserId };

                                // 온라인 상태로 변경 및 OnDisconnect 설정
                                SetUserOnlineStatus(currentUserId, true);

                                LoadNickname();
                            }
                        });
                }
            });
    }

    private void SetUserOnlineStatus(string userId, bool isOnline)
    {
        if (string.IsNullOrEmpty(userId)) return;

        // 1. 현재 상태 즉시 기록
        dbRef.Child("users").Child(userId).Child("isLoggedIn").SetValueAsync(isOnline);

        // 2. 앱 강제 종료/인터넷 끊김 시 서버가 자동으로 false로 바꾸도록 예약
        if (isOnline)
        {
            dbRef.Child("users").Child(userId).Child("isLoggedIn").OnDisconnect().SetValue(false);
        }
        else
        {
            // 로그아웃 시에는 OnDisconnect 예약 취소 (선택 사항이지만 안전하게)
            dbRef.Child("users").Child(userId).Child("isLoggedIn").OnDisconnect().Cancel();
        }
    }

    private void LoadNickname()
    {
        dbRef.Child("users").Child(currentUserId).Child("nickname")
            .GetValueAsync().ContinueWithOnMainThread(task =>
            {
                isLoginProcessing = false;

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
        if (!string.IsNullOrEmpty(currentUserId))
        {
            SetUserOnlineStatus(currentUserId, false);
        }

        auth.SignOut();
        currentUserId = null;
        currentNickname = null;
        isLoginProcessing = false;
        OutgameCanvasManager.Instance.SetStatus("로그아웃 완료");

        if (PhotonNetwork.IsConnected) PhotonNetwork.Disconnect();

        OutgameCanvasManager.Instance.ShowLoginPanel();
    }
    #endregion
}