using TMPro;
using UnityEngine;

namespace SM64
{
    /// <summary>
    /// Displays the player's active state on a TextMeshPro UI component.
    /// Can be attached to any UI GameObject with a TMP_Text or TextMeshProUGUI.
    /// </summary>
    public class PlayerStateUIDisplay : MonoBehaviour
    {
        [Header("UI Reference")]
        [Tooltip("TextMeshPro component to update. If null, will search on this GameObject.")]
        [SerializeField] private TMP_Text stateText;

        [Header("Target Player")]
        [Tooltip("Player controller to monitor. If null, will search for one in the scene.")]
        [SerializeField] private SM64PlayerController playerController;

        [Header("Display Format")]
        [SerializeField] private string prefix = "State: ";
        [SerializeField] private bool formatReadableNames = true;

        private void Awake()
        {
            if (stateText == null)
            {
                stateText = GetComponent<TMP_Text>();
            }
        }

        private void Start()
        {
            if (playerController == null)
            {
                playerController = FindFirstObjectByType<SM64PlayerController>();
            }

            if (playerController != null && playerController.StateMachine != null)
            {
                playerController.StateMachine.OnStateChanged += HandleStateChanged;

                if (playerController.StateMachine.CurrentState != null)
                {
                    HandleStateChanged(playerController.StateMachine.CurrentState);
                }
            }
            else
            {
                Debug.LogWarning("PlayerStateUIDisplay: SM64PlayerController not found in scene.");
            }
        }

        private void OnDestroy()
        {
            if (playerController != null && playerController.StateMachine != null)
            {
                playerController.StateMachine.OnStateChanged -= HandleStateChanged;
            }
        }

        private void HandleStateChanged(IPlayerState newState)
        {
            if (stateText == null || newState == null) return;

            string rawName = newState.GetType().Name;
            string displayName = formatReadableNames ? FormatStateName(rawName) : rawName;

            stateText.text = $"{prefix}{displayName}";
        }

        private string FormatStateName(string stateClassName)
        {
            // Clean common prefixes and suffixes
            string formatted = stateClassName;
            if (formatted.StartsWith("Player"))
                formatted = formatted.Substring(6);
            if (formatted.EndsWith("State"))
                formatted = formatted.Substring(0, formatted.Length - 5);

            // Insert spaces before capital letters (e.g. "WallSlide" -> "Wall Slide")
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
