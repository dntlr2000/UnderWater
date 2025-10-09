using Photon.Pun;
using System;
using UnityEngine;
using Firebase.Database;
using Firebase.Extensions;
using Firebase;
using Photon.Pun.Demo.PunBasics;
using System.Linq;
using System.Collections.Generic;

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

    public float autoSaveInterval = 1f;
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

    private Player FindPlayerController(string userId)
    {
        foreach (var pc in UnityEngine.Object.FindObjectsByType<Player>(UnityEngine.FindObjectsSortMode.None))
        {
            if (pc.photonView != null && pc.photonView.Owner.UserId == userId)
                return pc;
        }
        return null;
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
        string userId = NetworkManager.Instance.currentUserId;

        SaveData data = SaveSystem.Load(userId, roomName) ?? new SaveData(roomName);

        if (data.players == null)
            data.players = new List<PlayerData>();
        if (data.jobAssignments == null)
            data.jobAssignments = new Dictionary<string, int>();
        if (data.worldProgress == null)
            data.worldProgress = new WorldProgress();

        data.saveId = data.saveId ?? Guid.NewGuid().ToString();
        data.roomName = roomName;
        data.createdDate = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");

        data.players.Clear();

        foreach (var photonPlayer in PhotonNetwork.PlayerList)
        {
            PlayerData pd = new PlayerData();
            pd.playerId = photonPlayer.UserId;

            // 씬에서 Player 찾기
            Player pc = FindPlayerController(pd.playerId);
            if (pc != null)
            {
                pd.position = new PlayerLocation(pc.transform.position);
                pd.jobIndex = (int)(pc.JobIndex ?? -1);
                /*pd.items = pc.Items?.ToArray();*/
            }
            else
            {
                // Player 오브젝트가 없으면 기본값
                pd.position = new PlayerLocation(Vector3.zero);
                pd.jobIndex = -1;
            }

            data.players.Add(pd);

            // 직업 정보도 jobAssignments에 저장
            data.jobAssignments[pd.playerId] = pd.jobIndex;
        }

        // 퀘스트 진행도 반영
        if (data.worldProgress == null)
            data.worldProgress = new WorldProgress();
        if (QuestManager.Instance != null)
        {
            var activeQuests = QuestManager.Instance.GetActiveQuests();
            if (activeQuests.Count > 0)
                data.worldProgress.QuestID = activeQuests[0].questID;
            else
                data.worldProgress.QuestID = "None"; // 또는 기본값

            data.worldProgress.Difficulty = QuestManager.Instance.Difficulty;
        }
        else
        {
            Debug.LogWarning("[SaveManager] QuestManager.Instance가 null입니다. Quest 정보 저장 생략.");
        }

        return data;
    }

    public void ApplySaveData(SaveData data)
    {
        if (data == null) return;
        if (data.players == null || data.jobAssignments == null) return;

        foreach (var pd in data.players)
        {
            if (pd == null) continue;

            // Photon 플레이어 찾기
            var photonPlayer = PhotonNetwork.PlayerList
                .FirstOrDefault(p => p.UserId == pd.playerId || p.NickName == pd.playerId);
            if (photonPlayer == null) continue;

            // 로컬 플레이어인지 확인
            if (photonPlayer.IsLocal)
            {
                Player localPlayer = Player.localPlayer;
                if (localPlayer != null)
                {
                    // 직업 동기화
                    if (data.jobAssignments.TryGetValue(pd.playerId, out int jobIndex))
                    {
                        localPlayer.SetJob(jobIndex);
                    }

                    // 위치 동기화
                    if (pd.position != null)
                    {
                        localPlayer.TeleportTo(pd.position.ToVector3());
                    }
                }
            }
        }
    }
}
