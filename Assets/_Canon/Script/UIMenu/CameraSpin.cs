using UnityEngine;

/// <summary>
/// Orbits the camera clockwise (seen from above) around a target while keeping it in view.
/// Attach to the Camera. Works with no target assigned (it simply does nothing).
/// </summary>
public class CameraSpin : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Tooltip("Offset from the target's position. Added on top of the orbit, so you can raise the focus point.")]
    [SerializeField] private Vector3 focusOffset = new Vector3(0f, 1f, 0f);

    [Header("Orbit")]
    [Tooltip("Horizontal distance from the target.")]
    [SerializeField] private float distance = 8f;

    [Tooltip("Camera height above the focus point.")]
    [SerializeField] private float height = 3f;

    [Tooltip("Degrees per second. Positive = clockwise seen from above.")]
    [SerializeField] private float spinSpeed = 20f;

    [Tooltip("Starting angle in degrees around the target.")]
    [SerializeField] private float startAngle = 0f;

    [Header("Smoothing")]
    [Tooltip("How quickly the camera catches up to a moving target. 0 = instant.")]
    [SerializeField] private float followSmoothTime = 0f;

    private float angle;
    private Vector3 smoothedFocus;
    private Vector3 focusVelocity;
    private bool initialized;

    private void OnEnable()
    {
        angle = startAngle;
        initialized = false;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 focus = target.position + focusOffset;

        if (!initialized)
        {
            smoothedFocus = focus;
            initialized = true;
        }

        smoothedFocus = followSmoothTime > 0f
            ? Vector3.SmoothDamp(smoothedFocus, focus, ref focusVelocity, followSmoothTime)
            : focus;

        // Unity yaw increases clockwise when seen from above, so adding is clockwise.
        angle = Mathf.Repeat(angle + spinSpeed * Time.deltaTime, 360f);

        Quaternion rot = Quaternion.Euler(0f, angle, 0f);
        Vector3 horizontalOffset = rot * (Vector3.back * distance);

        transform.position = smoothedFocus + horizontalOffset + Vector3.up * height;
        transform.LookAt(smoothedFocus);
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        initialized = false;
    }
}
