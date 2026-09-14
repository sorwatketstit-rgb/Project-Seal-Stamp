using System;
using UnityEngine;

namespace SM64
{
    /// <summary>
    /// Manages current state, previous state, and transitions between IPlayerState instances.
    /// </summary>
    public class PlayerStateMachine
    {
        public IPlayerState CurrentState { get; private set; }
        public IPlayerState PreviousState { get; private set; }

        public event Action<IPlayerState> OnStateChanged;

        public void Initialize(IPlayerState startingState)
        {
            if (startingState == null)
            {
                Debug.LogError("PlayerStateMachine: startingState cannot be null.");
                return;
            }

            CurrentState = startingState;
            CurrentState.Enter();
            OnStateChanged?.Invoke(CurrentState);
        }

        public void ChangeState(IPlayerState newState)
        {
            if (newState == null)
            {
                Debug.LogError("PlayerStateMachine: Cannot transition to a null state.");
                return;
            }

            if (CurrentState == newState)
                return;

            CurrentState?.Exit();
            PreviousState = CurrentState;
            CurrentState = newState;
            CurrentState.Enter();

            OnStateChanged?.Invoke(CurrentState);
        }

        public void HandleInput()
        {
            CurrentState?.HandleInput();
        }

        public void LogicUpdate()
        {
            CurrentState?.LogicUpdate();
        }

        public void PhysicsUpdate()
        {
            CurrentState?.PhysicsUpdate();
        }
    }
}
