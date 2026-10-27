using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Cone suction + air meter. Does nothing until the state machine sets Active = true.
/// Detection: OverlapSphere + angle check. Pull: ONE merged force toward the mouth and
/// toward the nearest point on the center line, with distance falloff, applied in FixedUpdate.
/// </summary>
public class SealSuction : MonoBehaviour
{
    [SerializeField] Transform mouth;

    [Header("Cone")]
    [SerializeField] float range = 8f;
    [SerializeField, Range(5f, 120f)] float coneAngle = 45f;
    [SerializeField] LayerMask suckableMask = ~0;

    [Header("Force (ForceMode.Acceleration)")]
    [SerializeField] float maxForce = 40f;      // strength right at the mouth
    [SerializeField] float minForce = 8f;       // strength at the edge of range
    [SerializeField] float lineStrength = 1f;   // how hard objects are funneled toward the center line

    [Header("Limits (not decided yet - placeholders)")]
    [SerializeField] float maxMass = 20f;
    [SerializeField] float maxSize = 3f;

    [Header("Air")]
    [SerializeField] float maxAir = 5f;         // 5 bars
    [SerializeField] float drainPerSecond = 1f; // -1 bar per second while sucking
    [SerializeField] float refillTime = 2.5f;   // empty -> full takes this long once stopped
    [SerializeField] float minAirToStart = 1f;  // need at least this much to start (avoids flicker)

    public bool Active { get; set; }
    public float Air { get; private set; }
    public float Air01 => maxAir <= 0f ? 0f : Air / maxAir;
    public bool CanStart => Air >= minAirToStart;

    readonly Collider[] buffer = new Collider[64];
    readonly HashSet<Suckable> processed = new HashSet<Suckable>();
    readonly List<Rigidbody> pulled = new List<Rigidbody>();

    void Awake()
    {
        Air = maxAir;
    }

    void OnDisable()
    {
        Active = false;
        pulled.Clear();
    }

    void Update()
    {
        if (Active)
            Air = Mathf.Max(0f, Air - drainPerSecond * Time.deltaTime);
        else
            Air = Mathf.Min(maxAir, Air + (maxAir / refillTime) * Time.deltaTime);
    }

    void FixedUpdate()
    {
        pulled.Clear();
        if (!Active) return;

        Vector3 origin = mouth.position;
        Vector3 forward = mouth.forward;
        float halfAngle = coneAngle * 0.5f;

        int count = Physics.OverlapSphereNonAlloc(
            origin, range, buffer, suckableMask, QueryTriggerInteraction.Ignore);

        processed.Clear();

        for (int i = 0; i < count; i++)
        {
            Suckable s = buffer[i].GetComponentInParent<Suckable>();
            if (s == null || s.IsStored) continue;
            if (!processed.Add(s)) continue; // compound objects have several colliders

            Rigidbody rb = s.Body;
            if (rb == null || rb.isKinematic) continue;
            if (rb.mass > maxMass || s.Size > maxSize) continue;

            Vector3 center = rb.worldCenterOfMass;
            Vector3 toTarget = center - origin;
            float dist = toTarget.magnitude;
            if (dist < 0.0001f) continue;

            if (Vector3.Angle(forward, toTarget) > halfAngle) continue;

            float t = Mathf.Clamp01(dist / range);
            float strength = Mathf.Lerp(maxForce, minForce, t);

            // Merged force: toward the mouth + toward the closest point on the center line.
            // Line pull is strongest far from the mouth and fades as the object gets close.
            Vector3 toMouth = -toTarget / dist;
            Vector3 linePoint = origin + forward * Vector3.Dot(toTarget, forward);
            Vector3 toLine = linePoint - center;
            float lineDist = toLine.magnitude;
            Vector3 toLineDir = lineDist > 0.001f ? toLine / lineDist : Vector3.zero;

            Vector3 dir = toMouth + toLineDir * (lineStrength * t);
            rb.AddForce(dir * strength, ForceMode.Acceleration);

            pulled.Add(rb);
        }
    }

    void OnDrawGizmosSelected()
    {
        Transform m = mouth != null ? mouth : transform;
        Vector3 o = m.position;
        Vector3 f = m.forward;
        Vector3 r = m.right;
        Vector3 u = m.up;

        // Center line
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(o, o + f * range);

        // Cone: rim circle + spokes
        float half = coneAngle * 0.5f * Mathf.Deg2Rad;
        float rimRadius = Mathf.Sin(half) * range;
        Vector3 rimCenter = o + f * (Mathf.Cos(half) * range);

        Gizmos.color = Color.cyan;
        const int segments = 24;
        Vector3 prev = Vector3.zero;
        for (int i = 0; i <= segments; i++)
        {
            float a = (i / (float)segments) * Mathf.PI * 2f;
            Vector3 p = rimCenter + (r * Mathf.Cos(a) + u * Mathf.Sin(a)) * rimRadius;
            if (i > 0) Gizmos.DrawLine(prev, p);
            if (i % 6 == 0) Gizmos.DrawLine(o, p);
            prev = p;
        }

        // Max range
        Gizmos.color = new Color(1f, 1f, 1f, 0.15f);
        Gizmos.DrawWireSphere(o, range);

        // Runtime: lines to objects currently being pulled
        if (Application.isPlaying)
        {
            Gizmos.color = Color.green;
            foreach (Rigidbody rb in pulled)
                if (rb != null) Gizmos.DrawLine(o, rb.worldCenterOfMass);
        }
    }
}
