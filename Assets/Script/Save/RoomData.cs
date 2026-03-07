using System.Collections.Generic;
using System.Linq;
using Photon.Pun;

[System.Serializable]
public class RoomData
{
    public string roomName;
    public string[] playerIds;
    public int[] jobIndices;
    public int maxPlayers; // 방 최대 인원

    public RoomData(int maxPlayers = 4)
    {
        this.maxPlayers = maxPlayers;
        playerIds = Enumerable.Repeat("", maxPlayers).ToArray();
        jobIndices = Enumerable.Repeat(-1, maxPlayers).ToArray();
    }

    public bool IsFull()
    {
        return playerIds.Count(p => !string.IsNullOrEmpty(p)) >= maxPlayers;
    }

    public bool AddPlayer(string userId, int jobIndex, out int slotIndex)
    {
        slotIndex = -1;
        for (int i = 0; i < maxPlayers; i++)
        {
            if (string.IsNullOrEmpty(playerIds[i]))
            {
                playerIds[i] = userId;
                jobIndices[i] = jobIndex;
                slotIndex = i;
                SaveToRoomProperties();
                return true;
            }
        }
        return false;
    }

    public void RemovePlayer(string userId)
    {
        for (int i = 0; i < maxPlayers; i++)
        {
            if (playerIds[i] == userId)
            {
                playerIds[i] = "";
                jobIndices[i] = -1;
            }
        }
        SaveToRoomProperties();
    }

    public void ResetJob(string userId)
    {
        for (int i = 0; i < maxPlayers; i++)
        {
            if (playerIds[i] == userId)
                jobIndices[i] = -1;
        }
        SaveToRoomProperties();
    }

    public void LoadFromSaveData(SaveData data)
    {
        this.roomName = data.roomName;
        if (data.jobAssignments == null)
            data.jobAssignments = new Dictionary<string, int>();

        playerIds = new string[maxPlayers];
        jobIndices = new int[maxPlayers];

        for (int i = 0; i < maxPlayers; i++)
        {
            playerIds[i] = null;
            jobIndices[i] = -1;
        }

        int index = 0;
        foreach (var kvp in data.jobAssignments)
        {
            if (index < maxPlayers)
            {
                playerIds[index] = kvp.Key;
                jobIndices[index] = kvp.Value;
                index++;
            }
        }

        SaveToRoomProperties();
    }

    public void SaveToRoomProperties()
    {
        if (PhotonNetwork.CurrentRoom == null) return;

        string[] safePlayerIds = playerIds.Select(p => p ?? "").ToArray();
        int[] safeJobIndices = jobIndices.ToArray();

        var props = new ExitGames.Client.Photon.Hashtable
        {
            ["playerIds"] = safePlayerIds,
            ["jobIndices"] = safeJobIndices
        };

        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
    }

    public void LoadFromRoomProperties()
    {
        if (PhotonNetwork.CurrentRoom == null) return;

        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("playerIds", out object ids))
        {
            string[] loaded = (string[])ids;
            for (int i = 0; i < loaded.Length && i < playerIds.Length; i++)
                playerIds[i] = string.IsNullOrEmpty(loaded[i]) ? null : loaded[i];
        }

        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("jobIndices", out object jobs))
            jobIndices = ((int[])jobs).ToArray(); 
    }
}
