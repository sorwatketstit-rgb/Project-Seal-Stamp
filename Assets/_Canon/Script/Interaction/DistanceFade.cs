using UnityEngine;

/// <summary>
/// Controls the opacity/alpha of a UI Canvas based on distance to the player.
/// Features:
/// - Calculates a rotating line from this object/canvas toward the player when in range.
/// - The line becomes shorter as the player gets closer.
/// - The longer the line's distance, the less opacity the UI has (fades out at distance).
/// - Has configurable minAlpha and maxAlpha thresholds.
/// - Controls opacity via CanvasGroup (auto-added if missing).
/// - Draws the rotating line and range sphere in the Scene view via Gizmos (and optional Debug.DrawLine in Game view).
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class DistanceFade : MonoBehaviour
{
    [Header("Player Target")]
    [Tooltip("Target player transform. Auto-finds object tagged 'Player' or SM64PlayerController if left empty.")]
    [SerializeField] private Transform targetPlayer;

    [Tooltip("Point from which distance is measured. If null, uses this transform.")]
    [SerializeField] private Transform measurementOrigin;

    [Header("Distance Range")]
    [Tooltip("The maximum range of the line. At or beyond this distance, opacity is minAlpha.")]
    [SerializeField] private float maxDistance = 10f;

    [Tooltip("Distance at or closer than which opacity is maxAlpha.")]
    [SerializeField] private float minDistance = 2f;

    [Header("Alpha / Opacity Limits")]
    [Range(0f, 1f)]
    [Tooltip("Minimum opacity when player is at max distance or beyond.")]
    [SerializeField] private float minAlpha = 0f;

    [Range(0f, 1f)]
    [Tooltip("Maximum opacity when player is closest to the object.")]
    [SerializeField] private float maxAlpha = 1f;

    [Header("Smoothing")]
    [Tooltip("If true, smoothly interpolates alpha changes. If false, updates immediately.")]
    [SerializeField] private bool smoothFade = true;

    [Tooltip("Speed of alpha interpolation when smoothFade is enabled.")]
    [SerializeField] private float fadeSpeed = 8f;

    [Header("Line Visualization")]
    [Tooltip("Draws the rotating line and range sphere in Scene view via Gizmos.")]
    [SerializeField] private bool showGizmoLine = true;

    [Tooltip("Draws the rotating line in Game view via Debug.DrawLine.")]
    [SerializeField] private bool showDebugLineInGame = false;

    [Tooltip("Optional LineRenderer if you want a visible in-game line effect.")]
    [SerializeField] private LineRenderer optionalLineRenderer;

    [SerializeField] private Color gizmoLineColor = Color.cyan;
    [SerializeField] private Color gizmoRangeColor = new Color(0f, 1f, 1f, 0.15f);

    private CanvasGroup canvasGroup;
    private float currentLineDistance;
    private bool playerInRange;

    public float CurrentDistance => currentLineDistance;
    public bool PlayerInRange => playerInRange;
    public float CurrentAlpha => canvasGroup != null ? canvasGroup.alpha : 0f;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        FindPlayer();
        UpdateFade(true);
    }

    private void OnEnable()
    {
        FindPlayer();
        UpdateFade(true);
    }

    private void Update()
    {
        FindPlayer();
        UpdateFade(false);
    }

    private void FindPlayer()
    {
        if (targetPlayer != null) return;

        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        {
            targetPlayer = playerObj.transform;
            return;
        }

        SM64.SM64PlayerController controller = FindFirstObjectByType<SM64.SM64PlayerController>();
        if (controller != null)
        {
            targetPlayer = controller.transform;
        }
    }

    private void UpdateFade(bool instant)
    {
        if (canvasGroup == null) return;

        Vector3 origin = measurementOrigin != null ? measurementOrigin.position : transform.position;

        if (targetPlayer == null)
        {
            canvasGroup.alpha = minAlpha;
            playerInRange = false;
            UpdateLineRenderer(false, origin, origin);
            return;
        }

        Vector3 toPlayer = targetPlayer.position - origin;
        currentLineDistance = toPlayer.magnitude;

        float targetAlpha;

        if (currentLineDistance >= maxDistance)
        {
            // Outside range: line does not reach player, opacity is minimum
            playerInRange = false;
            targetAlpha = minAlpha;
            UpdateLineRenderer(false, origin, origin);
        }
        else
        {
            // Inside range: line reaches player (becomes shorter as player gets closer)
            playerInRange = true;

            // The longer the line's distance is, the less opacity the UI has
            float t = Mathf.InverseLerp(maxDistance, minDistance, currentLineDistance);
            targetAlpha = Mathf.Lerp(minAlpha, maxAlpha, t);

            if (showDebugLineInGame)
            {
                Debug.DrawLine(origin, targetPlayer.position, gizmoLineColor);
            }

            UpdateLineRenderer(true, origin, targetPlayer.position);
        }

        if (instant || !smoothFade)
        {
            canvasGroup.alpha = targetAlpha;
        }
        else
        {
            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, targetAlpha, fadeSpeed * Time.deltaTime);
        }
    }

    private void UpdateLineRenderer(bool active, Vector3 start, Vector3 end)
    {
        if (optionalLineRenderer == null) return;

        optionalLineRenderer.enabled = active;
        if (active)
        {
            optionalLineRenderer.positionCount = 2;
            optionalLineRenderer.SetPosition(0, start);
            optionalLineRenderer.SetPosition(1, end);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 origin = measurementOrigin != null ? measurementOrigin.position : transform.position;

        // Draw max range wire sphere
        Gizmos.color = gizmoRangeColor;
        Gizmos.DrawWireSphere(origin, maxDistance);

        // Draw min distance wire sphere (full opacity radius)
        Gizmos.color = new Color(0f, 1f, 0f, 0.2f);
        Gizmos.DrawWireSphere(origin, minDistance);

        if (!showGizmoLine) return;

        Transform p = targetPlayer;
        if (p == null)
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null) p = playerObj.transform;
        }

        if (p != null)
        {
            float dist = Vector3.Distance(origin, p.position);
            if (dist <= maxDistance)
            {
                // Line rotates toward player and shortens as player gets closer
                float t = Mathf.InverseLerp(maxDistance, minDistance, dist);
                Gizmos.color = Color.Lerp(Color.red, Color.green, t);
                Gizmos.DrawLine(origin, p.position);
                Gizmos.DrawWireSphere(p.position, 0.15f);
            }
        }
    }
}

