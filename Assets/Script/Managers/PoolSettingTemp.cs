using UnityEngine;
using System.Collections;

public class PoolSettingTemp : MonoBehaviour
{
    public GameObject itemPrefab;

    void Start()
    {
        PoolManager.Instance.CreatePool("Item", itemPrefab, 10);
        StartCoroutine(SpawnLoop());
    }

    void SpawnItem(Vector3 location)
    {
        GameObject item = PoolManager.Instance.GetFromPool("Item");
        item.transform.position = location;
        item.SetActive(true);
        Debug.Log($"Spawned Item at {location.x}, {location.y}, {location.z}.");
    }

    IEnumerator SpawnLoop()
    {
        Debug.Log("SpawnLoop Activated");
        while (true)
        {
            if (HasItemInPool("Item")) {
                Vector3 randomPos = new Vector3(Random.Range(-40f, 40f), 0f,Random.Range(-40f, 40f));
                SpawnItem(randomPos);
            }

            yield return new WaitForSeconds(1f);
        }
       // yield return null;
    }
    bool HasItemInPool(string key)
    {
        // 내부 풀에 오브젝트가 있는지 확인 (예외 처리 포함)
        return PoolManager.Instance != null
            && PoolManager.Instance.poolDictionary.ContainsKey(key)
            && PoolManager.Instance.poolDictionary[key].Count > 0;
    }
}
