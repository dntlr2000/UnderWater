#if UNITY_EDITOR
using UnityEditor; // MenuItem 사용을 위해 필요
using UnityEngine;
using UnityEngine.SceneManagement;


public class FindMissingScripts : MonoBehaviour
{
    [MenuItem("Tools/Find Missing Scripts In Scene")]
    static void FindMissing()
    {
        int missingCount = 0;
        Scene scene = SceneManager.GetActiveScene();
        GameObject[] rootObjects = scene.GetRootGameObjects();

        foreach (GameObject root in rootObjects)
        {
            // 하위까지 모두 검사
            Transform[] children = root.GetComponentsInChildren<Transform>(true);
            foreach (Transform t in children)
            {
                GameObject go = t.gameObject;
                Component[] components = go.GetComponents<Component>();
                for (int i = 0; i < components.Length; i++)
                {
                    if (components[i] == null)
                    {
                        Debug.Log($"Missing script on: {GetFullPath(go)}", go);
                        missingCount++;
                    }
                }
            }
        }

        Debug.Log($"검색 완료! Missing Script 총 {missingCount}개 발견됨.");
    }

    static string GetFullPath(GameObject go)
    {
        return go.transform.parent == null
            ? go.name
            : GetFullPath(go.transform.parent.gameObject) + "/" + go.name;
    }
}
#endif