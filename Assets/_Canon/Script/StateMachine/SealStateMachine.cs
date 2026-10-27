using UnityEngine;

/// <summary>
/// Seal ability component. Kept under its old name so the scene reference on Player stays intact.
///
/// It no longer decides WHEN to suck or spit: PlayerSealState / PlayerSealSuckingState do that.
/// This component owns the ability's data and timers: suction on/off, the suction particle object,
/// the post-store lockout, the spit cooldown, and the seal's nudge-movement settings.
/// Put this on the Player root.
/// </summary>
public class SealStateMachine : MonoBehaviour
{
    [Header("References")]
    [SerializeField] SealSuction suction;
    [SerializeField] SealStorage storage;
    [SerializeField] SealMouth mouth;

    [Header("Suction VFX")]
    [Tooltip("Object enabled while sucking and disabled when not. Leave empty for no particle.")]
    [SerializeField] GameObject suctionParticle;

    [Header("Timings")]
    [SerializeField] float lockoutDuration = 2f;   // Shift disabled this long after storing an object
    [SerializeField] float spitDuration = 0.25f;   // Shift disabled this long after spitting

    [Header("Seal Movement (nudge)")]
    [Tooltip("Seconds between nudges while a direction is held.")]
    [SerializeField] float nudgeInterval = 1f;
    [Tooltip("Peak speed at the start of a nudge. It fades to zero over Nudge Duration.")]
    [SerializeField] float nudgeSpeed = 6f;
    [Tooltip("How long one nudge lasts. Distance moved is roughly speed x duration / 2.")]
    [SerializeField] float nudgeDuration = 0.4f;

    public float NudgeInterval => nudgeInterval;
    public float NudgeSpeed => nudgeSpeed;
    public float NudgeDuration => nudgeDuration;

    public float LockoutRemaining { get; private set; }
    public float SpitCooldownRemaining { get; private set; }

    public bool IsLocked => LockoutRemaining > 0f;
    public bool IsSpitting => SpitCooldownRemaining > 0f;
    public bool IsSucking => suction.Active;
    public bool HasItem => storage.HasItem;
    public bool OutOfAir => suction.Air <= 0f;

    /// <summary>True when Shift may start a suck: not locked, not spitting, hands empty, enough air.</summary>
    public bool CanStartSucking => !IsLocked && !IsSpitting && !storage.HasItem && suction.CanStart;

    /// <summary>True when Shift may spit the stored item.</summary>
    public bool CanSpit => !IsLocked && !IsSpitting && storage.HasItem;

    void OnEnable()
    {
        mouth.ObjectReachedMouth += OnObjectReachedMouth;
        SetParticle(false);
    }

    void OnDisable()
    {
        mouth.ObjectReachedMouth -= OnObjectReachedMouth;
        EndSucking();
    }

    void Update()
    {
        if (LockoutRemaining > 0f)
            LockoutRemaining = Mathf.Max(0f, LockoutRemaining - Time.deltaTime);

        if (SpitCooldownRemaining > 0f)
            SpitCooldownRemaining = Mathf.Max(0f, SpitCooldownRemaining - Time.deltaTime);
    }

    /// <summary>Called by PlayerSealSuckingState.Enter.</summary>
    public void BeginSucking()
    {
        suction.Active = true;
        SetParticle(true);
    }

    /// <summary>Called whenever sucking stops (release, out of air, swallowed, leaving seal mode).</summary>
    public void EndSucking()
    {
        suction.Active = false;
        SetParticle(false);
    }

    /// <summary>Spits the stored item. Returns false if spitting is not allowed right now.</summary>
    public bool Spit()
    {
        if (!CanSpit) return false;

        EndSucking();
        storage.Spit();
        SpitCooldownRemaining = spitDuration;
        return true;
    }

    void OnObjectReachedMouth(Suckable item)
    {
        // Only swallow while actively sucking, and only one item at a time.
        if (!suction.Active || storage.HasItem) return;
        if (!storage.Store(item)) return;

        LockoutRemaining = lockoutDuration;
        EndSucking(); // PlayerSealSuckingState sees this and returns to the seal state
    }

    void SetParticle(bool on)
    {
        if (suctionParticle != null && suctionParticle.activeSelf != on)
            suctionParticle.SetActive(on);
    }
}