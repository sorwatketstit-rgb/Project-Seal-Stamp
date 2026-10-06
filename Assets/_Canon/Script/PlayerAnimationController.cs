using System;
using System.Collections.Generic;
using UnityEngine;

namespace SM64
{
    /// <summary>
    /// Supported animation state types mapped from player states and physics conditions.
    /// </summary>
    public enum PlayerAnimationType
    {
        Idle,
        Walk,
        Run,
        Jump,
        DoubleJump,
        Fall,
        LongJump,
        Backflip,
        WallSlide,
        WallJump,
        LedgeHang,
        LedgeClimb,
        GroundPoundStall,
        GroundPoundSlam,
        GroundPoundLand,
        Leap
    }

    /// <summary>
    /// Animation player and controller that monitors the SM64PlayerController state machine
    /// and drives character animations while enforcing core validation rules:
    /// 1. Cannot Run while Airborne (corrected to mid-air Fall/Jump).
    /// 2. Cannot Fall while Grounded (corrected to Idle/Walk/Run).
    /// Supports direct Animator state crossfading and Animator parameter updates (Speed, IsGrounded, etc.).
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerAnimationController : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The SM64PlayerController to monitor. If null, searches on this GameObject or in parent/children.")]
        [SerializeField] private SM64PlayerController playerController;

        [Tooltip("Animator driving character model animations. If null, searches on this GameObject or children.")]
        [SerializeField] private Animator animator;

        [Header("Transition Settings")]
        [Tooltip("Default cross-fade transition duration between animation states in seconds.")]
        [SerializeField] private float defaultCrossfadeDuration = 0.12f;

        [Header("Velocity & Thresholds")]
        [Tooltip("Minimum horizontal speed to transition from Idle to Walk animation.")]
        [SerializeField] private float walkSpeedThreshold = 0.15f;

        [Tooltip("Vertical speed threshold below which airborne state is classified as Falling.")]
        [SerializeField] private float fallVelocityThreshold = -0.5f;

        [Header("Animation State Names (Customizable for Animator States)")]
        [SerializeField] private string idleStateName = "Idle";
        [SerializeField] private string walkStateName = "Walk";
        [SerializeField] private string runStateName = "Run";
        [SerializeField] private string jumpStateName = "Jump";
        [SerializeField] private string doubleJumpStateName = "DoubleJump";
        [SerializeField] private string fallStateName = "Fall";
        [SerializeField] private string longJumpStateName = "LongJump";
        [SerializeField] private string backflipStateName = "Backflip";
        [SerializeField] private string wallSlideStateName = "WallSlide";
        [SerializeField] private string wallJumpStateName = "WallJump";
        [SerializeField] private string ledgeHangStateName = "LedgeHang";
        [SerializeField] private string ledgeClimbStateName = "LedgeClimb";
        [SerializeField] private string groundPoundStallStateName = "GroundPoundStall";
        [SerializeField] private string groundPoundSlamStateName = "GroundPoundSlam";
        [SerializeField] private string groundPoundLandStateName = "GroundPoundLand";
        [SerializeField] private string leapStateName = "Leap";

        [Header("Animator Parameter Names")]
        [SerializeField] private string speedParam = "Speed";
        [SerializeField] private string verticalSpeedParam = "VerticalSpeed";
        [SerializeField] private string isGroundedParam = "IsGrounded";
        [SerializeField] private string isRunningParam = "IsRunning";
        [SerializeField] private string isAirborneParam = "IsAirborne";
        [SerializeField] private string isWallSlidingParam = "IsWallSliding";
        [SerializeField] private string isLedgeHangingParam = "IsLedgeHanging";
        [SerializeField] private string stateHashParam = "AnimState";

        [Header("Debug & Diagnostics")]
        [SerializeField] private bool enableDebugLogs = false;
        [SerializeField] private PlayerAnimationType currentAnimationState = PlayerAnimationType.Idle;
        [SerializeField] private bool isGroundedStatus;
        [SerializeField] private int ruleCorrectionsCount = 0;

        // ─── Model Tilt ──────────────────────────────────────────────────────────
        [Header("Model Tilt (Air Only)")]
        [Tooltip("Transform to tilt (the visual model root). Leave empty to tilt this GameObject.")]
        [SerializeField] private Transform modelRoot;

