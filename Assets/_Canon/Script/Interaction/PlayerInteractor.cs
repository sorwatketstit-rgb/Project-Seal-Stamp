using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Put this on the player. Finds the closest Interactable the player is in range of,
/// shows its floaty prompt automatically, and triggers it when E is pressed.
/// </summary>
public class PlayerInteractor : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] private float detectionRadius = 3f;
    [SerializeField] private LayerMask interactableLayer; // set to "Interactable"
    [SerializeField] private Transform detectionCenter;   // optional, defaults to this transform

    [Header("Input")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private readonly Collider[] hits = new Collider[16];
    private readonly HashSet<Collider> previousHits = new HashSet<Collider>();
    private readonly HashSet<Collider> currentHits = new HashSet<Collider>();
    private Interactable current;

    public Interactable Current => current;

    private void Awake()
    {
        if (detectionCenter == null) detectionCenter = transform;

        if (debugLogs && interactableLayer.value == 0)
        {
            Debug.LogError("[PlayerInteractor] interactableLayer is set to NOTHING. Assign the Interactable layer in the inspector!", this);
        }
    }

    private void Update()
    {
        Interactable best = FindBestInteractable();

        // Manage prompt visibility based strictly on whether a valid target is in range
        if (best != current)
        {
            if (current != null) current.HidePrompt();
            current = best;
            if (current != null) current.ShowPrompt();
        }

        if (Input.GetKeyDown(interactKey))
            TryInteract();
    }

    private void TryInteract()
    {
        if (current != null)
        {
            if (debugLogs) Debug.Log($"[PlayerInteractor] SUCCESS: interacted with '{current.name}'.", this);
            current.Interact();
            return;
        }

        if (!debugLogs) return;

        // Work out why nothing was interacted with.
        if (interactableLayer.value == 0)
        {
            Debug.LogWarning("[PlayerInteractor] FAIL: interactableLayer mask is empty (Nothing).", this);
            return;
        }

        int count = Physics.OverlapSphereNonAlloc(
            detectionCenter.position, detectionRadius, hits,
            interactableLayer, QueryTriggerInteraction.Collide);

        if (count == 0)
        {
            Debug.LogWarning("[PlayerInteractor] FAIL: nothing on the Interactable layer is inside my detection sphere. " +
                             "(Check the object's layer, that it has a collider, and the detection radius.)", this);
            return;
        }

        for (int i = 0; i < count; i++)
        {
            Collider col = hits[i];
            Interactable candidate = col.GetComponentInParent<Interactable>();

            if (candidate == null)
            {
                Debug.LogWarning($"[PlayerInteractor] FAIL: '{col.name}' is on the Interactable layer but has no Interactable script.", col);
                continue;
            }

            if (!candidate.PlayerInRange)
            {
                Debug.LogWarning($"[PlayerInteractor] FAIL: player is not in range of '{candidate.name}'s own sphere " +
                                 "(its trigger didn't register me: check Player tag, a Rigidbody/CharacterController on the player, and its radius).", candidate);
                continue;
            }

            Debug.LogWarning($"[PlayerInteractor] FAIL: '{candidate.name}' passed all checks but wasn't selected (unexpected).", candidate);
        }
    }

    private Interactable FindBestInteractable()
    {
        int count = Physics.OverlapSphereNonAlloc(
            detectionCenter.position, detectionRadius, hits,
            interactableLayer, QueryTriggerInteraction.Collide);

        LogRangeChanges(count);

        Interactable best = null;
        float bestDist = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            Interactable candidate = hits[i].GetComponentInParent<Interactable>();
            if (candidate == null || !candidate.PlayerInRange) continue;

            float dist = (candidate.transform.position - detectionCenter.position).sqrMagnitude;
            if (dist < bestDist)
            {
                bestDist = dist;
                best = candidate;
            }
        }

        return best;
    }

    private void LogRangeChanges(int count)
    {
        if (!debugLogs) return;

        currentHits.Clear();
        for (int i = 0; i < count; i++) currentHits.Add(hits[i]);

        foreach (Collider c in currentHits)
        {
            if (!previousHits.Contains(c))
                Debug.Log($"[PlayerInteractor] '{c.name}' entered my interaction range " +
                          $"(layer: {LayerMask.LayerToName(c.gameObject.layer)}, has Interactable: {c.GetComponentInParent<Interactable>() != null}).", c);
        }

        foreach (Collider c in previousHits)
        {
            if (c != null && !currentHits.Contains(c))
                Debug.Log($"[PlayerInteractor] '{c.name}' left my interaction range.", c);
        }

        previousHits.Clear();
        foreach (Collider c in currentHits) previousHits.Add(c);
    }

    private void OnDisable()
    {
        if (current != null) current.HidePrompt();
        current = null;
    }

    private void OnDrawGizmosSelected()
    {
        Transform c = detectionCenter != null ? detectionCenter : transform;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(c.position, detectionRadius);
    }
}