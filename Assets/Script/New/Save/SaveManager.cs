using Photon.Pun;
using System;
using System.Net.NetworkInformation;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public float autoSaveInterval = 60f; // 1분마다 저장
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
        SaveSystem.Save(data);
        Debug.Log(Application.persistentDataPath);
    }

    private SaveData CollectSaveData()
    {
        string roomName = PhotonNetwork.CurrentRoom?.Name ?? "Room";

        SaveData data = new SaveData(roomName);
        data.saveId = Guid.NewGuid().ToString();
        data.roomName = PhotonNetwork.CurrentRoom.Name;
        data.createdDate = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");

        // 예시: 게임 상태 반영
        data.dayCount = 5; // (게임에서 실제 값 넣기)
        foreach (var player in PhotonNetwork.PlayerList)
        {
            PlayerData pd = new PlayerData();
            pd.playerId = player.UserId ?? player.NickName;
            pd.position = new PlayerLocation(Vector3.zero); // 실제 위치 가져오기
            pd.items = new Item[0]; // 인벤토리 저장
            data.players.Add(pd);
        }

        return data;
    }
}
