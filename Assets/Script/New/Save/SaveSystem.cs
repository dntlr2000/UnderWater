using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System;

public static class SaveSystem
{
    private static string SavePath => Path.Combine(Application.persistentDataPath, "Saves");

    // 저장하기
    public static void Save(SaveData data)
    {
        if (data == null) return;

        if (string.IsNullOrEmpty(data.roomName)) data.roomName = "Room";

        if (!Directory.Exists(SavePath))
            Directory.CreateDirectory(SavePath);

        string json = JsonUtility.ToJson(data, true);
        string fileName = $"{data.roomName}.json";  // 방별 파일명
        File.WriteAllText(Path.Combine(SavePath, fileName), json);

        Debug.Log($"[SaveSystem] 저장 완료: {fileName}");
    }

    // 방 이름으로 저장 불러오기
    public static SaveData LoadByRoomName(string roomName)
    {
        if (string.IsNullOrEmpty(roomName)) return null;

        string path = Path.Combine(SavePath, roomName + ".json");
        if (!File.Exists(path)) return null;

        string json = File.ReadAllText(path);
        return JsonUtility.FromJson<SaveData>(json);
    }

    // 모든 세이브 불러오기
    public static List<SaveData> LoadAll()
    {
        List<SaveData> saves = new();

        if (!Directory.Exists(SavePath)) return saves;

        string[] files = Directory.GetFiles(SavePath, "*.json");
        foreach (var file in files)
        {
            string json = File.ReadAllText(file);
            SaveData data = JsonUtility.FromJson<SaveData>(json);
            saves.Add(data);
        }

        return saves;
    }

    // 방 이름 목록 반환 (로비에서 선택용)
    public static List<string> GetRoomNames()
    {
        List<string> roomNames = new();
        if (!Directory.Exists(SavePath)) return roomNames;

        string[] files = Directory.GetFiles(SavePath, "*.json");
        foreach (var f in files)
        {
            roomNames.Add(Path.GetFileNameWithoutExtension(f)); // 확장자 제거
        }
        return roomNames;
    }
}
