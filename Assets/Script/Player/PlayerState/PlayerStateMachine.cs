using UnityEngine;

public class PlayerStateMachine
{
    public PlayerState currentState;

    public void Initialize(PlayerState startState)
    {
        currentState = startState;
        currentState.Enter();
    }

    public void ChangeState(PlayerState newState)
    {
        if (currentState == newState) return;
        currentState.Exit();
        currentState = newState;
        currentState.Enter();
    }
}
