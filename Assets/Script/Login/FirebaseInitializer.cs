using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using UnityEngine;

public class FirebaseInitializer : MonoBehaviour
{
    [Header("Firebase Realtime Database URL")]
    public string databaseUrl = "https://theoverflown-5908d-default-rtdb.firebaseio.com/";

    private void Start()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            var app = FirebaseApp.DefaultInstance;
            // Database URL 명시적으로 지정
            FirebaseDatabase database = FirebaseDatabase.GetInstance(app, databaseUrl);

            Debug.Log("Firebase 초기화 완료, Database URL 설정됨");
        });
    }
}