        [Tooltip("Maximum tilt angle in degrees applied along each local axis.")]
        [SerializeField] private float maxTiltAngle = 25f;

        [Tooltip("How fast the model tilts toward the target angle (degrees/sec).")]
        [SerializeField] private float tiltSpeed = 8f;

        [Tooltip("How fast the model returns to upright when input is released or grounded (degrees/sec).")]
        [SerializeField] private float tiltRecoverySpeed = 10f;

        /// <summary>Current smooth tilt angles applied to modelRoot (X = forward/back pitch, Z = side roll).</summary>
        private Vector3 _currentTiltEuler;

        // Events
        public event Action<PlayerAnimationType, PlayerAnimationType> OnAnimationChanged;

        // Public Accessors
        public PlayerAnimationType CurrentAnimationState => currentAnimationState;
        public Animator Animator => animator;
        public SM64PlayerController PlayerController => playerController;

        // Cached Hashes for Performance
        private readonly Dictionary<PlayerAnimationType, int> _stateNameToHash = new Dictionary<PlayerAnimationType, int>();
        private int _speedHash;
        private int _vertSpeedHash;
        private int _groundedHash;
        private int _runningHash;
        private int _airborneHash;
        private int _wallSlideHash;
        private int _ledgeHangHash;
        private int _stateParamHash;

        // Parameter existence caching on Animator
        private readonly HashSet<int> _availableAnimatorParameters = new HashSet<int>();

        private void Awake()
        {
            if (playerController == null)
            {
                playerController = GetComponent<SM64PlayerController>() ??
                                   GetComponentInParent<SM64PlayerController>() ??
                                   GetComponentInChildren<SM64PlayerController>();
            }

            if (animator == null)
            {
                animator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();
            }

            // Default tilt target to this transform when no model root is assigned
            if (modelRoot == null)
            {
                modelRoot = transform;
            }

            InitializeHashes();
            CacheAnimatorParameters();
        }

        private void Start()
        {
            if (playerController != null && playerController.StateMachine != null)
            {
                playerController.StateMachine.OnStateChanged += HandleStateChanged;
                EvaluateAndApplyAnimation(force: true);
            }
            else
            {
                Debug.LogWarning("PlayerAnimationController: SM64PlayerController not found.", this);
            }
        }

        private void OnDestroy()
        {
            if (playerController != null && playerController.StateMachine != null)
            {
                playerController.StateMachine.OnStateChanged -= HandleStateChanged;
            }
        }

        private void Update()
        {
            if (playerController == null) return;

            isGroundedStatus = playerController.IsGrounded();

            // Continuous animation state evaluation (e.g. Speed changes, Jump -> Fall transitions)
            EvaluateAndApplyAnimation(force: false);

            // Update standard blend parameters for Animator
            UpdateAnimatorParameters();
        }

        private void LateUpdate()
        {
            if (playerController == null || modelRoot == null) return;

            ApplyModelTilt();
        }

