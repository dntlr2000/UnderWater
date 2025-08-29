using System.Collections.Generic;
using System.Threading;
using UnityEngine;


[System.Serializable]
public class MonsterPoolSetting
{
    public GameObject prefab; // 몬스터 프리팹
    public int count;         // 풀에 생성할 개수
}

public class MonsterManager : MonoBehaviour
{
    public static MonsterManager Instance { get; private set; }

    public List<MonsterPoolSetting> avoidMonsterSettings;
    public List<MonsterPoolSetting> attackMonsterSettings;

    private Dictionary<GameObject, Queue<GameObject>> monsterPools = new Dictionary<GameObject, Queue<GameObject>>();
    private List<Monster> activeMonsters = new List<Monster>();

    private void Awake()
    {
        if (Instance == null) 
            Instance = this;
        else 
            Destroy(gameObject);
    }

    private void Start()
    {
        InitializePools();
    }
    private void InitializePools()
    {
        monsterPools.Clear();

        foreach (var setting in avoidMonsterSettings)
            CreatePool(setting);

        foreach (var setting in attackMonsterSettings)
            CreatePool(setting);
    }

    /// <summary>
    /// 특정 몬스터 풀 생성
    /// </summary>
    private void CreatePool(MonsterPoolSetting setting)
    {
        if (setting.prefab == null || setting.count <= 0) return;

        Queue<GameObject> pool = new Queue<GameObject>();

        for (int i = 0; i < setting.count; i++)
        {
            GameObject obj = Instantiate(setting.prefab);
            obj.SetActive(false);
            pool.Enqueue(obj);
        }
        monsterPools[setting.prefab] = pool;
    }

    /// <summary>
    /// 몬스터 스폰
    /// </summary>
    public Monster SpawnMonster(GameObject prefab, Vector3 position)
    {
        GameObject obj;

        if (!monsterPools.ContainsKey(prefab) || monsterPools[prefab].Count == 0)
        {
            Debug.LogWarning($"풀에 {prefab.name}이 부족합니다.");
            obj = Instantiate(prefab);
        }
        else
        {
            obj = monsterPools[prefab].Dequeue();
        }

        return ActivateMonster(obj, position, prefab);
    }

    /// <summary>
    /// 몬스터 활성화
    /// </summary>
    private Monster ActivateMonster(GameObject obj, Vector3 position, GameObject prefab)
    {
        Debug.Log($"Activating {prefab.name} at {position}");
        obj.SetActive(true);
        obj.transform.position = position;

        Monster monster = obj.GetComponent<Monster>();
        if (monster != null)
        {
            monster.prefabReference = prefab;
            activeMonsters.Add(monster);
            Debug.Log($"Monster activated: {monster.name}, health={monster.health}");
        }
        return monster;
    }

    /// <summary>
    /// 몬스터 반납
    /// </summary>
    public void ReturnMonster(GameObject prefab, Monster monster)
    {
        if (monster == null) return;
/*        Debug.Log($"[Return] {monster.name} 반환됨 / prefab={(prefab != null ? prefab.name : "null")}");*/
        monster.gameObject.SetActive(false);
        activeMonsters.Remove(monster);

        if (prefab == null)
        {
/*            Debug.LogError($"{monster.name} 의 prefabReference가 null! (반납 실패)");*/
            return;
        }

        if (!monsterPools.ContainsKey(prefab))
            monsterPools[prefab] = new Queue<GameObject>();

        monsterPools[prefab].Enqueue(monster.gameObject);
    }

    /// <summary>
    /// 현재 활성화된 모든 몬스터 반환
    /// </summary>
    public void ClearActiveMonsters()
    {
        foreach (var monster in new List<Monster>(activeMonsters))
        {
            ReturnMonster(monster.prefabReference, monster);
        }
    }
}
