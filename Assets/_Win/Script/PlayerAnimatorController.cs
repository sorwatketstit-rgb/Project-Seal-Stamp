
using UnityEngine;

public class PlayerAnimationController : MonoBehaviour
{
    [Header("References")]
    public Animator animator;

    [Header("Movement Settings")]
    public float walkSpeed = 0.1f;
    public float runSpeed = 3f;

    private Vector3 lastPosition;

    void Start()
    {
        lastPosition = transform.position;

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        // Calculate movement speed
        float speed = (transform.position - lastPosition).magnitude
                      / Time.deltaTime;

        lastPosition = transform.position;

        // Select animation based on speed
        if (speed < walkSpeed)
        {
            animator.Play("Idle");
        }
        else if (speed < runSpeed)
        {
            animator.Play("Walk");
        }
        else
        {
            animator.Play("Run");
        }
    }
}
