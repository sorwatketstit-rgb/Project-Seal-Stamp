using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Holds the one object the seal has swallowed, and handles spitting it back out.
/// Put this on the seal root (it collects the seal's colliders to briefly ignore them on spit).
/// </summary>
public class SealStorage : MonoBehaviour
{
    [SerializeField] Transform mouth;

    [Header("Spit")]
    [SerializeField] float spitForce = 15f;            // VelocityChange, so mass doesn't matter
    [SerializeField] float spitSpawnOffset = 0.6f;     // how far in front of the mouth it appears
    [SerializeField] float pickupBlockTime = 0.75f;    // mouth ignores the item for this long
    [SerializeField] float sealCollisionIgnoreTime = 0.4f;

    public Suckable Stored { get; private set; }
    public bool HasItem => Stored != null;

    public event Action<Suckable> ItemStored;
    public event Action<Suckable> ItemSpat;

    Collider[] sealColliders;

    void Awake()
    {
        sealColliders = GetComponentsInChildren<Collider>(true);
    }

    public bool Store(Suckable item)
    {
        if (HasItem || item == null || item.IsStored) return false;

        Stored = item;
        item.Store();
        ItemStored?.Invoke(item);
        return true;
    }

    public void Spit()
    {
        if (!HasItem) return;

        Suckable item = Stored;
        Stored = null;

        Vector3 pos = mouth.position + mouth.forward * spitSpawnOffset;
        item.Release(pos, mouth.rotation, pickupBlockTime);

        IgnoreSealTemporarily(item.Colliders);
        item.Body.AddForce(mouth.forward * spitForce, ForceMode.VelocityChange);

        ItemSpat?.Invoke(item);
    }

    void IgnoreSealTemporarily(Collider[] itemColliders)
    {
        SetIgnore(itemColliders, true);
        StartCoroutine(RestoreCollision(itemColliders));
    }

    IEnumerator RestoreCollision(Collider[] itemColliders)
    {
        yield return new WaitForSeconds(sealCollisionIgnoreTime);
        SetIgnore(itemColliders, false);
    }

    void SetIgnore(Collider[] itemColliders, bool ignore)
    {
        foreach (Collider a in itemColliders)
        {
            if (a == null) continue;
            foreach (Collider b in sealColliders)
            {
                if (b == null) continue;
                Physics.IgnoreCollision(a, b, ignore);
            }
        }
    }
}
