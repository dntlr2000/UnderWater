using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System;

public static class SaveSystem
{
    private static string SavePath => Path.Combine(Application.persistentDataPath, "Saves");

    [Serializable]
    private class SaveWrapper
    {
        public string saveId;
        public int dayCount;
        public List<JobAssignment> jobs = new();
    }

    [Serializable]
    private class JobAssignment
    {
        public string playerId;
        public int jobIndex;
    }

    // 저장하기
    public static void Save(SaveData data)
    {
        if (!Directory.Exists(SavePath))
            Directory.CreateDirectory(SavePath);

        SaveWrapper wrapper = new SaveWrapper
        {
            saveId = data.saveId,
            dayCount = data.dayCount
        };

        if (data.jobAssignments != null)
        {
            foreach (var kv in data.jobAssignments)
            {
                wrapper.jobs.Add(new JobAssignment { playerId = kv.Key, jobIndex = kv.Value });
            }
        }

        string json = JsonUtility.ToJson(wrapper, true);
        File.WriteAllText(Path.Combine(SavePath, data.saveId + ".json"), json);
    }

    // 불러오기
    public static SaveData Load(string saveId)
    {
        string file = Path.Combine(SavePath, saveId + ".json");
        if (!File.Exists(file)) return null;

        string json = File.ReadAllText(file);
        SaveWrapper wrapper = JsonUtility.FromJson<SaveWrapper>(json);

        SaveData data = new SaveData
        {
            saveId = wrapper.saveId,
            dayCount = wrapper.dayCount,
            jobAssignments = new Dictionary<string, int>()
        };

        foreach (var job in wrapper.jobs)
        {
            data.jobAssignments[job.playerId] = job.jobIndex;
        }

        return data;
    }

    // 저장된 모든 파일 불러오기
    public static List<SaveData> LoadAll()
    {
        List<SaveData> saves = new List<SaveData>();

        if (!Directory.Exists(SavePath))
            return saves;

        string[] files = Directory.GetFiles(SavePath, "*.json");
        foreach (var file in files)
        {
            string json = File.ReadAllText(file);
            SaveWrapper wrapper = JsonUtility.FromJson<SaveWrapper>(json);

            SaveData data = new SaveData
            {
                saveId = wrapper.saveId,
                dayCount = wrapper.dayCount,
                jobAssignments = new Dictionary<string, int>()
            };

            foreach (var job in wrapper.jobs)
            {
                data.jobAssignments[job.playerId] = job.jobIndex;
            }

            saves.Add(data);
        }

        return saves;
    }
}
