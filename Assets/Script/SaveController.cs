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
    }


    //private string saveFilePath;
    public GameObject PlayerObject;

    private Transform PlayerTransform;
    private string playerId;

    void Start()
    {
        //saveFilePath = Path.Combine(Application.persistentDataPath, "saveData.json");
        PlayerTransform = PlayerObject.transform;
        playerId = "Player001";//gameObject.name;
    }

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
        /*
        if (File.Exists(saveFilePath))
        {

        }
        else
        {
            Debug.LogWarning("저장된 파일이 없습니다.");
            return false;
        }
        */
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

    
}
