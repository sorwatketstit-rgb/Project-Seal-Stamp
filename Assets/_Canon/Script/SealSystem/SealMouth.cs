using System;
using UnityEngine;

/// <summary>
/// Trigger collider at the seal's mouth. Reports any Suckable that touches it.
/// The state machine decides whether to actually store it (only while Sucking).
/// Put this on a child object with a Collider set to "Is Trigger".
/// The object's blue (forward) axis should point where the seal is facing.
/// </summary>
[RequireComponent(typeof(Collider))]
public class SealMouth : MonoBehaviour
{
    public event Action<Suckable> ObjectReachedMouth;

    void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    // Stay (not Enter) so an object already inside the mouth when sucking starts still counts.
    void OnTriggerStay(Collider other)
    {
        Suckable s = other.GetComponentInParent<Suckable>();
        if (s == null || s.IsStored) return;
        if (Time.time < s.PickupBlockedUntil) return;

        ObjectReachedMouth?.Invoke(s);
    }
}
