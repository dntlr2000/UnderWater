using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "QuestDatabase", menuName = "SunkenCity/Database/Quest")]
public class QuestDatabase : ScriptableObject
{
    private Dictionary<string, QuestRuntimeData> _map;

    public void Build(List<QuestRuntimeData> list)
    {
        _map = list.ToDictionary(q => q.questID);
    }

    public QuestRuntimeData Get(string id)
    {
        if (_map == null) { Debug.LogError("[QuestDatabase] 초기화 전 접근"); return null; }
        return _map.TryGetValue(id, out var q) ? q : null;
    }

    public IEnumerable<QuestRuntimeData> GetAll() => _map?.Values ?? Enumerable.Empty<QuestRuntimeData>();
}