        /// <summary>
        /// Tilts the model root based on WASD input when the player is airborne and not leaping.
        /// Pitch (X) tilts forward/backward; Roll (Z) tilts sideways.
        /// Runs in LateUpdate so it overrides any Animator root-bone transform in the same frame.
        /// </summary>
        private void ApplyModelTilt()
        {
            bool isGrounded = playerController.IsGrounded();
            bool isLeaping  = playerController.StateMachine?.CurrentState is PlayerLeapState;

            // ── Tilt is only active while airborne and not leaping ───────────────
            bool shouldTilt = !isGrounded && !isLeaping;

            Vector3 targetTilt = Vector3.zero;

            if (shouldTilt)
            {
                // Read input vector from the controller
                SM64PlayerInput input = playerController.Input;
                Vector2 moveInput = (input != null) ? input.MoveInput : Vector2.zero;

                if (moveInput.sqrMagnitude > 0.01f)
                {
                    // Convert move input to camera-relative world direction (same as controller)
                    Vector3 worldDir = playerController.GetCameraRelativeInput(moveInput);

                    // Project that world direction onto the model's local axes to get tilt axes
                    // Forward/back component  -> pitch around local X (forward tilt)
                    // Left/right component    -> roll  around local Z (sideways tilt)
                    float forwardDot = Vector3.Dot(worldDir, modelRoot.forward);
                    float rightDot   = Vector3.Dot(worldDir, modelRoot.right);

                    // Positive X tilts nose up when moving forward
                    float targetPitch = forwardDot * maxTiltAngle;
                    // Positive Z tilts right side up when moving right
                    float targetRoll  = rightDot   * maxTiltAngle;

                    targetTilt = new Vector3(targetPitch, 0f, targetRoll);
                }
            }

            // Interpolate toward target — use recovery speed when zeroing out
            float speed = (targetTilt.sqrMagnitude > 0.001f) ? tiltSpeed : tiltRecoverySpeed;
            _currentTiltEuler = Vector3.MoveTowards(_currentTiltEuler, targetTilt, speed * maxTiltAngle * Time.deltaTime);

            // Apply as a local rotation offset on top of whatever Y rotation the controller set
            float currentY = modelRoot.localEulerAngles.y;
            modelRoot.localEulerAngles = new Vector3(
                _currentTiltEuler.x,
                currentY,
                _currentTiltEuler.z
            );
        }

        /// <summary>
        /// Cache state string hashes into integer IDs for high performance.
        /// </summary>
        private void InitializeHashes()
        {
            _stateNameToHash[PlayerAnimationType.Idle] = Animator.StringToHash(idleStateName);
            _stateNameToHash[PlayerAnimationType.Walk] = Animator.StringToHash(walkStateName);
            _stateNameToHash[PlayerAnimationType.Run] = Animator.StringToHash(runStateName);
            _stateNameToHash[PlayerAnimationType.Jump] = Animator.StringToHash(jumpStateName);
            _stateNameToHash[PlayerAnimationType.DoubleJump] = Animator.StringToHash(doubleJumpStateName);
            _stateNameToHash[PlayerAnimationType.Fall] = Animator.StringToHash(fallStateName);
            _stateNameToHash[PlayerAnimationType.LongJump] = Animator.StringToHash(longJumpStateName);
            _stateNameToHash[PlayerAnimationType.Backflip] = Animator.StringToHash(backflipStateName);
            _stateNameToHash[PlayerAnimationType.WallSlide] = Animator.StringToHash(wallSlideStateName);
            _stateNameToHash[PlayerAnimationType.WallJump] = Animator.StringToHash(wallJumpStateName);
            _stateNameToHash[PlayerAnimationType.LedgeHang] = Animator.StringToHash(ledgeHangStateName);
            _stateNameToHash[PlayerAnimationType.LedgeClimb] = Animator.StringToHash(ledgeClimbStateName);
            _stateNameToHash[PlayerAnimationType.GroundPoundStall] = Animator.StringToHash(groundPoundStallStateName);
            _stateNameToHash[PlayerAnimationType.GroundPoundSlam] = Animator.StringToHash(groundPoundSlamStateName);
            _stateNameToHash[PlayerAnimationType.GroundPoundLand] = Animator.StringToHash(groundPoundLandStateName);
            _stateNameToHash[PlayerAnimationType.Leap] = Animator.StringToHash(leapStateName);

            _speedHash = Animator.StringToHash(speedParam);
            _vertSpeedHash = Animator.StringToHash(verticalSpeedParam);
            _groundedHash = Animator.StringToHash(isGroundedParam);
            _runningHash = Animator.StringToHash(isRunningParam);
            _airborneHash = Animator.StringToHash(isAirborneParam);
            _wallSlideHash = Animator.StringToHash(isWallSlidingParam);
            _ledgeHangHash = Animator.StringToHash(isLedgeHangingParam);
            _stateParamHash = Animator.StringToHash(stateHashParam);
        }

        private void CacheAnimatorParameters()
        {
            _availableAnimatorParameters.Clear();
            if (animator == null || animator.runtimeAnimatorController == null) return;

            foreach (AnimatorControllerParameter param in animator.parameters)
            {
                _availableAnimatorParameters.Add(param.nameHash);
            }
        }

        private void HandleStateChanged(IPlayerState newState)
        {
            EvaluateAndApplyAnimation(force: true);
        }

