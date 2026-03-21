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
    public Dictionary<string, int> jobAssignments = new(); // PlayerID (UserId) → JobIndex 

    public List<PlayerData> players = new(); // 플레이어별 데이터
    public WorldProgress worldProgress = new();
    //public Options options = new(); // 현재 설정은 개인 클라이언트에 의존하도록 구현하여 일단 여기선 주석처리
    public List<BoxSaveData> storageBoxes = new List<BoxSaveData>();

    public List<EntitySaveData> worldEntities = new List<EntitySaveData>();

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
    public InventoryData items;
    public int jobIndex; // 선택된 직업 인덱스
    public ConditionData conditionData;

    public List<string> completedQuestIds = new List<string>(); // 완료된 퀘스트 ID 목록
    public List<QuestProgressData> activeQuests = new List<QuestProgressData>(); // 현재 진행중인 퀘스트 정보
}

[Serializable]
public class PlayerLocation
{
    public float x, y, z;
    public PlayerLocation(Vector3 v) { x = v.x; y = v.y; z = v.z; }
    public Vector3 ToVector3() => new Vector3(x, y, z);
}

// Item, WorldProgress, Options 구조체는 그대로 유지
//InventoryData는 InventoryFrame.cs에 존재하므로 일단 이 곳에 옮기거나 정의하지 않음
/*
[Serializable]
public class Item
{
    public int itemId;
    public int amount;
}
*/

[Serializable]
public class QuestProgressData
{
    public string questId;
    public int[] objectiveCounts; // 각 목표별 현재 달성 수
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

[Serializable]
public class ConditionData
{
    public bool isSaved;
    public float health;
    public float hunger;
    public float thirst;
    public float oxygen;
    public float vitality;
    public float stamina;
}

[Serializable]
public class BoxSaveData
{
    public string boxId; // 창고의 고유 이름 (boxName)
    public InventoryData items; // 창고 안에 든 아이템과 돈
}

public interface ISavable
{
    // Photon Instantiate에 사용할 프리팹 경로 (ex: "FieldItem/Object1")
    // SceneObject_를 붙여서 처음부터 씬에 배치되어있는 오브젝트를 구분함
    string PrefabPath { get; }

    // 오브젝트 고유의 데이터를 JSON 형태로 반환하여 저장할 수 있게 (ex: 드랍된 아이템이면 아이템 ID, 수량 등)
    // [Serializable]로 선언된 구조체 = > return JsonUtility.ToJson(data);
    string GetSaveDataJson();

    // JsonUtility.FromJson<FieldItemSaveStruct>(json)으로 해당 구조체로 변환
    void RestoreSaveData(string json);
}
[Serializable]
public class EntitySaveData
{
    public string prefabPath;
    public PlayerLocation position;
    public float yRotation;
    public string customDataJson; // 각 오브젝트 고유의 데이터가 담길 JSON
}
