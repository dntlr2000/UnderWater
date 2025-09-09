using UnityEngine;
using Photon.Pun;

public enum MonsterBehaviorType
{
    AvoidPlayer,
    AttackPlayer
}

[RequireComponent(typeof(Rigidbody))] //Rigidbody를 무조건 가지도록함
[DisallowMultipleComponent] //monster스크립트가 단 하나만 붙게 함
public class Monster : Character
{
    [Header("추적 대상")]
    public Transform target;
    public float stopDistance = 1.5f;

    [Header("행동 타입")]
    public MonsterBehaviorType behaviorType = MonsterBehaviorType.AvoidPlayer;
    public float avoidDistance = 5f; //피할 거리(AvoidPlayer일때)
    public float attackRange = 1f;
    public float attackCooldown = 1f;

    [Header("물속 영역 제한")]
    public Transform waterAreaCenter;
    public float waterAreaRadius = 10f;

    [Header("아이템 드랍")]
    public GameObject dropItemPrefab;

    [Header("탐지 설정")]
    [Tooltip("플레이어를 최초 인식하는 범위(트리거 반경).")]
    public float detectionRadius = 8f;
    [Tooltip("타겟이 이 거리 밖으로 나가면 타겟 해제.")]
    public float loseTargetDistance = 4f; //몬스터가 플레이어를 인식하는 범위
    [Tooltip("탐지에 사용할 레이어")]
    public LayerMask detectionLayers;

    [Header("회전 보정")]
    [Tooltip("모델의 정면이 +Z가 아니라면 Yaw 보정(도). 예: 머리가 +X면 -90")]
    public float yawOffset = 0f;

    [Header("네트워크")]
    [Tooltip("호스트/마스터에서만 AI 실행")]
    public bool runOnlyOnMaster = false;

    [HideInInspector]
    public GameObject prefabReference;

    private Rigidbody mrb;
    private string poolkey;
    private float lastAttackTime;
    private float waterDamagePerSecond = 10f;

    private Vector3 randomDir;
    private float changeDirInterval = 1f;
    private float lastDirChangeTime;

    private SphereCollider detectionTrigger;

    public void Init(GameObject prefab) => prefabReference = prefab;

    private void Awake()
    {
        mrb = GetComponent<Rigidbody>();
        mrb.interpolation = RigidbodyInterpolation.Interpolate; // 이동 부드럽게
        mrb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ; // 수평 유지

        EnsureDetectionTrigger();
    }

    public void Initialize(string poolkey)
    {
        this.poolkey = poolkey;
        health = 100; // 초기 체력
        moveSpeed = 3f;
        atkPower = 10f;
    }
    private void EnsureDetectionTrigger()
    {
        detectionTrigger = GetComponent<SphereCollider>();
        if (detectionTrigger == null)
            detectionTrigger = gameObject.AddComponent<SphereCollider>();

        detectionTrigger.isTrigger = true;
        detectionTrigger.radius = detectionRadius;
    }

