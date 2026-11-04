using UnityEngine;
using UnityEngine.UI;

namespace SM64
{
    /// <summary>
    /// Put this on any UI Button GameObject to make it act as an Unstuck button.
    /// Automatically connects to button.onClick or can be invoked via OnClick().
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class UnstuckButton : MonoBehaviour
    {
        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
            if (_button != null)
            {
                _button.onClick.AddListener(OnButtonClicked);
            }
        }

        private void OnDestroy()
        {
            if (_button != null)
            {
                _button.onClick.RemoveListener(OnButtonClicked);
            }
        }

        public void OnButtonClicked()
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

