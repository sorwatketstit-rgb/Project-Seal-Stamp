using UnityEngine;
using UnityEngine.Events;
using SM64;

/// <summary>
/// Put this on any object the player can interact with.
/// Needs: a SphereCollider (set to trigger, auto-added) and the object on the "Interactable" layer.
/// The player must have the "Player" tag and an SM64PlayerController component (SM64 namespace).
/// </summary>
[RequireComponent(typeof(SphereCollider))]
public class Interactable : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] private float interactRadius = 2f;
    [Tooltip("Point the camera line-of-sight check aims at. Falls back to this transform.")]
    [SerializeField] private Transform lineOfSightTarget;

    [Header("Events")]
    public UnityEvent onInteract;

    [Header("Floaty UI (placeholder)")]
    [SerializeField] private GameObject promptUI;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private SphereCollider sphere;
    private int playersInRange;

    /// <summary>True while a Player-tagged object with SM64PlayerController is inside this object's sphere.</summary>
    public bool PlayerInRange => playersInRange > 0;

    public Vector3 LineOfSightPoint => lineOfSightTarget != null ? lineOfSightTarget.position : transform.position;

    private void Reset()
    {
        sphere = GetComponent<SphereCollider>();
        sphere.isTrigger = true;
        sphere.radius = interactRadius;
    }

    private void Awake()
    {
        sphere = GetComponent<SphereCollider>();
        sphere.isTrigger = true;
        sphere.radius = interactRadius;
        HidePrompt();
    }

    private void OnValidate()
    {
        if (sphere == null) sphere = GetComponent<SphereCollider>();
        if (sphere != null) sphere.radius = interactRadius;
    }

    private void OnTriggerEnter(Collider other)
    {
        bool tagOk = other.CompareTag("Player");
        bool controllerOk = other.GetComponentInParent<SM64PlayerController>() != null;

        if (debugLogs)
        {
            Debug.Log($"[Interactable:{name}] '{other.name}' entered my range. " +
                      $"Tag='{other.tag}' (Player tag: {tagOk}), has SM64PlayerController: {controllerOk}", this);
        }

        if (tagOk && controllerOk)
        {
            playersInRange++;
            if (debugLogs) Debug.Log($"[Interactable:{name}] Player registered in range.", this);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (debugLogs) Debug.Log($"[Interactable:{name}] '{other.name}' left my range.", this);

        if (IsPlayer(other))
        {
            playersInRange = Mathf.Max(0, playersInRange - 1);
            if (playersInRange == 0) HidePrompt();
        }
    }

    private static bool IsPlayer(Collider other)
    {
        return other.CompareTag("Player") && other.GetComponentInParent<SM64PlayerController>() != null;
    }

    /// <summary>Called by PlayerInteractor when E is pressed.</summary>
    public void Interact()
    {
        if (!PlayerInRange)
        {
            if (debugLogs) Debug.LogWarning($"[Interactable:{name}] Interact() called but player is not in my range.", this);
            return;
        }
        onInteract?.Invoke();
    }

    // ---- Floaty UI placeholder ----
    // Replace the body with your real UI (world-space canvas, fade, bobbing, etc).
    public void ShowPrompt()
    {
        if (promptUI != null && !promptUI.activeSelf) promptUI.SetActive(true);
    }

    public void HidePrompt()
    {
        if (promptUI != null && promptUI.activeSelf) promptUI.SetActive(false);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }
}