using UnityEngine;
using UnityEngine.UI;

namespace SM64
{
    /// <summary>
    /// Helper UI component for the Unstuck button.
    /// Can be attached to a Menu canvas or HUD.
    /// Wires directly to an existing Button or can be hooked to Unity UI events.
    /// </summary>
    public class UnstuckUI : MonoBehaviour
    {
        [Header("Button Reference")]
        [Tooltip("The UI Button that triggers unstuck. If unassigned, attempts to find Button on this GameObject.")]
        [SerializeField] private Button unstuckButton;

        private void Awake()
        {
            if (unstuckButton == null)
            {
                unstuckButton = GetComponent<Button>();
            }

            if (unstuckButton != null)
            {
                unstuckButton.onClick.AddListener(OnUnstuckClicked);
            }
        }

        private void OnDestroy()
        {
            if (unstuckButton != null)
            {
                unstuckButton.onClick.RemoveListener(OnUnstuckClicked);
            }
        }

        /// <summary>
        /// Triggered when the unstuck button is clicked.
        /// </summary>
        public void OnUnstuckClicked()
        {
            if (PlayerRespawnManager.Instance != null)
            {
                PlayerRespawnManager.Instance.Unstuck();
            }
            else
            {
                SM64PlayerController controller = FindFirstObjectByType<SM64PlayerController>();
                if (controller != null)
                {
                    controller.Respawn();
                }
            }
        }
    }
}

