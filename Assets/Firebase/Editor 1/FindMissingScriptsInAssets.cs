#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

public class FindMissingScriptsInAssets
{
    [MenuItem("Tools/Find Missing Scripts In Prefabs")]
    static void FindMissingScripts()
    {
        string[] allPrefabs = Directory.GetFiles("Assets", "*.prefab", SearchOption.AllDirectories);
        int missingCount = 0;

        foreach (string path in allPrefabs)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Component[] components = prefab.GetComponentsInChildren<Component>(true);

            foreach (Component c in components)
            {
                if (c == null)
                {
                    Debug.Log($" Missing script in prefab: {path}", prefab);
                    missingCount++;
                    break;
                }
            }
        }

        Debug.Log($" ÃÑ {missingCount}°³ÀÇ ÇÁ¸®ÆÕ¿¡¼­ Missing Script°¡ ¹ß°ßµÊ.");
    }
}
#endif