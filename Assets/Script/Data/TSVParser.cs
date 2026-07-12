using System.Collections.Generic;
using UnityEngine;

public static class TSVParser
{
    public static List<Dictionary<string, string>> Parse(string tsvText)
    {
        var result = new List<Dictionary<string, string>>();
        if (string.IsNullOrEmpty(tsvText)) return result;

        var lines = tsvText.Split('\n');
        if (lines.Length < 2) return result;

        var headers = lines[0].TrimEnd('\r').Split('\t');

        for (int i = 1; i < lines.Length; i++)
        {
            var line = lines[i].TrimEnd('\r');
            if (string.IsNullOrWhiteSpace(line)) continue;

            var values = line.Split('\t');
            var row = new Dictionary<string, string>();

            for (int j = 0; j < headers.Length; j++)
                row[headers[j]] = j < values.Length ? values[j].Trim() : string.Empty;

            result.Add(row);
        }

        return result;
    }

    public static string Get(Dictionary<string, string> row, string key)
    {
        return row.TryGetValue(key, out var val) ? val : string.Empty;
    }

    public static int GetInt(Dictionary<string, string> row, string key, int fallback = 0)
    {
        return int.TryParse(Get(row, key), out var v) ? v : fallback;
    }

    public static float GetFloat(Dictionary<string, string> row, string key, float fallback = 0f)
    {
        return float.TryParse(Get(row, key), out var v) ? v : fallback;
    }

    public static bool GetBool(Dictionary<string, string> row, string key)
    {
        return Get(row, key).Equals("TRUE", System.StringComparison.OrdinalIgnoreCase);
    }

    public static T GetEnum<T>(Dictionary<string, string> row, string key, T fallback = default) where T : struct
    {
        return System.Enum.TryParse<T>(Get(row, key), true, out var v) ? v : fallback;
    }

    public static TextAsset LoadTSV(string resourcePath)
    {
        var asset = Resources.Load<TextAsset>(resourcePath);
        if (asset == null)
            Debug.LogError($"[TSVParser] TSV ¾øÀ½: Resources/{resourcePath}");
        return asset;
    }
}