using UnityEngine;

public class PlayerMoveState : PlayerState
{
    public PlayerMoveState(Player _player, PlayerStateMachine _stateMachine, string _animBoolName) : base(_player, _stateMachine, _animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();
    }
    public override void Update()
    {
        base.Update();

        if(moveDirection == Vector3.zero)
        {
            stateMachine.ChangeState(player.idleState);
            return;
        }
        HandleMovement();
    }

    protected virtual void HandleMovement()
    {
        Vector3 move = moveDirection * player.moveSpeed;
        player.rb.linearVelocity = new Vector3(move.x, player.rb.linearVelocity.y, move.z);

        if(moveDirection != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(moveDirection);
            player.transform.rotation = Quaternion.Slerp(player.transform.rotation, lookRotation, 10f * Time.deltaTime);
        }
    }

    public override void Exit()
    {
        base.Exit();
    }

   
}
