using UnityEngine;

public class Monster : Character
{
    [Header("추적 대상")]
    public Transform target;
    public float stopDistance = 1.5f;

    private Rigidbody mrb;

    private string poolkey;

    private void Awake()
    {
        mrb = GetComponent<Rigidbody>();
    }

    //일정 시간마다 타겟이 있는지 확인하고 방향 및 이동 여부를 판단
    private void FixedUpdate()
    {
        if (target == null) return;

        Vector3 direction = (target.position - transform.position);
        float distance = direction.magnitude;

        if (distance > stopDistance)
        {
            direction.y = 0; // 수직 이동 제거
            Move(direction.normalized);
        }
    }

    public void Initialize(string poolkey)
    {
        this.poolkey = poolkey;
        health = 100; // 초기 체력 등
        target = GameObject.FindWithTag("Player")?.transform;
    }
    protected override void Move(Vector3 direction)
    {
        Vector3 moveVector = direction * moveSpeed * Time.fixedDeltaTime;
        mrb.MovePosition(transform.position + moveVector);

        // 바라보는 방향 회전
        if (direction != Vector3.zero)
        {
            Quaternion lookRot = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, 10f * Time.fixedDeltaTime);
        }
        Debug.Log("몬스터 이동중");
    }

    public override void Attack()
    {
        base.Attack();
    }

    public override void TakeDamage(float damage)
    {
        base.TakeDamage(damage);
    }

    protected override void Death()
    {
        Debug.Log("몬스터 사망");
        //사망시 오브젝트 풀에 반환
        PoolManager.Instance.ReturnToPool(poolkey, gameObject);
    }

    //플레이어를 공격하는 루틴 설정
    //경험치 또는 아이템 드랍
}
