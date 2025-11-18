using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SaveData
{
    public string saveId; // 고유 ID
    public string roomName;
    public string createdDate;    // 저장일자 (yyyy-MM-dd HH:mm:ss)

    public int dayCount;  // 예: 게임 진행 시간/일수
    // Dictionary는 JsonUtility.ToJson으로 직렬화되지 않습니다.
    // 하지만 Unity 에디터 외부에서 사용하거나 Firebase 저장을 위해 유지한다고 가정합니다.
    public Dictionary<string, int> jobAssignments = new(); // PlayerID (UserId) → JobIndex 

    public List<PlayerData> players = new(); // 플레이어별 데이터
    public WorldProgress worldProgress = new();
    public Options options = new();

    public SaveData(string roomName)
    {
        this.roomName = roomName;
        // 날짜 형식은 파일 이름으로 사용될 경우를 대비해 슬래시를 하이픈으로 변경했습니다.
        this.createdDate = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        this.dayCount = 0;

    }
}

[Serializable]
public class PlayerData
{
    // playerId는 AuthManager의 UserId 또는 ActorNumber 기반 ID를 사용해야 합니다.
    public string playerId;
    public string playerName; // Nickname
    public PlayerLocation position;
    public Item[] items; // 게임 내 아이템 구조체
    public int jobIndex; // 선택된 직업 인덱스
}

[Serializable]
public class PlayerLocation
{
    public float x, y, z;
    public PlayerLocation(Vector3 v) { x = v.x; y = v.y; z = v.z; }
    public Vector3 ToVector3() => new Vector3(x, y, z);
}

// Item, WorldProgress, Options 구조체는 그대로 유지
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