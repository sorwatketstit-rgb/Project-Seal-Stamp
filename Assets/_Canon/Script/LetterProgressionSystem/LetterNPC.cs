using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Add this next to NPCInteractable on each customer.
/// When the player finishes the NPC's dialogue (reaches the last line or a Stop line,
/// NOT walking away mid-talk), the letter counts as delivered.
/// Also exposes the minimap circle info.
/// </summary>
[RequireComponent(typeof(NPCInteractable))]
public class LetterNPC : MonoBehaviour
{
    /// <summary>All customers in the loaded scene. The minimap reads this.</summary>
    public static readonly List<LetterNPC> All = new List<LetterNPC>();

    [SerializeField] private LetterNPCData data;

    [Header("Minimap circle")]
    [Tooltip("Centre of the circle. Leave empty to use this NPC's position. Move it a little off the NPC so the circle doesn't point at them exactly.")]
    [SerializeField] private Transform mapCircleCenter;
    [SerializeField] private float mapCircleRadius = 25f;

    [Header("Delivery trigger")]
    [Tooltip("ON: delivers automatically when the dialogue is finished.\nOFF: only delivers when you call DeliverLetter() yourself (e.g. from a UnityEvent).")]
    [SerializeField] private bool deliverOnDialogueEnd = true;

    [Header("Events (stamp animation, VFX, audio, thank-you)")]
    public UnityEvent onDelivered;

    private NPCInteractable interactable;

    public LetterNPCData Data => data;
    public bool IsDelivered => LetterDeliveryManager.Instance != null && LetterDeliveryManager.Instance.IsDelivered(data);

    /// <summary>Minimap draws the circle only while this is true. Secret NPCs never get one.</summary>
    public bool ShouldShowMapCircle => data != null && !data.isSecret && !IsDelivered;
    public Vector3 MapCircleWorldCenter => mapCircleCenter != null ? mapCircleCenter.position : transform.position;
    public float MapCircleRadius => mapCircleRadius;

    private void Awake()
    {
        interactable = GetComponent<NPCInteractable>();
        interactable.onDialogueEnd.AddListener(HandleDialogueEnd);
    }

    private void OnEnable() { All.Add(this); }
    private void OnDisable() { All.Remove(this); }

    private void OnDestroy()
    {
        if (interactable != null) interactable.onDialogueEnd.RemoveListener(HandleDialogueEnd);
        if (LetterDeliveryManager.Instance != null)
            LetterDeliveryManager.Instance.OnProgressReset -= HandleProgressReset;
    }

    // After a reset, make the NPC play its first dialogue again and be deliverable again.
    private void HandleProgressReset()
    {
        if (interactable != null) interactable.ResetInteractionState();
    }

    private void Start()
    {
        if (LetterDeliveryManager.Instance != null)
            LetterDeliveryManager.Instance.OnProgressReset += HandleProgressReset;

        // Already delivered in an earlier session: play the "again" dialogue and show the happy state.
        if (IsDelivered)
        {
            interactable.HasInteracted = true;
            onDelivered?.Invoke();
        }
    }

    // onDialogueEnd also fires when the player just walks away. In that case HasInteracted
    // is still false, because NPCInteractable only sets it when the dialogue actually finishes.
    private void HandleDialogueEnd()
    {
        if (!deliverOnDialogueEnd) return;
        if (!interactable.HasInteracted) return;
        TryDeliver();
    }

    /// <summary>
    /// UnityEvent-friendly (returns void so it shows up in the Inspector dropdown).
    /// Call it from NPCInteractable's onBeginDialogue / onDialogueStart / onDialogueLineChanged,
    /// or any other UnityEvent. Marks this customer as delivered, which fills the stamp UI.
    /// Safe to call more than once: only the first call counts.
    /// </summary>
    public void DeliverLetter()
    {
        TryDeliver();
    }

    public bool TryDeliver()
    {
        var manager = LetterDeliveryManager.Instance;
        if (manager == null || data == null) return false;

        bool newDelivery = manager.TryDeliver(data);
        if (newDelivery) onDelivered?.Invoke();
        return newDelivery;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 c = mapCircleCenter != null ? mapCircleCenter.position : transform.position;
        UnityEditor.Handles.color = Color.cyan;
        UnityEditor.Handles.DrawWireDisc(c, Vector3.up, mapCircleRadius);
    }
#endif
}