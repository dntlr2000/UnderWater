using UnityEngine;

public class PlayerState
{
    //MYJ 폴더 안에 있는 PlayerState 스크립트는 삭제 대신 전부 주석처리함
    protected Player player;
    protected PlayerStateMachine stateMachine; //StateMachince은 옛날에 폐기한다는 것으로 기억해서 사용하지 않는 방향으로 코드 작성
    protected string animationBoolName;

    protected bool isMoving;
    protected bool isRunning;
    protected bool onAir;
    protected bool inWater;
    protected bool isDown;
    protected bool onSit;


    public PlayerState(Player player, PlayerStateMachine stateMachine, string animationBoolName)
    {
        this.player = player;
        this.stateMachine = stateMachine;
        this.animationBoolName = animationBoolName;
    }

    public virtual void Enter()
    {
        //if (!string.IsNullOrEmpty(animationBoolName))
            //player.animator.SetBool(animationBoolName, true);

        Debug.Log($"PlayerState Entered : {animationBoolName}");
        //ApplyAllAnimations();
    }

    public virtual void Update()
    {
        //ApplyAllAnimations();
    }

    public virtual void Exit()
    {
        if (!string.IsNullOrEmpty(animationBoolName))
            player.animator.SetBool(animationBoolName, false);
    }

    protected void ApplyAllAnimations()
    {
        isMoving = player.isMoving;
        isRunning = player.isRunning;
        // onAir = !player.IsGrounded(); // 제거 (Player.cs가 처리)
        inWater = player.isUnderwater;
        isDown = player.isFainted;
        onSit = player.onSit;

        player.thirdViewAnimator.RequestSetDownState(isDown);
        // player.thirdViewAnimator.RequestSetAirState(onAir); // 제거
        player.thirdViewAnimator.RequestSetSitState(onSit);
        player.thirdViewAnimator.RequestSetWaterState(inWater);
        // player.thirdViewAnimator.RequestApplyVelocityY(); // 제거
        player.thirdViewAnimator.RequestSetMoveState(isMoving, isRunning);
    }

    protected void SetAttack()
    {
        player.thirdViewAnimator.RequestSetAttackState(0);
    }

    //public void SetJump()
    //{
    //    player.thirdViewAnimator.RequestSetJumpState(true);
    //}
}