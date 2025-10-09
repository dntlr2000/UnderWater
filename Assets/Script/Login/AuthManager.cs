using System;
using Firebase.Auth;
using Firebase.Extensions;
using UnityEngine;
using UnityEngine.UI;

public class AuthManager : MonoBehaviour
{
    public InputField emailInput;
    public InputField passwordInput;
    public Text statusText;

    public static string CurrentUserId { get; private set; }

    private FirebaseAuth auth;

    private void Awake()
    {
        auth = FirebaseAuth.DefaultInstance;
    }

    public void SignUp()
    {
        string email = emailInput.text;
        string password = passwordInput.text;

        auth.CreateUserWithEmailAndPasswordAsync(email, password)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted && !task.IsFaulted && !task.IsCanceled)
                {
                    FirebaseUser newUser = task.Result.User;
                    CurrentUserId = newUser.UserId;
                    Debug.Log("[AuthManager] 회원가입 성공, UserID: " + CurrentUserId);
                    statusText.text = "회원가입 성공";
                }
                else
                {
                    Debug.LogError("[AuthManager] 회원가입 실패: " + task.Exception);
                    statusText.text = "회원가입 실패";
                }
            });
    }

    public void Login()
    {
        string email = emailInput.text;
        string password = passwordInput.text;

        auth.SignInWithEmailAndPasswordAsync(email, password)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted && !task.IsFaulted && !task.IsCanceled)
                {
                    FirebaseUser user = task.Result.User;
                    CurrentUserId = user.UserId;
                    Debug.Log("[AuthManager] 로그인 성공, UserID: " + CurrentUserId);
                    statusText.text = "로그인 성공";

                    // 로그인 성공 후 로컬 저장 불러오기 가능
                }
                else
                {
                    Debug.LogError("[AuthManager] 로그인 실패: " + task.Exception);
                    statusText.text = "로그인 실패";
                }
            });
    }

    public void Logout()
    {
        auth.SignOut();
        CurrentUserId = null;
        statusText.text = "로그아웃 완료";
    }
}
