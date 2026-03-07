using UnityEngine;

public enum PlayerStateType
{
    Idle,
    Walk,
    Run,
    SwimIdle,
    SwimMove,
    SwimFast,
    Attack,
    Die
}
/*
public class PlayerIdleState : PlayerState
{
    public PlayerIdleState(Player player, PlayerStateMachine stateMachine)
        : base(player, stateMachine, "Idle") { }

    public override void Update()
    {
        if (player.isUnderwater)
        {
            stateMachine.ChangeState(new PlayerSwimIdleState(player, stateMachine));
            //player.thirdViewAnimator.RequestSetWaterState(true);
            return;
        }

        float moveInput = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical")).magnitude;
        if (moveInput > 0.1f)
        {
            if (Input.GetKey(KeyCode.LeftShift))
            {

                stateMachine.ChangeState(new PlayerRunState(player, stateMachine));
            }
            else
                stateMachine.ChangeState(new PlayerWalkState(player, stateMachine));
        }

        if (player.health <= 0)
        {
            stateMachine.ChangeState(new PlayerDieState(player, stateMachine));
            //player.thirdViewAnimator.RequestSetDownState(true);
        }

        player.thirdViewAnimator.RequestSetWaterState(false);
        player.thirdViewAnimator.RequestSetMoveState(false, false);

    }
}

public class PlayerWalkState : PlayerState
{

    public PlayerWalkState(Player player, PlayerStateMachine stateMachine)
        : base(player, stateMachine, "Walk") { }
    public override void Update()
    {
        if (player.isUnderwater)
        {
            stateMachine.ChangeState(new PlayerSwimIdleState(player, stateMachine));
            return;
        }

        float moveInput = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical")).magnitude;
        if (moveInput <= 0.1f)
        {
            stateMachine.ChangeState(new PlayerIdleState(player, stateMachine));
        }
        else if ((Input.GetKey(KeyCode.LeftShift)))
        {
            stateMachine.ChangeState(new PlayerRunState(player, stateMachine));
        }
        player.thirdViewAnimator.RequestSetWaterState(false);
        player.thirdViewAnimator.RequestSetMoveState(true, false);

        player.thirdViewAnimator.RequestSetDownState(false);

    }
}
public class PlayerRunState : PlayerState
{
    public PlayerRunState(Player player, PlayerStateMachine stateMachine)
        : base(player, stateMachine, "Run") { }

    public override void Update()
    {
        if (player.isUnderwater)
        {
            stateMachine.ChangeState(new PlayerSwimIdleState(player, stateMachine));
            return;
        }

        float moveInput = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical")).magnitude;
        if (moveInput <= 0.1f || !Input.GetKey(KeyCode.LeftShift))
        {
            stateMachine.ChangeState(new PlayerWalkState(player, stateMachine));
        }
        player.thirdViewAnimator.RequestSetWaterState(false);
        player.thirdViewAnimator.RequestSetMoveState(true, true);
    }
}

public class PlayerSwimIdleState : PlayerState
{
    public PlayerSwimIdleState(Player player, PlayerStateMachine stateMachine)
        : base(player, stateMachine, "SwimIdle") { }

    public override void Update()
    {
        if (!player.isUnderwater)
        {
            stateMachine.ChangeState(new PlayerIdleState(player, stateMachine));
            return;
        }

        float moveInput = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical")).magnitude;
        bool verticalMove = Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.LeftControl);

        if (moveInput > 0.1f || verticalMove)
        {
            if (Input.GetKey(KeyCode.LeftShift))
                stateMachine.ChangeState(new PlayerSwimFastState(player, stateMachine));
            else
                stateMachine.ChangeState(new PlayerSwimMoveState(player, stateMachine));
        }
        player.thirdViewAnimator.RequestSetWaterState(true);
        player.thirdViewAnimator.RequestSetMoveState(false, false);

        player.thirdViewAnimator.RequestSetDownState(false);
    }
}

public class PlayerSwimMoveState : PlayerState
{
    public PlayerSwimMoveState(Player player, PlayerStateMachine stateMachine)
        : base(player, stateMachine, "SwimMove") { }

    public override void Update()
    {
        if (!player.isUnderwater)
        {
            stateMachine.ChangeState(new PlayerIdleState(player, stateMachine));
            return;
        }

        float moveInput = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical")).magnitude;
        bool verticalMove = Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.LeftControl);

        if (moveInput <= 0.1f && !verticalMove)
        {
            stateMachine.ChangeState(new PlayerSwimIdleState(player, stateMachine));
        }
        else if (Input.GetKey(KeyCode.LeftShift))
        {
            stateMachine.ChangeState(new PlayerSwimFastState(player, stateMachine));
        }
        player.thirdViewAnimator.RequestSetWaterState(true);
        player.thirdViewAnimator.RequestSetMoveState(true, false);
    }
}

public class PlayerSwimFastState : PlayerState
{
    public PlayerSwimFastState(Player player, PlayerStateMachine stateMachine)
        : base(player, stateMachine, "SwimFast") { }

    public override void Update()
    {
        if (!player.isUnderwater)
        {
            stateMachine.ChangeState(new PlayerIdleState(player, stateMachine));
            return;
        }

        if (!Input.GetKey(KeyCode.LeftShift))
        {
            stateMachine.ChangeState(new PlayerSwimMoveState(player, stateMachine));
        }
        player.thirdViewAnimator.RequestSetWaterState(true);
        player.thirdViewAnimator.RequestSetMoveState(true, true);
    }
}

public class PlayerAttackState : PlayerState
{
    public PlayerAttackState(Player player, PlayerStateMachine stateMachine)
        : base(player, stateMachine, "Attack") { }

    public override void Update()
    {
        // 공격 모션이 끝나면 원래 상태로 복귀
        if (!player.animator.GetCurrentAnimatorStateInfo(0).IsName("Attack"))
        {
            player.thirdViewAnimator.RequestResetTrigger();
            if (player.isUnderwater)
                stateMachine.ChangeState(new PlayerSwimIdleState(player, stateMachine));
            else
                stateMachine.ChangeState(new PlayerIdleState(player, stateMachine));
            return;
        }
        player.thirdViewAnimator.RequestSetAttackState(0); //트리거 형식이라 Update랑은 안맞음 ->리셋 트리거로 어떻게든 해봄
    }
}

public class PlayerDieState : PlayerState
{
    public PlayerDieState(Player player, PlayerStateMachine stateMachine)
        : base(player, stateMachine, "Die") { }

    public override void Enter()
    {
        base.Enter();
        player.isBusy = true; // 죽으면 조작 불가
        player.thirdViewAnimator.RequestSetDownState(true);
    }
}
*/