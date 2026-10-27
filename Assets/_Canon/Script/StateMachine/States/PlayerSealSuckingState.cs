using UnityEngine;

namespace SM64
{
    /// <summary>
    /// Seal Sucking State — a sub-state rooted in <see cref="PlayerSealState"/>.
    /// It can ONLY be entered from the seal state while the action key (Shift) is held, and it
    /// always returns to the seal state: on release, when the air runs out, or when an item is swallowed.
    /// While sucking the seal stands still (it can still turn to aim the cone) and the suction
    /// particle object is enabled.
    ///
    /// It derives from PlayerSealState so existing checks such as `state is PlayerSealState`
    /// (animation controller, UI) keep treating it as part of the seal state.
    /// </summary>
    public class PlayerSealSuckingState : PlayerSealState
    {
        private readonly PlayerSealState _root;
        private bool _returningToSeal;

        public PlayerSealSuckingState(SM64PlayerController controller, PlayerStateMachine stateMachine, PlayerSealState root)
            : base(controller, stateMachine, root)
        {
            _root = root;
        }

        public override void Enter()
        {
            _returningToSeal = false;

            // No transformation VFX / model swap here: the player is already a seal.
            Seal.BeginSucking();
        }

        public override void HandleInput()
        {
            if (Input == null) return;

            // Jump is ignored while sucking; release Shift first, then jump to revert.
            if (!Input.KickHeld)
            {
                ReturnToSeal();
            }
        }

        public override void LogicUpdate()
        {
            // Keep the landing check (the seal can enter this state mid-air after a leap timeout).
            base.LogicUpdate();

            // Swallowed an item (suction was switched off) or ran out of air.
            if (!Seal.IsSucking || Seal.HasItem || Seal.OutOfAir)
            {
                ReturnToSeal();
            }
        }

        public override void PhysicsUpdate()
        {
            Controller.ApplyGravity();

            // Stand still, but allow turning to aim the cone.
            Vector3 desiredDir = Controller.GetCameraRelativeInput(Input != null ? Input.MoveInput : Vector2.zero);
            if (desiredDir.sqrMagnitude > 0.001f)
            {
                Controller.RotateTowards(desiredDir, Controller.turnSmoothTime);
            }

            Controller.HorizontalVelocity = Vector3.MoveTowards(
                Controller.HorizontalVelocity, Vector3.zero, Controller.deceleration * Time.deltaTime);
        }

        public override void Exit()
        {
            Seal.EndSucking();

            if (_returningToSeal)
            {
                _returningToSeal = false;
                return; // still a seal; the seal state's Enter will not replay the transform VFX
            }

            // Something else pulled the player out of sucking: leave seal mode properly.
            LeaveSealMode();
        }

        private void ReturnToSeal()
        {
            if (_returningToSeal) return; // HandleInput and LogicUpdate can both fire in one frame
            _returningToSeal = true;
            StateMachine.ChangeState(_root);
        }
    }
}
