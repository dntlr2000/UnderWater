using System;
using UnityEngine;

/// <summary>
/// UI에서 플레이어 슬롯을 채우기 위한 간단한 DTO
/// </summary>
[Serializable]
public class PlayerInfo
{
    public string UserId;
    public string Nickname;
    public string JobName;
    public Sprite JobIcon;

    public PlayerInfo() { }

    public PlayerInfo(string userId, string nickname, string jobName, Sprite jobIcon = null)
    {
        UserId = userId;
        Nickname = nickname;
        JobName = jobName;
        JobIcon = jobIcon;
    }
}