        /// <summary>
        /// Evaluates the desired animation based on current player state machine,
        /// validates the animation against controller rules, and plays it.
        /// </summary>
        public void EvaluateAndApplyAnimation(bool force = false)
        {
            if (playerController == null || playerController.StateMachine == null) return;

            IPlayerState activeState = playerController.StateMachine.CurrentState;
            if (activeState == null) return;

            PlayerAnimationType rawDesiredState = DetermineDesiredAnimation(activeState);
            PlayerAnimationType validatedState = ValidateAnimationRules(rawDesiredState);

            if (force || validatedState != currentAnimationState)
            {
                TransitionToAnimation(validatedState);
            }
        }

        /// <summary>
        /// Maps the active IPlayerState and controller parameters to a raw PlayerAnimationType.
        /// </summary>
        private PlayerAnimationType DetermineDesiredAnimation(IPlayerState state)
        {
            float horizSpeed = playerController.HorizontalVelocity.magnitude;
            float vertSpeed = playerController.VerticalVelocity;

            if (state is PlayerWalkingState)
            {
                return (horizSpeed > walkSpeedThreshold) ? PlayerAnimationType.Walk : PlayerAnimationType.Idle;
            }

            if (state is PlayerRunningState)
            {
                return PlayerAnimationType.Run;
            }

            if (state is PlayerAirborneState)
            {
                // If moving upward -> Jump, otherwise -> Fall
                return (vertSpeed > fallVelocityThreshold) ? PlayerAnimationType.Jump : PlayerAnimationType.Fall;
            }

            if (state is PlayerLongJumpState)
            {
                return PlayerAnimationType.LongJump;
            }

            if (state is PlayerBackflipState)
            {
                return PlayerAnimationType.Backflip;
            }

            if (state is PlayerWallJumpState)
            {
                return PlayerAnimationType.WallJump;
            }

            if (state is PlayerWallSlideState)
            {
                return PlayerAnimationType.WallSlide;
            }

            if (state is PlayerLedgeGrabState)
            {
                // Can distinguish hang vs climb
                return PlayerAnimationType.LedgeHang;
            }

            if (state is PlayerGroundPoundState)
            {
                if (vertSpeed < -10f)
                    return PlayerAnimationType.GroundPoundSlam;
                if (playerController.IsGrounded())
                    return PlayerAnimationType.GroundPoundLand;

                return PlayerAnimationType.GroundPoundStall;
            }

            if (state is PlayerLeapState)
            {
                return PlayerAnimationType.Leap;
            }

            // Fallback: use grounded status and speed
            if (playerController.IsGrounded())
            {
                if (playerController.CurrentMovementSubState == MovementSubState.Running && horizSpeed > walkSpeedThreshold)
                    return PlayerAnimationType.Run;
                if (horizSpeed > walkSpeedThreshold)
                    return PlayerAnimationType.Walk;

                return PlayerAnimationType.Idle;
            }

            return (vertSpeed > fallVelocityThreshold) ? PlayerAnimationType.Jump : PlayerAnimationType.Fall;
        }

        /// <summary>
        /// Enforces animation controller specific rules:
        /// 1. CANNOT RUN WHILE AIRBORNE: If Run is requested while airborne, corrected to Fall/Jump.
        /// 2. CANNOT FALL WHILE GROUNDED: If Fall is requested while grounded, corrected to Grounded Idle/Walk/Run.
        /// </summary>
        public PlayerAnimationType ValidateAnimationRules(PlayerAnimationType requestedState)
        {
            bool grounded = playerController.IsGrounded();
            float horizSpeed = playerController.HorizontalVelocity.magnitude;
            float vertSpeed = playerController.VerticalVelocity;

            // RULE 1: Can't run while airborne
            if (!grounded)
            {
                if (requestedState == PlayerAnimationType.Run || requestedState == PlayerAnimationType.Walk || requestedState == PlayerAnimationType.Idle)
                {
                    ruleCorrectionsCount++;
                    PlayerAnimationType corrected = (vertSpeed > fallVelocityThreshold) ? PlayerAnimationType.Jump : PlayerAnimationType.Fall;

                    if (enableDebugLogs)
                    {
                        Debug.LogWarning($"[PlayerAnimationController Rule Violation Prevented] Attempted '{requestedState}' while Airborne. Corrected to '{corrected}'.", this);
                    }
                    return corrected;
                }
            }

            // RULE 2: Can't fall while grounded
            if (grounded)
            {
                if (requestedState == PlayerAnimationType.Fall)
                {
                    ruleCorrectionsCount++;
                    PlayerAnimationType corrected;

                    if (playerController.CurrentMovementSubState == MovementSubState.Running && horizSpeed > walkSpeedThreshold)
                    {
                        corrected = PlayerAnimationType.Run;
                    }
                    else if (horizSpeed > walkSpeedThreshold)
                    {
                        corrected = PlayerAnimationType.Walk;
                    }
                    else
                    {
                        corrected = PlayerAnimationType.Idle;
                    }

                    if (enableDebugLogs)
                    {
                        Debug.LogWarning($"[PlayerAnimationController Rule Violation Prevented] Attempted '{requestedState}' while Grounded. Corrected to '{corrected}'.", this);
                    }
                    return corrected;
                }
            }

            return requestedState;
        }

