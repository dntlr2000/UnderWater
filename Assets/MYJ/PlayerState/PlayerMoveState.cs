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

    protected override void HandleMovement() //virtual로 되어 있길래 override로 수정
    {
        Vector3 move = moveDirection * player.moveSpeed;
        //지상 상태일 때
        //player.rb.linearVelocity = new Vector3(move.x, player.rb.linearVelocity.y, move.z);
        //수중 상태일 때
        player.rb.linearVelocity = new Vector3(move.x, move.y, move.z);
        /*
         //캐릭터가 바라보는 방향을 조절하는 스크립트. 이걸 빼고나면 PlayerState의 HandleMovement()와는 차이점이 없음. 
        // 이후에도 다른 스크립트가 추가되지 않을 예정이면 삭제하는 것이 좋아보임
        if(moveDirection != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(moveDirection);
            player.transform.rotation = Quaternion.Slerp(player.transform.rotation, lookRotation, 10f * Time.deltaTime);
        }
        */
    }

    public override void Exit()
    {
        base.Exit();
    }

   
}
