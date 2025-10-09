using System;
using System.Collections.Generic;
using UnityEngine;
using static SaveController;

[Serializable]
public class SaveData
{
    public string saveId; // 고유 ID
    public string roomName;
    public string createdDate;    // 저장일자 (yyyy-MM-dd HH:mm:ss)

    public int dayCount;  // 예: 게임 진행 시간/일수
    public Dictionary<string, int> jobAssignments = new(); // NickName 또는 고유 ID → JobIndex

    public List<PlayerData> players = new(); // 플레이어별 데이터
    public WorldProgress worldProgress = new();
    public Options options = new();

    public SaveData(string roomName)
    {
        this.roomName = roomName;
        this.createdDate = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        this.dayCount = 0;
        this.jobAssignments = new Dictionary<string, int>(); // ← 꼭 필요
        this.players = new List<PlayerData>();
        this.worldProgress = new WorldProgress();
    }
}

[Serializable]
public class PlayerData
{
    public string playerId;
    public PlayerLocation position;
    public Item[] items;
    public int jobIndex;
}

[Serializable]
public class PlayerLocation
{
    public float x, y, z;
    public PlayerLocation(Vector3 v) { x = v.x; y = v.y; z = v.z; }
    public Vector3 ToVector3() => new Vector3(x, y, z);
}

[Serializable]
public class Item
{
    public int itemId;
    public int amount;
}

[Serializable]
public class WorldProgress
{
    public string QuestID;
    public int Difficulty;
    public int SubmarinePowerLevel;
}

[Serializable]
public class Options
{
    public float SensivityX;
    public float SensivityY;
    public float BGMVolume;
    public float SFXVolume;
    public bool isWindowMode;
}