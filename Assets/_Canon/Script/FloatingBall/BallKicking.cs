using UnityEngine;

namespace SM64
{
    /// <summary>
    /// Player-side kick trigger. It decides WHEN to kick (input + player state) and simply asks the ball
    /// to kick via <see cref="FloatingBallController.RequestKick"/>. It never touches the ball's position —
    /// that belongs to <see cref="FloatingBallLocation"/>.
    /// Put this on the player.
    /// </summary>
    public class BallKicking : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The ball's state controller.")]
        [SerializeField] private FloatingBallController ball;

        [Tooltip("Transform whose forward direction is the kick direction. Uses this GameObject if left empty.")]
        [SerializeField] private Transform facingSource;

        [Header("Input")]
        [SerializeField] private KeyCode kickKey = KeyCode.LeftShift;

        [Tooltip("Poll the legacy Input Manager for the kick key. Turn off if you use the new Input System and call TryKick() from an input event instead.")]
        [SerializeField] private bool pollLegacyInput = true;

        /// <summary>
        /// Set this from your player controller. Kicking does nothing while running (for now).
        /// </summary>
        public bool IsRunning { get; set; }

        private void Awake()
        {
            if (facingSource == null) facingSource = transform;
        }

        private void Update()
        {
            if (pollLegacyInput && Input.GetKeyDown(kickKey))
            {
                TryKick();
            }
        }

        /// <summary>Requests a kick. Returns true if the ball accepted it.</summary>
        public bool TryKick()
        {
            if (ball == null || IsRunning) return false;
            return ball.RequestKick(facingSource.forward);
        }
    }
}
