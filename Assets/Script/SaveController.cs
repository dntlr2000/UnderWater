using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;

public class SaveController : MonoBehaviour
{
    [Serializable]
    public class PlayerLocation
    {
        public float x, y, z;

        public PlayerLocation(Vector3 v)
        {
            x = v.x;
            y = v.y;
            z = v.z;
        }

        public Vector3 ToVector3() => new Vector3(x, y, z);
    }

    [Serializable]
    public class PlayerData
    {   
        public string playerId;
        public PlayerLocation position;

        public Item[] items;
        //public float PlayetHealth;
        //public float PlayerStamina;
    }

    [Serializable]
    public class Item
    {
        //public string name;
        public int amount;
        public int ItemId; //ID로 다른 곳에서 이름이나 기능을 가져오는게 낫지 않나 ex: 마인크래프트
    }

    //private string saveFilePath;
    public GameObject PlayerObject;

    private Transform PlayerTransform;
    private string playerId;

    [Serializable]
    public class WorldProgress
    {
        public int QuestID;
        public int Difficulty;

        public int SubmarinePowerLevel; //이런 식으로 잠수함 별 능력치 레벨을 올려도 될 듯
        //public int PlayerMoney;
    }

    [Serializable]
    public class Options
    {
        public float SensivityX; //X축 감도
        public float SensivityY; //Y축 감도

        public float BGMVolume; //배경음 크기
        public float SFXVolume; //효과음 크기

        public bool isWindowMode; //창모드 여부
    }

    void Start()
    {
        //saveFilePath = Path.Combine(Application.persistentDataPath, "saveData.json");
        //PlayerTransform = PlayerObject.transform;
        //playerId = "Player001";//gameObject.name;
        //Photon으로 구현하는지, Unity6 자체 제공 패키지로 구현하는지에 따라 다른 플레이어에 대한 정보를 받아오는 방식이 달라서 다른 플레이어에 대한 저장을 알아보는건 보류
        
    }


    // ~SavePosition ~ LoadPositionButton : 위치 저장 테스트용
    //PunRPC: 다른 클라이언트에서 호출이 가능
    //[PunRPC] //Photon에서 다른 플레이어 저장 방식, Photon은 서버 내 사람들끼리 동기화만 시켜줄 뿐, 데이터를 관리하지 않으므로 클라이언트가 세이브 파일 전부 떠안음
    private void SavePosition(Transform playerTransform, string playerId)
    {
        PlayerData data = new PlayerData();
        data.playerId = playerId;
        data.position = new PlayerLocation(playerTransform.position);

        string json = JsonUtility.ToJson(data);
        string path = Application.persistentDataPath + $"/player_{playerId}_position.json";
        File.WriteAllText(path, json);

        Debug.Log($"저장 경로: {path}");
    }

    private bool LoadPosition(Transform playerTransform, string playerId)
    {
        string path = Application.persistentDataPath + $"/player_{playerId}_position.json";
        if (!File.Exists(path))
        {
            Debug.LogWarning("저장된 위치 정보가 없습니다.");
            return false;
        }

        string json = File.ReadAllText(path);
        PlayerData data = JsonUtility.FromJson<PlayerData>(json);
        playerTransform.position = data.position.ToVector3();
        Debug.Log($"[로드] 위치 복원 완료: {data.position.x}, {data.position.y}, {data.position.z}");
        return true;
    }

    public void SavePositionButton()
    {
        SavePosition(PlayerTransform, playerId);
    }

    public void LoadPositionButton()
    {
        LoadPosition(PlayerTransform, playerId);
    }

    //설정 관련
    //public OptionManager optionManager;

    public Options LoadOptions()
    {
        string path = Application.persistentDataPath + $"/options.json";

        if (!File.Exists(path))
        {
            Debug.LogWarning("저장된 설정 정보가 없습니다.");
            return null;
        }

        string json = File.ReadAllText(path);
        Options data = JsonUtility.FromJson<Options>(json);

        return data;
    }
    public void SaveOptions(float x, float y, float bgm, float sfx)
    {
        string path = Application.persistentDataPath + $"/options.json";
        Options data = new Options();

        data.SensivityX = x;
        data.SensivityY = y;
        data.BGMVolume = bgm;
        data.SFXVolume = sfx;

        string json = JsonUtility.ToJson(data);
        File.WriteAllText(path, json);

        Debug.Log($"설정 저장 경로: {path}");
    }


}
