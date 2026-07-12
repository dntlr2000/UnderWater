using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "StringTable", menuName = "SunkenCity/Database/StringTable")]
public class StringTable : ScriptableObject
{
    public enum Language { KO, EN }

    private Dictionary<string, string> _map;

    public void Build(List<Dictionary<string, string>> rows, Language lang)
    {
        string col = lang.ToString();
        _map = new Dictionary<string, string>();
        foreach (var row in rows)
        {
            var key = TSVParser.Get(row, "stringKey");
            if (!string.IsNullOrEmpty(key))
                _map[key] = TSVParser.Get(row, col);
        }
    }

    public string Get(string key, string fallback = "")
    {
        if (_map == null) return fallback;
        return _map.TryGetValue(key, out var val) ? val : fallback;
    }
}