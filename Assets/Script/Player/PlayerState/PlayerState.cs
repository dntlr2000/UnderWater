using UnityEngine;

public class PlayerState
{
    //MYJ 폴더 안에 있는 PlayerState 스크립트는 삭제 대신 전부 주석처리함
    protected Player player;
    protected PlayerStateMachine stateMachine;
    protected string animationBoolName;

    public PlayerState(Player player, PlayerStateMachine stateMachine, string animationBoolName)
    {
        this.player = player;
        this.stateMachine = stateMachine;
        this.animationBoolName = animationBoolName;
    }

    public virtual void Enter()
    {
        if (!string.IsNullOrEmpty(animationBoolName))
            player.animator.SetBool(animationBoolName, true);
    }

    public virtual void Update()
    {
        // 상태별 애니메이션 처리 로직이 필요하면 여기에 작성
    }

    public virtual void Exit()
    {
        if (!string.IsNullOrEmpty(animationBoolName))
            player.animator.SetBool(animationBoolName, false);
    }
}