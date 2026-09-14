using UnityEngine;

namespace SM64
{
    /// <summary>
    /// Abstract base class for player states providing easy access to controller context and components.
    /// </summary>
    public abstract class PlayerState : IPlayerState
    {
        protected readonly SM64PlayerController Controller;
        protected readonly PlayerStateMachine StateMachine;

        protected SM64PlayerInput Input => Controller.Input;
        protected CharacterController CharController => Controller.CharacterController;

        protected PlayerState(SM64PlayerController controller, PlayerStateMachine stateMachine)
        {
            Controller = controller;
            StateMachine = stateMachine;
        }

        public virtual void Enter() { }
        public virtual void HandleInput() { }
        public virtual void LogicUpdate() { }
        public virtual void PhysicsUpdate() { }
        public virtual void Exit() { }
    }
}
