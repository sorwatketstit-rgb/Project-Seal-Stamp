using UnityEngine;

public class SelfDestruct : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Time in seconds before the object disables itself.")]
    [SerializeField] private float lifetime = 3.0f;

    private void OnEnable()
    {
        // Cancel any pending invokes to prevent overlapping timers
        CancelInvoke(nameof(DisableSelf));
        
        // Schedule the object to disable after 'lifetime' seconds
        Invoke(nameof(DisableSelf), lifetime);
    }

    private void OnDisable()
    {
        // Clean up the invoke when the object is disabled manually or by the timer
        CancelInvoke(nameof(DisableSelf));
    }

    private void DisableSelf()
    {
        gameObject.SetActive(false);
    }
}