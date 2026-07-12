using UnityEngine;

public class TSVTestRunner : MonoBehaviour
{
    void Start()
    {
        // 방법 1 베이스 타입으로 전체 로드
        var allItems = Resources.LoadAll<ItemData>("Data/ItemData");
        Debug.Log($"[Test] Resources.LoadAll<ItemData> 결과: {allItems.Length}개");
        foreach (var item in allItems)
            Debug.Log($"  - {item.itemId} : {item.itemName} ({item.GetType().Name})");

        // 방법 2 ScriptableObject로 더 넓게 로드
        var allSO = Resources.LoadAll<ScriptableObject>("Data/ItemData");
        Debug.Log($"[Test] Resources.LoadAll<ScriptableObject> 결과: {allSO.Length}개");
        foreach (var so in allSO)
            Debug.Log($"  - {so.name} ({so.GetType().Name})");
    }
}