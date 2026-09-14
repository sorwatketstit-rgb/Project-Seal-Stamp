using TMPro;
using UnityEngine;

namespace SM64
{
    /// <summary>
    /// UIManager responsible for updating UI elements such as TextMeshPro (TMP) state indicators.
    /// Supports two dedicated TMP objects: Action State and Movement State.
    /// Provides global singleton access (UIManager.Instance) and public inspector fields.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("TextMeshPro UI References")]
        [Tooltip("TextMeshPro component displaying the current Action State (e.g., Long Jump, Wall Slide, Ground Pound, None).")]
        [SerializeField] public TMP_Text actionStateText;

        [Tooltip("TextMeshPro component displaying the current Movement State (e.g., Idle, Walking, Running, Airborne).")]
        [SerializeField] public TMP_Text movementStateText;

        [Header("Display Formatting")]
        [SerializeField] private bool includePrefix = true;
        [SerializeField] private string actionPrefix = "Action: ";
        [SerializeField] private string movementPrefix = "Movement: ";

        [Header("Auto Tracking")]
        [Tooltip("Automatically listens to SM64PlayerController state machine transitions.")]
        [SerializeField] private bool autoTrackPlayer = true;
        [Tooltip("Player controller to monitor. If left empty, automatically searches the scene.")]
        [SerializeField] private SM64PlayerController playerController;

        private string _lastMovementState = "";

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            if (!autoTrackPlayer) return;

            if (playerController == null)
            {
                playerController = FindFirstObjectByType<SM64PlayerController>();
            }

            if (playerController != null && playerController.StateMachine != null)
            {
                playerController.StateMachine.OnStateChanged += HandlePlayerStateChanged;

                if (playerController.StateMachine.CurrentState != null)
                {
                    HandlePlayerStateChanged(playerController.StateMachine.CurrentState);
                }
            }
            else
            {
                Debug.LogWarning("[UIManager] SM64PlayerController not found in scene. Automatic state tracking is disabled.");
            }
        }

        private void Update()
        {
            if (!autoTrackPlayer || playerController == null || playerController.StateMachine == null) return;

            // Dynamically distinguish Idle vs Walking while in WalkingState
            if (playerController.StateMachine.CurrentState is PlayerWalkingState)
            {
                float horizontalSpeed = playerController.HorizontalVelocity.magnitude;
                string dynamicMovement = horizontalSpeed > 0.15f ? "Walking" : "Idle";

                if (dynamicMovement != _lastMovementState)
                {
                    _lastMovementState = dynamicMovement;
                    SetMovementState(dynamicMovement);
                }
            }
        }

        private void OnDestroy()
        {
            if (playerController != null && playerController.StateMachine != null)
            {
                playerController.StateMachine.OnStateChanged -= HandlePlayerStateChanged;
            }
        }

        #region Public UI Update Methods

        /// <summary>
        /// Updates the Action State TextMeshPro component.
        /// Can be called directly from any script: UIManager.Instance.SetActionState("Long Jump");
        /// </summary>
        public void SetActionState(string stateName)
        {
            if (actionStateText == null) return;
            actionStateText.text = includePrefix ? $"{actionPrefix}{stateName}" : stateName;
        }

        /// <summary>
        /// Updates the Movement State TextMeshPro component.
        /// Can be called directly from any script: UIManager.Instance.SetMovementState("Running");
        /// </summary>
        public void SetMovementState(string stateName)
        {
            if (movementStateText == null) return;
            movementStateText.text = includePrefix ? $"{movementPrefix}{stateName}" : stateName;
        }

        /// <summary>
        /// Convenience method to update both Action and Movement states simultaneously.
        /// </summary>
        public void SetStates(string actionState, string movementState)
        {
            SetActionState(actionState);
            SetMovementState(movementState);
        }

        // Aliases for convenience
        public void UpdateActionState(string stateName) => SetActionState(stateName);
        public void UpdateMovementState(string stateName) => SetMovementState(stateName);

        #endregion

        #region Automatic State Mapping

        private void HandlePlayerStateChanged(IPlayerState newState)
        {
            if (newState == null) return;

            string action = "None";
            string movement = "Grounded";

            switch (newState)
            {
                case PlayerWalkingState:
                    action = "None";
                    movement = (playerController != null && playerController.HorizontalVelocity.magnitude > 0.15f) ? "Walking" : "Idle";
                    _lastMovementState = movement;
                    break;

                case PlayerRunningState:
                    action = "None";
                    movement = "Running";
                    _lastMovementState = movement;
                    break;

                case PlayerAirborneState:
                    action = "Jump";
                    movement = "Airborne";
                    _lastMovementState = movement;
                    break;

                case PlayerLongJumpState:
                    action = "Long Jump";
                    movement = "Airborne";
                    _lastMovementState = movement;
                    break;

                case PlayerBackflipState:
                    action = "Backflip";
                    movement = "Airborne";
                    _lastMovementState = movement;
                    break;

                case PlayerWallJumpState:
                    action = "Wall Jump";
                    movement = "Airborne";
                    _lastMovementState = movement;
                    break;

                case PlayerWallSlideState:
                    action = "Wall Slide";
                    movement = "Sliding";
                    _lastMovementState = movement;
                    break;

                case PlayerLedgeGrabState:
                    action = "Ledge Grab";
                    movement = "Hanging";
                    _lastMovementState = movement;
                    break;

                case PlayerGroundPoundState:
                    action = "Ground Pound";
                    movement = "Falling";
                    _lastMovementState = movement;
                    break;

                default:
                    action = FormatStateName(newState.GetType().Name);
                    movement = "Active";
                    _lastMovementState = movement;
                    break;
            }

            SetActionState(action);
            SetMovementState(movement);
        }

        private string FormatStateName(string stateClassName)
        {
            string formatted = stateClassName;
            if (formatted.StartsWith("Player"))
                formatted = formatted.Substring(6);
            if (formatted.EndsWith("State"))
                formatted = formatted.Substring(0, formatted.Length - 5);

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (int i = 0; i < formatted.Length; i++)
            {
                if (i > 0 && char.IsUpper(formatted[i]))
                {
                    sb.Append(' ');
                }
                sb.Append(formatted[i]);
            }
            return sb.ToString();
        }

        #endregion
    }
}
