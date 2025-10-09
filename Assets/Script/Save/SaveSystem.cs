using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System;

public static class SaveSystem
{
    private static string SavePath => Path.Combine(Application.persistentDataPath, "Saves");

    // 저장하기
    public static void Save(SaveData data, string userId)
    {
        if (data == null) return;
        if (string.IsNullOrEmpty(userId)) userId = "Guest";

        if (!Directory.Exists(SavePath))
            Directory.CreateDirectory(SavePath);

        string json = JsonUtility.ToJson(data, true);
        string fileName = $"{userId}_{data.roomName}.json";  // 유저ID + 방이름 기반 파일명
        File.WriteAllText(Path.Combine(SavePath, fileName), json);

        Debug.Log($"[SaveSystem] 저장 완료: {fileName}");
    }

    // 방 이름으로 저장 불러오기
    public static SaveData Load(string userId, string roomName)
    {
        if (string.IsNullOrEmpty(userId)) userId = "Guest";
        if (string.IsNullOrEmpty(roomName)) roomName = "Room";

        string path = Path.Combine(SavePath, $"{userId}_{roomName}.json");
        if (!File.Exists(path)) return null;

        string json = File.ReadAllText(path);
        return JsonUtility.FromJson<SaveData>(json);
    }

    // 모든 세이브 불러오기
    public static List<SaveData> LoadAll(string userId)
    {
        List<SaveData> saves = new();
        if (string.IsNullOrEmpty(userId)) userId = "Guest";

        if (!Directory.Exists(SavePath)) return saves;

        string[] files = Directory.GetFiles(SavePath, $"{userId}_*.json"); // 해당 유저 파일만
        foreach (var file in files)
        {
            string json = File.ReadAllText(file);
            SaveData data = JsonUtility.FromJson<SaveData>(json);
            saves.Add(data);
        }

        return saves;
    }

    // 방 이름 목록 반환 (로비에서 선택용)
    public static List<string> GetRoomNames(string userId)
    {
        List<string> roomNames = new();
        if (string.IsNullOrEmpty(userId)) userId = "Guest";
        if (!Directory.Exists(SavePath)) return roomNames;

        string[] files = Directory.GetFiles(SavePath, $"{userId}_*.json"); // 해당 유저 파일만
        foreach (var f in files)
        {
            string fileName = Path.GetFileNameWithoutExtension(f);
            // 접두사(userId_) 제거
            string roomName = fileName.Substring(userId.Length + 1);
            roomNames.Add(roomName);
        }
        return roomNames;
    }
}
