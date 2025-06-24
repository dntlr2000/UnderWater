using UnityEngine;


public class PlayerIdleState : PlayerState
{
    public PlayerIdleState(Player player, PlayerStateMachine stateMachine, string animationBoolName)
        : base(player, stateMachine, animationBoolName)
    {
    }

    public override void Update()
    {
        base.Update();
        // 필요시 애니메이션 조건 확인 등을 여기에 추가
    }
}

public class PlayerSwimState : PlayerState
{
    public PlayerSwimState(Player player, PlayerStateMachine stateMachine, string animationBoolName)
        : base(player, stateMachine, animationBoolName)
    {
    }

    public override void Update()
    {
        base.Update();
        // 물 속 애니메이션 전환 관련 처리만
    }
}
public class PlayerGroundMoveState : PlayerState
{
    public PlayerGroundMoveState(Player player, PlayerStateMachine stateMachine, string animationBoolName)
        : base(player, stateMachine, animationBoolName)
    {
    }

    public override void Update()
    {
        base.Update();
        // 달리기 등과 관련된 애니메이션만 담당
    }
}
