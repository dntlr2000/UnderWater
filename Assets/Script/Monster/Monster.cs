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

    [Header("수중 이동")]
    [Tooltip("초당 최대 수직 이동 거리. 수평 속도는 moveSpeed를 사용합니다.")]
    [SerializeField, Min(0f)] private float verticalMoveSpeed = 0.6f;
    [Tooltip("몬스터 중심에서 수면·물 영역 경계·장애물까지 확보할 여유 거리입니다.")]
    [SerializeField, Min(0.05f)] private float swimBoundaryPadding = 0.3f;
    [Tooltip("수영 경로를 막는 지형·장애물 레이어입니다. 트리거와 자신의 콜라이더는 제외합니다.")]
    [SerializeField] private LayerMask swimmingObstacleLayers = Physics.DefaultRaycastLayers;

    [Header("물속 영역 제한")]
    public Transform waterAreaCenter;
    public float waterAreaRadius = 10f;

    [Header("아이템 드랍")]
    //public GameObject dropItemPrefab;
    public int dropItemID = -1;

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
    private Collider waterVolume;
    private Collider[] waterHits = new Collider[16];
    private RaycastHit[] obstacleHits = new RaycastHit[16];
    private Vector3 lastWaterPosition;
    private bool hasLastWaterPosition;

    public void Init(GameObject prefab) => prefabReference = prefab;


    protected override void Awake()
    {
        base.Awake();
        mrb = GetComponent<Rigidbody>();
        mrb.interpolation = RigidbodyInterpolation.Interpolate; // 이동 부드럽게
        mrb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ; // 수평 유지

        EnsureDetectionTrigger();
    }

    // 풀의 기본 상태를 초기화하되 이동 속도와 공격력은 각 프리팹의 설정을 유지합니다.
    public void Initialize(string poolkey)
    {
        this.poolkey = poolkey;
        health = 100; // 초기 체력
    }
    private void EnsureDetectionTrigger()
    {
        detectionTrigger = GetComponent<SphereCollider>();
        if (detectionTrigger == null)
            detectionTrigger = gameObject.AddComponent<SphereCollider>();

        detectionTrigger.isTrigger = true;
        detectionTrigger.radius = detectionRadius;
    }

    // 물 영역의 실제 내부에 있는지 검사하고 복귀에 사용할 콜라이더를 기억합니다.
    private bool IsInWater() => TryFindWaterVolume(mrb.position, 0f);

    // 경계 여유를 포함해 수영 가능한 물 영역을 찾고, 겹친 콜라이더가 많으면 버퍼를 확장합니다.
    private bool TryFindWaterVolume(Vector3 position, float padding)
    {
        if (ContainsSwimmingPosition(waterVolume, position, padding)) return true;

        int count;
        do
        {
            count = Physics.OverlapSphereNonAlloc(position, 0.3f, waterHits,
                Physics.AllLayers, QueryTriggerInteraction.Collide);
            if (count < waterHits.Length) break;
            System.Array.Resize(ref waterHits, waterHits.Length * 2);
        } while (true);

        for (int i = 0; i < count; i++)
        {
            if (!ContainsSwimmingPosition(waterHits[i], position, padding)) continue;
            waterVolume = waterHits[i];
            return true;
        }
        return false;
    }

    // 수면과 측면·바닥에 여유를 두며, 회전된 물 영역도 실제 콜라이더 형상으로 검사합니다.
    private bool ContainsSwimmingPosition(Collider volume, Vector3 position, float padding)
    {
        if (volume == null || !volume.enabled || !volume.gameObject.activeInHierarchy
            || !volume.CompareTag("Water") || !ContainsWaterPoint(volume, position)) return false;
        if (padding <= 0f) return true;

        return ContainsWaterPoint(volume, position + Vector3.up * padding)
            && ContainsWaterPoint(volume, position + Vector3.down * padding)
            && ContainsWaterPoint(volume, position + Vector3.right * padding)
            && ContainsWaterPoint(volume, position + Vector3.left * padding)
            && ContainsWaterPoint(volume, position + Vector3.forward * padding)
            && ContainsWaterPoint(volume, position + Vector3.back * padding);
    }

    // ClosestPoint가 입력 위치를 그대로 반환하면 해당 지점은 물 콜라이더 내부입니다.
    private bool ContainsWaterPoint(Collider volume, Vector3 position)
    {
        return (volume.ClosestPoint(position) - position).sqrMagnitude < 0.000001f;
    }

    // 풀에서 다시 꺼낼 때 이전 목표·수영 방향·복귀 위치를 지워 새 위치에서 탐색합니다.
    private void OnEnable()
    {
        // 풀에서 꺼낼 때 초기화
        target = null;
        lastAttackTime = -attackCooldown;
        health = 100;
        randomDir = Vector3.zero;
        lastDirChangeTime = float.NegativeInfinity;
        waterVolume = null;
        hasLastWaterPosition = false;
    }

    // 방장만 수중 경계를 확인한 뒤 높이 차이를 포함해 추적·회피하고 사거리 안에서 공격합니다.
    private void FixedUpdate()
    {
        if (PhotonNetwork.InRoom && runOnlyOnMaster && !PhotonNetwork.IsMasterClient) return;

        if (health <= 0) return;

        if (!IsInWater())
        {
            RequestForTakeDamage(waterDamagePerSecond * Time.fixedDeltaTime, false);

            //물 안으로 복귀 시도
            MoveTowardsWater();
            return;
        }

        // 경계 가까이에서 생성되거나 밀려난 경우 먼저 안전한 수중 위치로 돌아갑니다.
        if (!TryFindWaterVolume(mrb.position, swimBoundaryPadding))
        {
            MoveTowardsWater();
            return;
        }
        lastWaterPosition = mrb.position;
        hasLastWaterPosition = true;

        if (target != null)
        {
            Player targetPlayer = target.GetComponentInParent<Player>();
            if (!CanTargetPlayer(targetPlayer)
                || Vector3.Distance(transform.position, targetPlayer.transform.position) > loseTargetDistance)
            {
                ClearTarget();
            }
            else
            {
                target = targetPlayer.transform;
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

    // 자유 수영은 작은 수직 성분을 섞고 공통 이동 경로의 속도·경계 제한을 적용합니다.
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


    // 수평·수직 속도를 나누고 경계와 장애물을 확인한 후 몸체는 수평으로 회전시킵니다.
    private void Move(Vector3 direction, bool returningToWater = false)
    {
        if (direction.sqrMagnitude < 0.00000001f) return;

        Vector3 swimDirection = direction.normalized;
        Vector3 step = new Vector3(swimDirection.x * moveSpeed,
            swimDirection.y * verticalMoveSpeed, swimDirection.z * moveSpeed) * Time.fixedDeltaTime;

        if (returningToWater)
        {
            // 복귀 지점을 지나쳐 경계 안팎으로 진동하지 않도록 축별 남은 거리로 제한합니다.
            step.x = Mathf.Clamp(step.x, -Mathf.Abs(direction.x), Mathf.Abs(direction.x));
            step.y = Mathf.Clamp(step.y, -Mathf.Abs(direction.y), Mathf.Abs(direction.y));
            step.z = Mathf.Clamp(step.z, -Mathf.Abs(direction.z), Mathf.Abs(direction.z));
        }
        else
        {
            step = ConstrainStepToWater(step);
        }
        step = ConstrainStepToObstacles(step);
        mrb.MovePosition(mrb.position + step);

        Vector3 horizontalFacing = new Vector3(step.x, 0f, step.z);
        if (horizontalFacing.sqrMagnitude > 0.000001f)
        {
            Quaternion lookRot = Quaternion.LookRotation(horizontalFacing, Vector3.up);
            if (Mathf.Abs(yawOffset) > 0.01f)
                lookRot *= Quaternion.Euler(0f, yawOffset, 0f);
            mrb.MoveRotation(Quaternion.Slerp(mrb.rotation, lookRot, 10f * Time.fixedDeltaTime));
        }
    }

    // 물 경계를 넘는 성분만 차단하고 자유 수영 방향을 반전해 경계에 계속 머물지 않게 합니다.
    private Vector3 ConstrainStepToWater(Vector3 step)
    {
        Vector3 position = mrb.position;
        if (TryFindWaterVolume(position + step, swimBoundaryPadding)) return step;

        Vector3 horizontalStep = new Vector3(step.x, 0f, step.z);
        if (TryFindWaterVolume(position + horizontalStep, swimBoundaryPadding))
        {
            if (target == null) randomDir.y = -randomDir.y;
            return horizontalStep;
        }

        if (target == null) randomDir = -randomDir;
        Vector3 verticalStep = new Vector3(0f, step.y, 0f);
        return TryFindWaterVolume(position + verticalStep, swimBoundaryPadding) ? verticalStep : Vector3.zero;
    }

    // 트리거형 몬스터도 해저·벽을 통과하지 않도록 이동 구간에서 가장 가까운 장애물 앞에 멈춥니다.
    private Vector3 ConstrainStepToObstacles(Vector3 step)
    {
        float distance = step.magnitude;
        if (distance < 0.00001f) return Vector3.zero;

        int count;
        do
        {
            count = Physics.SphereCastNonAlloc(mrb.position, swimBoundaryPadding, step / distance,
                obstacleHits, distance, swimmingObstacleLayers, QueryTriggerInteraction.Ignore);
            if (count < obstacleHits.Length) break;
            System.Array.Resize(ref obstacleHits, obstacleHits.Length * 2);
        } while (true);

        float allowedDistance = distance;
        for (int i = 0; i < count; i++)
        {
            Collider obstacle = obstacleHits[i].collider;
            if (obstacle.attachedRigidbody == mrb || obstacle.transform.IsChildOf(transform)
                || obstacle.CompareTag("Water")) continue;
            allowedDistance = Mathf.Min(allowedDistance, Mathf.Max(0f, obstacleHits[i].distance - 0.01f));
        }
        if (allowedDistance < distance && target == null) randomDir = -randomDir;
        return step * (allowedDistance / distance);
    }

    // 마지막 정상 수중 위치를 우선 사용하고, 없으면 지정 중심 또는 실제 물 영역 중심으로 복귀합니다.
    private void MoveTowardsWater()
    {
        Vector3 destination;
        if (hasLastWaterPosition && TryFindWaterVolume(lastWaterPosition, swimBoundaryPadding))
            destination = lastWaterPosition;
        else if (waterAreaCenter != null && TryFindWaterVolume(waterAreaCenter.position, swimBoundaryPadding))
            destination = waterAreaCenter.position;
        else if (waterVolume != null && ContainsSwimmingPosition(waterVolume, waterVolume.bounds.center, swimBoundaryPadding))
            destination = waterVolume.bounds.center;
        else
            return;

        Move(destination - mrb.position, true);
    }

    // 공격 조건과 간격을 검증한 뒤 대상 소유자에게 고유한 공격을 한 번 전달합니다.
    public override void Attack()
    {
        if (!isActiveAndEnabled || health <= 0f || behaviorType != MonsterBehaviorType.AttackPlayer
            || (PhotonNetwork.InRoom && !PhotonNetwork.IsMasterClient)) return;

        Player targetPlayer = target != null ? target.GetComponentInParent<Player>() : null;
        if (!CanTargetPlayer(targetPlayer))
        {
            target = null;
            return;
        }

        if (float.IsNaN(attackRange) || float.IsInfinity(attackRange) || attackRange <= 0f
            || Vector3.Distance(transform.position, targetPlayer.transform.position) > attackRange
            || float.IsNaN(attackCooldown) || float.IsInfinity(attackCooldown)
            || Time.time - lastAttackTime < Mathf.Max(0f, attackCooldown)) return;

        string attackId = System.Guid.NewGuid().ToString("N");
        if (!targetPlayer.condition.RequestMonsterDamage(atkPower, attackId)) return;

        lastAttackTime = Time.time;
        base.Attack();
    }

    // 비활성·체력 소진·빈사 진입 중인 플레이어는 새 공격 대상으로 선택하지 않습니다.
    private bool CanTargetPlayer(Player targetPlayer)
    {
        return targetPlayer != null && targetPlayer.isActiveAndEnabled
            && targetPlayer.condition != null && targetPlayer.condition.isActiveAndEnabled
            && targetPlayer.condition.CanAct(false, true, false);
    }

    public override void Interact() //상호작용
    {
        if (GetInteractionType() == InteractionType.Instant)
        {
            if (Input.GetMouseButtonDown(0)) //좌클
            {
                //if (player.condition.GetIsBusy()) return;
                RequestForTakeDamage(GetDamageValueFromInventory());
            }
        }

    }

    protected override void Death()
    {
        Debug.Log($"{gameObject.name} 몬스터 사망");
        if (prefabReference == null)
        {
            Debug.LogError($"[Monster] {name} 의 prefabReference가 할당되지 않았습니다! 풀로 반납 불가");
            return;
        }

        //if (dropItemPrefab != null)
        if (dropItemID != -1)
        {
            //Instantiate(dropItemPrefab, transform.position, Quaternion.identity);
            ItemDatabase.Instance.GenerateItemPhoton(dropItemID, 3, transform.position);
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

    // 자식 콜라이더에 닿아도 플레이어 루트의 태그·레이어로 탐지 대상을 판단합니다.
    private void OnTriggerEnter(Collider other)
    {
        Player detectedPlayer = other.GetComponentInParent<Player>();
        if (!CanTargetPlayer(detectedPlayer) || !detectedPlayer.CompareTag("Player")) return;

        if (detectionLayers.value != 0 && ((1 << detectedPlayer.gameObject.layer) & detectionLayers) == 0)
            return;

        if (target == null)
        {
            SetTarget(detectedPlayer.transform);
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
    public override InteractionType GetInteractionType() => InteractionType.Instant;


    [PunRPC]
    public void PunRPC_Master_InstantiateDroppedItem(int itemID, int amount, Vector3 location)
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            return;
        }

        string prefabPath = $"FieldItem/Object{itemID}";
        if (Resources.Load(prefabPath) == null)
        {
            prefabPath = "FieldItem/Object1";
        }
        GameObject droppedItem = PhotonNetwork.Instantiate(prefabPath, location, Quaternion.identity);

        if (droppedItem != null)
        {
            PhotonView itemView = droppedItem.GetComponent<PhotonView>();
            if (itemView != null)
            {
                itemView.RPC("PunRPC_SetItemProperties", RpcTarget.All, itemID, amount);
            }
            else
            {
                Debug.LogError($"Dropped item prefab '{prefabPath}' is missing a PhotonView component.");
            }
        }
    }

    public override void TakeDamage(float damage)
    {
        health -= damage;

        if (health <= 0)
        {
            Debug.Log("체력이 0이하가 되었으므로 사망 처리 시작");
            RequestForDeath(); //오버라이드로 인해 변경된 부분
        }
    }

    public void RequestForDeath()
    {
            pv.RPC("PunRPC_MonsterDeath", RpcTarget.MasterClient);
    }

    [PunRPC]
    private void PunRPC_MonsterDeath(PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        if (gameObject == null) return;


        if (dropItemID != -1)
        {
            ItemDatabase.Instance.GenerateItemPhoton(dropItemID, 3, transform.position);
        }
        PhotonNetwork.Destroy(this.gameObject);
    }

    // 환경 피해도 상호작용 플레이어 참조 없이 기존 방장 피해 처리로 전달합니다.
    public void RequestForTakeDamage(float damage, bool setInvincible = true)
    {
        if (invincibleState) return;
        if (setInvincible) SetInvincible(0.5f);

        pv.RPC("PunRPC_MonsterDamaged", RpcTarget.MasterClient, damage);
        
    }

    [PunRPC]
    private void PunRPC_MonsterDamaged(float damage, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        if (gameObject == null) return;


        TakeDamage(damage);
    }
}
