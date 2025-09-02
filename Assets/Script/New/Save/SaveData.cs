using System;
using System.Collections.Generic;

[Serializable]
public class SaveData
{
    public string saveId; // 고유 ID
    public int dayCount;  // 예: 게임 진행 시간/일수
    public Dictionary<string, int> jobAssignments; // NickName 또는 고유 ID → JobIndex
}