        /// <summary>
        /// Triggers transition to target validated animation state.
        /// </summary>
        private void TransitionToAnimation(PlayerAnimationType newAnimation)
        {
            PlayerAnimationType previousState = currentAnimationState;
            currentAnimationState = newAnimation;

            if (enableDebugLogs)
            {
                Debug.Log($"[PlayerAnimationController] Transition: {previousState} -> {newAnimation}", this);
            }

            // Play state on Animator if attached
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                if (_stateNameToHash.TryGetValue(newAnimation, out int stateHash))
                {
                    // If the Animator has the state or supports CrossFade
                    animator.CrossFade(stateHash, defaultCrossfadeDuration);
                }
            }

            OnAnimationChanged?.Invoke(previousState, newAnimation);
        }

        /// <summary>
        /// Updates parameters on the Animator if they are present in the controller.
        /// </summary>
        private void UpdateAnimatorParameters()
        {
            if (animator == null || animator.runtimeAnimatorController == null) return;

            float horizSpeed = playerController.HorizontalVelocity.magnitude;
            float vertSpeed = playerController.VerticalVelocity;
            bool grounded = playerController.IsGrounded();
            bool isRunning = playerController.CurrentMovementSubState == MovementSubState.Running;
            bool isWallSlide = playerController.StateMachine?.CurrentState is PlayerWallSlideState;
            bool isLedge = playerController.StateMachine?.CurrentState is PlayerLedgeGrabState;

            SetParameterFloat(_speedHash, horizSpeed);
            SetParameterFloat(_vertSpeedHash, vertSpeed);
            SetParameterBool(_groundedHash, grounded);
            SetParameterBool(_runningHash, isRunning);
            SetParameterBool(_airborneHash, !grounded);
            SetParameterBool(_wallSlideHash, isWallSlide);
            SetParameterBool(_ledgeHangHash, isLedge);
            SetParameterInt(_stateParamHash, (int)currentAnimationState);
        }

        private void SetParameterFloat(int hash, float val)
        {
            if (_availableAnimatorParameters.Contains(hash))
                animator.SetFloat(hash, val);
        }

        private void SetParameterBool(int hash, bool val)
        {
            if (_availableAnimatorParameters.Contains(hash))
                animator.SetBool(hash, val);
        }

        private void SetParameterInt(int hash, int val)
        {
            if (_availableAnimatorParameters.Contains(hash))
                animator.SetInteger(hash, val);
        }

        /// <summary>
        /// Allows external systems (e.g. special abilities or cutscenes) to request a specific animation state,
        /// validating it against controller rules before playing.
        /// </summary>
        public void PlayAnimation(PlayerAnimationType state, float customCrossfade = -1f)
        {
            PlayerAnimationType validated = ValidateAnimationRules(state);
            float duration = (customCrossfade >= 0f) ? customCrossfade : defaultCrossfadeDuration;

            currentAnimationState = validated;
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                if (_stateNameToHash.TryGetValue(validated, out int stateHash))
                {
                    animator.CrossFade(stateHash, duration);
                }
            }
        }
    }
}
