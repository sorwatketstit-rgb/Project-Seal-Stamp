using TMPro;
using UnityEngine;

namespace SM64
{
    /// <summary>
    /// Displays the collected mail letter count on a TextMeshPro UI component.
    /// Listens to CollectableManager.OnLetterCountChanged for updates.
    ///
    /// Setup: Canvas > LetterCountUI (GameObject with this script + TextMeshProUGUI)
    /// Display format: "Letters: 3" (configurable prefix)
    /// </summary>
    public class LetterCountUIDisplay : MonoBehaviour
    {
        [Header("UI Reference")]
        [Tooltip("TextMeshPro component to update. If null, searches on this GameObject.")]
        [SerializeField] private TMP_Text letterCountText;

        [Header("Display Format")]
        [SerializeField] private string prefix = "Letters: ";

        // -----------------------------------------------------------------------
        //  Unity lifecycle
        // -----------------------------------------------------------------------

        private void Awake()
        {
            if (letterCountText == null)
                letterCountText = GetComponent<TMP_Text>();
        }

        private void Start()
        {
            if (CollectableManager.Instance != null)
            {
                CollectableManager.Instance.OnLetterCountChanged += HandleLetterCountChanged;
                // Show initial value immediately
                HandleLetterCountChanged(CollectableManager.Instance.LetterCount);
            }
            else
            {
                Debug.LogWarning("[LetterCountUIDisplay] CollectableManager.Instance is null. " +
                                 "Make sure CollectableManager loads before this UI.");
                if (letterCountText != null)
                    letterCountText.text = $"{prefix}0";
            }
        }

        private void OnDestroy()
        {
            if (CollectableManager.Instance != null)
                CollectableManager.Instance.OnLetterCountChanged -= HandleLetterCountChanged;
        }

        // -----------------------------------------------------------------------
        //  Event handler
        // -----------------------------------------------------------------------

        private void HandleLetterCountChanged(int newCount)
        {
            if (letterCountText == null) return;
            letterCountText.text = $"{prefix}{newCount}";
        }
    }
}
