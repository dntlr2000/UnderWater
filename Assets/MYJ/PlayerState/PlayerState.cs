using UnityEditor;
using UnityEngine;

//상태마다 입력 처리, 애니메이션 전환, 타이머 처리 등 기본 세팅공간
public class PlayerState
{
    protected Player player;
    protected PlayerStateMachine stateMachine;

    //플레이어 움직임 관련
    protected float xInput, zInput;
    protected Vector3 moveDirection;

    private string animBoolName;

    protected bool triggerCalled;

   

    public PlayerState(Player _player, PlayerStateMachine _stateMachine, string _animBoolName)
    {
        this.player = _player;
        this.stateMachine = _stateMachine;
        this.animBoolName = _animBoolName;
    }

    public virtual void Enter()
    {
        player.anim.SetBool(animBoolName, true);
        triggerCalled = false;
    }

    public virtual void Update()
    {
        xInput = Input.GetAxisRaw("Horizontal");
        zInput = Input.GetAxisRaw("Vertical");
        //moveDirection = new Vector3(xInput, 0, zInput).normalized;
        //이동을 월드의 방향 기준이 아닌 플레이어가 바라보는 방향을 기준으로 삼기
        Transform playerTransform = player.gameObject.transform;
        Vector3 playerForward = playerTransform.forward;
        //playerForward.y = 0f; //애초에 회전을 z축으로만 하기 때문에 필요 없음
        playerForward.Normalize();

        Vector3 playerRight = playerTransform.right;
        //playerRight.y = 0f;
        playerRight.Normalize();

        moveDirection = (playerForward * zInput + playerRight * xInput).normalized;


        //수직(y축)의 속도를 의미(jump, fall등에서 사용)
        float yVelocity = player.rb.linearVelocity.y;
        player.anim.SetFloat("yVelocity", yVelocity);

        HandleMovement();
    }

    public virtual void Exit()
    {
        player.anim.SetBool(animBoolName, false);
    }

    public virtual void AnimationFinishTrigger()
    {
        triggerCalled = true;
    }

    protected virtual void HandleMovement()
    {
        Vector3 move = moveDirection * player.moveSpeed;
        player.rb.linearVelocity = new Vector3(move.x, player.rb.linearVelocity.y, move.z);
    }
}
