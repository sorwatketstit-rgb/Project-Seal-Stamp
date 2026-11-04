using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Letters-left HUD. Each slot is a circle silhouette that turns into a stamp
/// once a customer receives their letter. Slots are anonymous (they don't map to a
/// specific NPC), the first N slots are simply filled in order.
/// </summary>
public class LetterStampUI : MonoBehaviour
{
    [Header("Slots (assign Image components, in order)")]
    [SerializeField] private List<Image> slots = new List<Image>();

    [Header("Sprites")]
    [SerializeField] private Sprite silhouetteSprite;
    [SerializeField] private Sprite stampSprite;

    [Header("Optional")]
    [SerializeField] private TMP_Text counterText; // e.g. "5 / 9"

    private LetterDeliveryManager manager;

    private void Start()
    {
        manager = LetterDeliveryManager.Instance;
        if (manager == null) { Debug.LogWarning("[LetterStampUI] No LetterDeliveryManager found.", this); return; }

        manager.OnProgressChanged += HandleProgress;
        Refresh();
    }

    private void OnDestroy()
    {
        if (manager != null) manager.OnProgressChanged -= HandleProgress;
    }

    private void HandleProgress(int delivered, int required) => Refresh();

    public void Refresh()
    {
        if (manager == null) return;

        int delivered = manager.DeliveredCount;
        int required = manager.RequiredCount;

        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] == null) continue;

            bool filled = i < delivered;
            slots[i].sprite = filled ? stampSprite : silhouetteSprite;

            // Only the required number of slots (9) are ever shown.
            slots[i].gameObject.SetActive(i < required);
        }

        if (counterText != null) counterText.text = $"{Mathf.Min(delivered, required)} / {required}";
    }
}