    private bool IsInWater()
    {
        // 몬스터 위치에 작은 Sphere를 체크해서 Water 태그 가진 콜라이더가 있는지 확인
        Collider[] hits = Physics.OverlapSphere(transform.position, 0.3f);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Water"))
                return true;
        }
        return false;
    }

    private void OnEnable()
    {
        // 풀에서 꺼낼 때 초기화
        target = null;
        lastAttackTime = -attackCooldown;
        health = 100;
    }

    private void FixedUpdate()
    {
        if (runOnlyOnMaster && (!PhotonNetwork.IsMasterClient)) return;

        if (health <= 0) return;

        if (!IsInWater())
        {
            TakeDamage(waterDamagePerSecond * Time.fixedDeltaTime);

            //물 안으로 복귀 시도
            MoveTowardsWater();
            return;
        }

        Vector3 flatPos = new Vector3(transform.position.x, 0, transform.position.z);

        if (target != null)
        {
            Vector3 flatTargetPos = new Vector3(target.position.x, 0, target.position.z);
            float dist = Vector3.Distance(transform.position, target.position);
            if (dist > loseTargetDistance)
            {
                ClearTarget();
            }
        }

        if (target == null)
        {
            // 자유 수영
            RandomSwim();
        }
        else
        {
            Vector3 direction = (target.position - transform.position);
            direction.y = 0;
            float distance = direction.magnitude;

            switch (behaviorType)
            {
                case MonsterBehaviorType.AvoidPlayer:
                    Move(-direction.normalized);
                    break;

                case MonsterBehaviorType.AttackPlayer:
                    if (distance > attackRange)
                        Move(direction.normalized);
                    else
                        Attack();
                    break;
            }
        }
    }

    private void RandomSwim()
    {
        if (Time.time - lastDirChangeTime > changeDirInterval)
        {
            randomDir = new Vector3(
            Random.Range(-1f, 1f),
            Random.Range(-0.3f, 0.3f),
            Random.Range(-1f, 1f)
            ).normalized;

            lastDirChangeTime = Time.time;
        }
        Move(randomDir);
    }


    private void Move(Vector3 direction)
    {
        if (direction.sqrMagnitude < 0.001f) return;

        direction.y = 0;
        direction.Normalize();

        Vector3 moveVector = direction * moveSpeed * Time.fixedDeltaTime;
        mrb.MovePosition(transform.position + moveVector);

        // 바라보는 방향 회전
        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion lookRot = Quaternion.LookRotation(direction, Vector3.up);
            if (Mathf.Abs(yawOffset) > 0.01f)
                lookRot *= Quaternion.Euler(0f, yawOffset, 0f);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, 10f * Time.fixedDeltaTime);
        }
        //Debug.Log("몬스터 이동중");
    }

    private void MoveTowardsWater()
    {
        if (waterAreaCenter == null)
            return;

        Vector3 dirToWater = (waterAreaCenter.position - transform.position);
        Move(dirToWater.normalized);
    }

    public override void Attack()
    {
        if (Time.time - lastAttackTime < attackCooldown) return;

        lastAttackTime = Time.time;

        base.Attack();

        if (target != null)
        {
            var player = target.GetComponent<Player>();
            if (player != null)
            {
                Debug.Log("플레이어를 공격했습니다. 체력이 깎입니다.");
                // player.TakeDamage(atkPower);
            }
        }
    }

    public override void TakeDamage(float damage)
    {
        base.TakeDamage(damage);
    }

    protected override void Death()
    {
        Debug.Log($"{gameObject.name} 몬스터 사망");
        if (prefabReference == null)
        {
            Debug.LogError($"[Monster] {name} 의 prefabReference가 할당되지 않았습니다! 풀로 반납 불가");
            return;
        }

        if (dropItemPrefab != null)
        {
            Instantiate(dropItemPrefab, transform.position, Quaternion.identity);
        }

        // 오브젝트 풀 사용 시
        if (MonsterManager.Instance != null && prefabReference != null)
        {
            MonsterManager.Instance.ReturnMonster(prefabReference, this);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void SetTarget(Transform t)
    {
        if (target == null)
        {
            target = t;
            Debug.Log($"[Monster] Target acquired: {t.name}");
        }
        // 이미 누군가 타겟이면 무시
    }

    private void ClearTarget()
    {
        Debug.Log("[Monster] Target lost");
        target = null;
    }

    private void OnTriggerEnter(Collider other)
    {
        //Debug.Log($"[Monster Trigger] {other.name}, layer={other.gameObject.layer}, tag={other.tag}");

        if (detectionLayers.value != 0 && ((1 << other.gameObject.layer) & detectionLayers) == 0)
            return;

        if (target == null && other.CompareTag("Player"))
        {
            SetTarget(other.transform);
        }
    }

    private void OnTriggerExit(Collider other)
    {

    }

#if UNITY_EDITOR   
    private void OnValidate()
    {
        if (loseTargetDistance < detectionRadius)
            loseTargetDistance = detectionRadius; // 기본적으로 같거나 크게
        if (detectionTrigger != null)
            detectionTrigger.radius = detectionRadius;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, loseTargetDistance);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
#endif
}
