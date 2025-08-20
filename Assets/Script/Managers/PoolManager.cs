using System.Collections.Generic;
using UnityEngine;

public class PoolManager : MonoBehaviour
{
    public static PoolManager Instance;

    public Dictionary<string, Queue<GameObject>> poolDictionary = new Dictionary<string, Queue<GameObject>>();
    public Dictionary<string, GameObject> prefabDictionary = new Dictionary<string, GameObject>();

    void Awake()
    {
        Instance = this;
    }

    public void CreatePool(string key, GameObject prefab, int initialSize)
    {
        prefabDictionary[key] = prefab;
        poolDictionary[key] = new Queue<GameObject>();

        for (int i = 0; i < initialSize; i++)
        {
            GameObject obj = Instantiate(prefab);
            obj.SetActive(false);
            poolDictionary[key].Enqueue(obj);
        }

/*        Debug.Log($"Created Pool, Name = {key}, Size = {poolDictionary[key].Count}");*/
    }

    public GameObject GetFromPool(string key)
    {
        if (!poolDictionary.ContainsKey(key))
        {
            Debug.LogError($"Pool {key} doesn't exist!");
            return null;
        }

        /*
        GameObject obj = poolDictionary[key].Count > 0
            ? poolDictionary[key].Dequeue()
            : Instantiate(prefabDictionary[key]);
        */
        GameObject obj;
        if (poolDictionary[key].Count > 0)
        {
            obj = poolDictionary[key].Dequeue();
            //Debug.Log($"Dequed Sucessfully, ID : {obj.GetInstanceID()}");
        }
        else
        {
            obj = Instantiate(prefabDictionary[key]);
            //Debug.Log("Instantiate Object");
        }
        

        obj.SetActive(true);
        //Debug.Log($"Object left in Pool: {poolDictionary[key].Count}");
        return obj;
    }

    public void ReturnToPool(string key, GameObject obj)
    {
        obj.SetActive(false);
        poolDictionary[key].Enqueue(obj);
    }


}
