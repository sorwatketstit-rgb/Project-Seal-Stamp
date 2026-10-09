using UnityEngine;

namespace SM64
{
    /// <summary>
    /// Base class for grounded movement states (Walking and Running).
    /// Provides shared slope handling, ground snapping, jump takeoff, and ledge fall detection.
    /// </summary>
    public abstract class PlayerGroundedState : PlayerState
    {
        protected bool IsLongJumpPrep;
        protected bool IsBackflipPrep;
        protected float PrepTimer;
        protected const float PrepTimeout = 0.5f;

        protected PlayerGroundedState(SM64PlayerController controller, PlayerStateMachine stateMachine) 
            : base(controller, stateMachine) { }

        public override void Enter()
        {
            Controller.ResetAirJumps();
            Controller.VerticalVelocity = -2f; // keeps controller glued to ground
            IsLongJumpPrep = false;
            IsBackflipPrep = false;
            PrepTimer = 0f;

            // Check if a jump was buffered prior to landing
            if (Controller.ConsumeBufferedJump())
            {
                TriggerJump();
            }
        }

        public override void HandleInput()
        {
            if (Input == null) return;

            // Transition to Crouching State
            if (Input.CrouchPressed)
            {
                StateMachine.ChangeState(Controller.CrouchingState);
                return;
            }

            // Jump trigger
            if (Input.JumpPressed)
            {
                TriggerJump();
            }
        }

        protected virtual void TriggerJump()
        {
            if (IsLongJumpPrep)
            {
                StateMachine.ChangeState(Controller.LongJumpState);
                return;
            }

            if (IsBackflipPrep)
            {
                StateMachine.ChangeState(Controller.BackflipState);
                return;
            }

            // Standard ground jump into AirborneState
            Controller.VerticalVelocity = Controller.jumpForce;
            Controller.AirborneState.SetupJump(consumedAirJump: false);
            StateMachine.ChangeState(Controller.AirborneState);
        }

        public override void LogicUpdate()
        {
            // If stepped off a ledge into the air
            if (!Controller.IsGrounded())
            {
                Controller.AirborneState.SetupFallWithCoyoteTime();
                StateMachine.ChangeState(Controller.AirborneState);
            }
        }

        public override void Exit()
        {
            IsLongJumpPrep = false;
            IsBackflipPrep = false;
        }
    }
}
