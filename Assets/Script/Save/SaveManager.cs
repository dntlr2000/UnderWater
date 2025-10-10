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

    private string GetStablePlayerId(Photon.Realtime.Player p)
    {
        // 우선 UserId, 없으면 ActorNumber, 없으면 NickName (최후)
        if (!string.IsNullOrEmpty(p.UserId)) return p.UserId;
        if (p.ActorNumber > 0) return $"Actor_{p.ActorNumber}";
        if (!string.IsNullOrEmpty(p.NickName)) return p.NickName;
        return $"Unknown_{p.ActorNumber}";
    }

    private Player FindPlayerController(string stableId)
    {
        if (string.IsNullOrEmpty(stableId)) return null;

        // 먼저 시도: UserId 매칭 (일반적)
        foreach (var pc in UnityEngine.Object.FindObjectsByType<Player>(UnityEngine.FindObjectsSortMode.None))
        {
            if (pc.photonView == null) continue;
            var owner = pc.photonView.Owner;
            if (owner != null)
            {
                // owner.UserId 우선 비교
                if (!string.IsNullOrEmpty(owner.UserId) && owner.UserId == stableId) return pc;

                // ActorNumber 비교 (we stored as "Actor_x" maybe)
                if (stableId.StartsWith("Actor_"))
                {
                    if (stableId == $"Actor_{owner.ActorNumber}") return pc;
                }

                // 닉네임 비교 (fallback)
                if (!string.IsNullOrEmpty(owner.NickName) && owner.NickName == stableId) return pc;
            }
        }

        // 못 찾으면 null 반환
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
        string userId = NetworkManager.Instance?.currentUserId;

        SaveData data = SaveSystem.Load(userId, roomName) ?? new SaveData(roomName);

        data.players = data.players ?? new List<PlayerData>();
        data.jobAssignments = data.jobAssignments ?? new Dictionary<string, int>();
        data.worldProgress = data.worldProgress ?? new WorldProgress();

        data.saveId = data.saveId ?? Guid.NewGuid().ToString();
        data.roomName = roomName;
        data.createdDate = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");

        data.players.Clear();
        data.jobAssignments.Clear();

        foreach (var photonPlayer in PhotonNetwork.PlayerList)
        {
            PlayerData pd = new PlayerData();

            // stable id 확보 (UserId 우선, 없으면 ActorNumber 기반)
            string stableId = GetStablePlayerId(photonPlayer);
            pd.playerId = stableId;

            // 씬에서 Player 찾기 (FindPlayerController는 stableId 규칙을 이해함)
            Player pc = FindPlayerController(stableId);
            if (pc != null)
            {
                pd.position = new PlayerLocation(pc.transform.position);
                pd.jobIndex = (int)(pc.JobIndex ?? -1);
                // pd.items = pc.Items?.ToArray();
            }
            else
            {
                pd.position = new PlayerLocation(Vector3.zero);
                pd.jobIndex = -1;
            }

            data.players.Add(pd);

            // jobAssignments에 저장: key는 stableId (null/빈 문자열 차단)
            if (!string.IsNullOrEmpty(stableId))
            {
                // 덮어쓰기 허용(최신 값)
                data.jobAssignments[stableId] = pd.jobIndex;
            }
        }

        // worldProgress 처리 (기존 로직 유지)
        if (QuestManager.Instance != null)
        {
            var activeQuests = QuestManager.Instance.GetActiveQuests();
            data.worldProgress.QuestID = (activeQuests.Count > 0) ? activeQuests[0].questID : "None";
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
            if (pd == null || string.IsNullOrEmpty(pd.playerId)) continue;

            // 1) UserId 일치 시도
            var photonPlayer = PhotonNetwork.PlayerList.FirstOrDefault(p => !string.IsNullOrEmpty(p.UserId) && p.UserId == pd.playerId);

            // 2) ActorNumber 기반 ("Actor_{num}")
            if (photonPlayer == null && pd.playerId.StartsWith("Actor_"))
            {
                if (int.TryParse(pd.playerId.Replace("Actor_", ""), out int actorNum))
                    photonPlayer = PhotonNetwork.PlayerList.FirstOrDefault(p => p.ActorNumber == actorNum);
            }

            // 3) NickName 매칭 (fallback)
            if (photonPlayer == null)
                photonPlayer = PhotonNetwork.PlayerList.FirstOrDefault(p => p.NickName == pd.playerId);

            if (photonPlayer == null) continue;

            // 로컬 플레이어에게만 적용 (원래 로직 유지)
            if (photonPlayer.IsLocal)
            {
                Player localPlayer = Player.localPlayer;
                if (localPlayer == null) continue;

                if (data.jobAssignments.TryGetValue(pd.playerId, out int jobIndex))
                {
                    localPlayer.SetJob(jobIndex);
                }

                if (pd.position != null)
                {
                    localPlayer.TeleportTo(pd.position.ToVector3());
                }
            }
        }
    }
}
