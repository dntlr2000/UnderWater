using System.Collections.Generic;

[System.Serializable]
public class RoomData
{
    public string roomName;
    public string[] playerIds = new string[4];
    public int[] jobIndices = new int[4];

    public RoomData()
    {
        for (int i = 0; i < 4; i++)
        {
            playerIds[i] = null;
            jobIndices[i] = -1;
        }
    }

    public bool IsFull() => !System.Array.Exists(playerIds, id => id == null);

    public bool AddPlayer(string userId, int jobIndex, out int slotIndex)
    {
        slotIndex = -1;
        for (int i = 0; i < 4; i++)
        {
            if (playerIds[i] == null)
            {
                playerIds[i] = userId;
                jobIndices[i] = jobIndex;
                slotIndex = i;
                return true;
            }
        }
        return false;
    }

    public void RemovePlayer(string userId)
    {
        for (int i = 0; i < 4; i++)
        {
            if (playerIds[i] == userId)
            {
                playerIds[i] = null;
                jobIndices[i] = -1;
            }
        }
    }

    public void ResetJob(string userId)
    {
        for (int i = 0; i < 4; i++)
        {
            if (playerIds[i] == userId)
                jobIndices[i] = -1;
        }
    }

    public void LoadFromSaveData(SaveData data)
    {
        this.roomName = data.roomName;

        // SaveData에 저장된 jobAssignments 복사
        if (data.jobAssignments == null)
            data.jobAssignments = new Dictionary<string, int>();

        if (playerIds == null)
            playerIds = new string[2];  // 예: 4

        if (jobIndices == null)
            jobIndices = new int[2];

        foreach (var kvp in data.jobAssignments)
        {
            int slot = kvp.Value;
            if (slot >= 0 && slot < jobIndices.Length)
            {
                playerIds[slot] = kvp.Key;
                jobIndices[slot] = slot;
            }
        }
    }
}
