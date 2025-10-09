using UnityEngine;

public class MonsterSpawner : MonoBehaviour
{
    private void Start()
    {
        // 회피형 몬스터 0번 프리팹 10마리 소환
        SpawnMultiple(MonsterManager.Instance.avoidMonsterSettings[0].prefab, 10, new Vector3(0, -0, 0));

        // 회피형 몬스터 1번 프리팹 10마리 소환
        SpawnMultiple(MonsterManager.Instance.avoidMonsterSettings[1].prefab, 10, new Vector3(5, -0, 0));

        // 공격형 몬스터 0번 프리팹 2마리 소환
        SpawnMultiple(MonsterManager.Instance.attackMonsterSettings[0].prefab, 2, new Vector3(10, 0, 0));
    }

    // 여러 마리 소환 함수
    void SpawnMultiple(GameObject prefab, int count, Vector3 basePosition)
    {
        for (int i = 0; i < count; i++)
        {
            // 살짝 위치를 달리해서 겹치지 않게 함 (원하는 방식으로 수정 가능)
            Vector3 spawnPos = basePosition + new Vector3(i * 1.5f, 0, 0);
            MonsterManager.Instance.SpawnMonsterPool(prefab, spawnPos);
        }
    }
}
