
using UnityEngine;

public class InteractUIWobble : MonoBehaviour
{
    [Header("Camera Settings")]
    [SerializeField] private Camera targetCamera;

    [Header("Rotation Settings")]
    [SerializeField] private bool rotateX = true;
    [SerializeField] private bool rotateY = false;
    [SerializeField] private bool rotateZ = true;
    [SerializeField] private float rotationSpeed = 10f;

    [Header("Wobble Settings")]
    [SerializeField] private float amplitude = 0.25f;
    [SerializeField] private float frequency = 2f;

    private Vector3 startPosition;

    private void Start()
    {
        startPosition = transform.position;

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
    }

    private void LateUpdate()
    {
        // Rotate toward the camera
        if (targetCamera != null)
        {
            Vector3 direction =
                targetCamera.transform.position - transform.position;

            Quaternion targetRotation = Quaternion.LookRotation(direction);

            Vector3 targetEuler = targetRotation.eulerAngles;
            Vector3 currentEuler = transform.rotation.eulerAngles;

            Vector3 desiredEuler = new Vector3(
                rotateX ? targetEuler.x : currentEuler.x,
                rotateY ? targetEuler.y : currentEuler.y,
                rotateZ ? targetEuler.z : currentEuler.z
            );

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.Euler(desiredEuler),
                rotationSpeed * Time.deltaTime
            );
        }

        // Wobble up and down
        float offset = Mathf.Sin(Time.time * frequency * Mathf.PI * 2f)
                       * amplitude;

        transform.position = startPosition + Vector3.up * offset;
    }
}
