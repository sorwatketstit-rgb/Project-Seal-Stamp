using UnityEngine;

namespace SM64
{
    /// <summary>
    /// Observes and logs player state transitions to the Unity console.
    /// Strictly read-only: observes state changes via PlayerStateMachine.OnStateChanged.
    /// Has no permission or capability to modify the player state or controller.
    /// </summary>
    [DisallowMultipleComponent]
    public class StateLogger : MonoBehaviour
    {
        [Header("Target Player")]
        [Tooltip("Player controller to monitor. If unassigned, automatically finds one in the scene.")]
        [SerializeField] private SM64PlayerController playerController;

        [Header("Logging Settings")]
        [Tooltip("Format state names into clean readable text (e.g. 'Walking' instead of 'PlayerWalkingState').")]
        [SerializeField] private bool formatReadableNames = true;

        [Tooltip("Log previous state -> new state on transitions.")]
        [SerializeField] private bool showStateTransition = true;

        [Tooltip("Log the initial starting state when initialized.")]
        [SerializeField] private bool logInitialState = true;

        private IPlayerState _previousState;
        private bool _isSubscribed;

        private void Awake()
        {
            if (playerController == null)
            {
                playerController = FindFirstObjectByType<SM64PlayerController>();
            }
        }

        private void OnEnable()
        {
            SubscribeToStateMachine();
        }

        private void OnDisable()
        {
            UnsubscribeFromStateMachine();
        }

        private void Start()
        {
            // Fallback if playerController was not yet present in Awake
            if (playerController == null)
            {
                playerController = FindFirstObjectByType<SM64PlayerController>();
                SubscribeToStateMachine();
            }

            // Log starting state if already initialized and not yet logged
            if (logInitialState && playerController != null && playerController.StateMachine != null)
            {
                IPlayerState current = playerController.StateMachine.CurrentState;
                if (current != null && _previousState == null)
                {
                    LogStateChange(current, null);
                    _previousState = current;
                }
            }
        }

        private void SubscribeToStateMachine()
        {
            if (_isSubscribed) return;

            if (playerController != null && playerController.StateMachine != null)
            {
                playerController.StateMachine.OnStateChanged += HandleStateChanged;
                _isSubscribed = true;
            }
        }

        private void UnsubscribeFromStateMachine()
        {
            if (!_isSubscribed) return;

            if (playerController != null && playerController.StateMachine != null)
            {
                playerController.StateMachine.OnStateChanged -= HandleStateChanged;
            }
            _isSubscribed = false;
        }

        private void HandleStateChanged(IPlayerState newState)
        {
            if (newState == null) return;

            LogStateChange(newState, _previousState);
            _previousState = newState;
        }

        private void LogStateChange(IPlayerState newState, IPlayerState oldState)
        {
            string newName = GetStateName(newState);

            if (oldState != null && showStateTransition)
            {
                string oldName = GetStateName(oldState);
                Debug.Log($"[StateLogger] Player state changed: {oldName} -> {newName}", this);
            }
            else
            {
                Debug.Log($"[StateLogger] Player state: {newName}", this);
            }
        }

        private string GetStateName(IPlayerState state)
        {
            if (state == null) return "None";

            string rawName = state.GetType().Name;
            return formatReadableNames ? FormatStateName(rawName) : rawName;
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
    }
}

