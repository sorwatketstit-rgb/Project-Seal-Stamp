using UnityEngine;

/// <summary>
/// Put this on any object the seal is allowed to suck up.
/// Needs a Rigidbody and at least one Collider (on this object or its children).
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class Suckable : MonoBehaviour
{
    [Header("Stored item UI")]
    public string displayName = "Item";
    public Sprite icon;

    public Rigidbody Body { get; private set; }
    public Collider[] Colliders { get; private set; }

    /// <summary>Largest side of the combined collider bounds, measured at Awake.</summary>
    public float Size { get; private set; }

    public bool IsStored { get; private set; }

    /// <summary>The mouth ignores this object until this time (stops a spat item being re-eaten).</summary>
    public float PickupBlockedUntil { get; private set; }

    void Awake()
    {
        Body = GetComponent<Rigidbody>();
        Colliders = GetComponentsInChildren<Collider>();

        if (Colliders.Length > 0)
        {
            Bounds b = Colliders[0].bounds;
            for (int i = 1; i < Colliders.Length; i++)
                b.Encapsulate(Colliders[i].bounds);
            Size = Mathf.Max(b.size.x, b.size.y, b.size.z);
        }
    }

    /// <summary>Called by SealStorage when the seal swallows this object.</summary>
    public void Store()
    {
        IsStored = true;
        ResetMotion();
        gameObject.SetActive(false);
    }

    /// <summary>Called by SealStorage when the seal spits this object out.</summary>
    public void Release(Vector3 position, Quaternion rotation, float pickupBlockTime)
    {
        IsStored = false;
        PickupBlockedUntil = Time.time + pickupBlockTime;
        transform.SetPositionAndRotation(position, rotation);
        gameObject.SetActive(true);
        ResetMotion();
    }

    void ResetMotion()
    {
        if (Body == null) return;
#if UNITY_6000_0_OR_NEWER
        Body.linearVelocity = Vector3.zero;
#else
        Body.velocity = Vector3.zero;
#endif
        Body.angularVelocity = Vector3.zero;
    }
}
