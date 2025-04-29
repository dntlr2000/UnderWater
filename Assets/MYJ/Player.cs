using Unity.IO.LowLevel.Unsafe;
using UnityEngine;

public class Player : Character
{
    public Animator anim { get; private set; }
    public Rigidbody rb { get; private set; }

    [Header("플레이어 상태")]
    public float hunger;    //허기
    public float thirst;    //수분
    public float oxygen;    //산소
    public float fatigue;   //피로도
    public float stamina;   //스테미너

    #region States 상태들관리
    public PlayerStateMachine stateMachine { get; private set; }

    public PlayerIdleState idleState { get; private set; }
    public PlayerMoveState moveState { get; private set; }
    //똑같이 상태 추가
    #endregion
    private void Awake()
    {
        stateMachine = new PlayerStateMachine();

        //각 상태 인스턴스 생성
        idleState = new PlayerIdleState(this, stateMachine, "Idle");
        //똑같이 상태 추가
        moveState = new PlayerMoveState(this, stateMachine, "Move");
    }
    private void Start()
    {
        anim = GetComponentInChildren<Animator>(); //수정 필요
        rb = GetComponent<Rigidbody>();

        // 게임 시작 시 초기 상태를 대기 상태(idleState)로 설정
        stateMachine.Initialize(idleState); //애니메이션을 넣어주어야 함
    }

    private void Update()
    {
        stateMachine.currentState.Update();
    }

    protected override void Move(Vector3 direction)
    {
        rb.MovePosition(transform.position + direction.normalized * moveSpeed * Time.deltaTime);
        Debug.Log("플레이어 이동중");
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
        Debug.Log("플레이어 사망");
    }
}
