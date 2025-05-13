using Unity.IO.LowLevel.Unsafe;
using UnityEngine;

public class Player : Character
{
    [Header("플레이어 애니메이터")]
    //public Animator anim{ get; private set; }
    public Animator anim; //1인칭과 3인칭이 구별되고 하위 프리팹에 존재하기 때문에, 프리팹 차원에서 할당하기 위하여 getter/setter 접근 제한자 삭제
    //public Animator anim3rdView;

    public Rigidbody rb { get; private set; }

    [Header("플레이어 상태")]
    public float hunger;    //허기
    public float thirst;    //수분
    public float oxygen;    //산소
    public float fatigue;   //피로도
    public float stamina;   //스테미너

    [Header("각 상태에 대응되는 바UI")]
    public StateUIManager healthBar;
    public StateUIManager hungerBar;    
    public StateUIManager thirstBar;    
    public StateUIManager oxygenBar;  
    public StateUIManager fatigueBar;    
    public StateUIManager staminaBar;
    //아예 UI 창에서 직접 생성하게 하여 관리하는 방법도 괜찮을듯
    //각 상태 별로 최대 수치는 정해져있는가?

    [Header("수중 이동용 카메라 할당")]
    public Transform X_AxisCamera;

    

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

        //UI 설정 (초기)
        SetBarUI();
        
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

    public void SetBarUI()
    {
        healthBar.SetBarUI(health);
        hungerBar.SetBarUI(hunger);
        thirstBar.SetBarUI(thirst);
        oxygenBar.SetBarUI(oxygen);
        fatigueBar.SetBarUI(fatigue);
        staminaBar.SetBarUI(stamina);
    }
}
