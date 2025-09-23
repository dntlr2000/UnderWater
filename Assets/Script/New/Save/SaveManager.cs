using Photon.Pun;
using System;
using UnityEngine;
using Firebase.Database;
using Firebase.Extensions;
using Firebase;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }
    public DatabaseReference dbRef;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        dbRef = FirebaseDatabase.GetInstance(FirebaseApp.DefaultInstance,
            "https://theoverflown-5908d-default-rtdb.firebaseio.com/").RootReference;
    }

    public float autoSaveInterval = 60f;
    private float timer;

    void Update()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        timer += Time.deltaTime;
        if (timer >= autoSaveInterval)
        {
            timer = 0f;
            SaveGame();
        }
    }

    public void SaveGame()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        SaveData data = CollectSaveData();
        string userId = NetworkManager.Instance.currentUserId;
        string nickname = NetworkManager.Instance.currentNickname;


        if (string.IsNullOrEmpty(userId))
        {
            Debug.LogWarning("[SaveManager] 로그인된 유저가 없습니다.");
            return;
        }

        // 1. 로컬 저장
        SaveSystem.Save(data, userId);
        Debug.Log("[SaveManager] 로컬 저장 완료: " + Application.persistentDataPath);

        // 2. Firebase 저장
        SaveUserInfoToFirebase(userId, nickname);
        SaveGameToFirebase(userId, data);
    }

    private void SaveUserInfoToFirebase(string userId, string nickname)
    {
        dbRef.Child("users").Child(userId).Child("nickname").SetValueAsync(nickname)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted)
                    Debug.Log("[SaveManager] Firebase 유저 정보 저장 완료");
                else
                    Debug.LogError("[SaveManager] Firebase 유저 정보 저장 실패: " + task.Exception);
            });
    }

    private void SaveGameToFirebase(string userId, SaveData data)
    {
        string json = JsonUtility.ToJson(data);
        dbRef.Child("saves").Child(userId).Child(data.saveId).SetRawJsonValueAsync(json)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted)
                    Debug.Log("[SaveManager] Firebase 클라우드 세이브 완료");
                else
                    Debug.LogError("[SaveManager] Firebase 세이브 실패: " + task.Exception);
            });
    }

    private SaveData CollectSaveData()
    {
        string roomName = PhotonNetwork.CurrentRoom?.Name ?? "Room";

        SaveData data = new SaveData(roomName);
        data.saveId = Guid.NewGuid().ToString();
        data.roomName = roomName;
        data.createdDate = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");

        data.dayCount = 5; // 실제 게임 값
        foreach (var player in PhotonNetwork.PlayerList)
        {
            PlayerData pd = new PlayerData();
            pd.playerId = player.UserId ?? player.NickName;
            pd.position = new PlayerLocation(Vector3.zero);
            pd.items = new Item[0];
            data.players.Add(pd);
        }

        return data;
    }
}
