using UnityEngine;

namespace SM64
{
    /// <summary>
    /// Owns ALL positional control of the floating ball.
    /// It never decides which state the ball is in — it only reads the state from
    /// <see cref="FloatingBallController"/> and moves the ball accordingly:
    /// - HasPlayer: stays in place (optional hover bobbing).
    /// - Moving:    flies in 3D toward the player, turning to face them.
    /// - Kicked:    records origin → travels to the wind-up GameObject → is shot as a physics-driven
    ///              projectile (keeps its own momentum) → arcs back to the recorded origin (facing the player).
    /// When the kick finishes it reports back to the controller, which then changes the state.
    /// </summary>
    [RequireComponent(typeof(FloatingBallController))]
    public class FloatingBallLocation : MonoBehaviour
    {
        private enum KickPhase
        {
            WindUp, // origin → wind-up GameObject
            Shoot,  // projectile flight with momentum
            Return  // arc back to origin
        }

        [Header("Targeting")]
        [Tooltip("Tag used to find the player GameObject if no target is assigned.")]
        [SerializeField] private string playerTag = "Player";

        [Tooltip("Direct reference to player transform. If left empty, will search by tag.")]
        [SerializeField] private Transform playerTarget;

        [Tooltip("Offset from the player's pivot (e.g. chest/center height).")]
        [SerializeField] private Vector3 targetOffset = new Vector3(0f, 1.2f, 0f);

        [Header("Flight Movement (Moving stage)")]
        [Tooltip("Maximum flight speed in units per second.")]
        [SerializeField] private float moveSpeed = 7f;

        [Tooltip("Acceleration / smoothing time for smooth flight damping.")]
        [SerializeField] private float smoothTime = 0.35f;

        [Tooltip("If true, uses smooth damping. If false, uses constant linear speed.")]
        [SerializeField] private bool useSmoothMovement = true;

        [Header("Facing")]
        [Tooltip("Pivot that marks the FRONT of the ball (an empty child object). The ball turns so this point faces the player while Moving and while returning from a kick. If empty, the transform's forward is used.")]
        [SerializeField] private Transform frontPoint;

        [Tooltip("How fast the ball turns toward the player, in degrees per second.")]
        [SerializeField] private float turnSpeed = 540f;

        [Header("Idle Hover / Floating VFX")]
        [SerializeField] private bool enableHoverBobbing = true;
        [SerializeField] private float hoverAmplitude = 0.2f;
        [SerializeField] private float hoverFrequency = 1.5f;

        [Header("Kick — Wind-Up (phase 1)")]
        [Tooltip("The GameObject the ball travels to before it is shot. Its position is tracked live, so it can be a child of the player (e.g. behind/above the player).")]
        [SerializeField] private Transform windUpPoint;

        [Tooltip("Time (seconds) to travel from the kick origin to the wind-up point.")]
        [SerializeField] private float windUpDuration = 0.25f;

        [Tooltip("Short pause (seconds) at the wind-up point before the ball is shot.")]
        [SerializeField] private float windUpHoldTime = 0.08f;

        [Tooltip("Only used if no Wind-Up Point is assigned: distance behind the kick origin.")]
        [SerializeField] private float fallbackWindUpBack = 2f;

        [Tooltip("Only used if no Wind-Up Point is assigned: height above the kick origin.")]
        [SerializeField] private float fallbackWindUpHeight = 1.5f;

        [Header("Kick — Projectile Shot (phase 2)")]
        [Tooltip("Initial launch speed (units/second). This becomes the ball's momentum.")]
        [SerializeField] private float shootSpeed = 25f;

        [Tooltip("Object to Enable when kick")]
        [SerializeField] private GameObject SmokeObject;

        [Tooltip("Effect to Disable when kick")]
        [SerializeField] private GameObject PropellerEffect;

        [Tooltip("Launch pitch above the horizontal, in degrees. 0 = flat shot.")]
        [SerializeField] private float shootUpAngle = 0f;

        [Tooltip("Multiplier on Physics.gravity applied while flying. 0 = no gravity (pure straight line).")]
        [SerializeField] private float shotGravityScale = 0.35f;

        [Tooltip("Air drag. Higher = momentum bleeds off faster.")]
        [SerializeField] private float shotDrag = 0.1f;

        [Tooltip("Maximum time (seconds) the ball may fly before it turns around.")]
        [SerializeField] private float maxShotTime = 0.8f;

        [Tooltip("If the ball's speed falls under this, the shot ends early.")]
        [SerializeField] private float minShotSpeed = 2f;

        [Header("Kick — Projectile Collision")]
        [Tooltip("Layers the ball bounces off during the shot. Leave on 'Nothing' to disable collision.")]
        [SerializeField] private LayerMask collisionMask = 0;

        [Tooltip("Radius used for collision checks while shooting.")]
        [SerializeField] private float ballRadius = 0.3f;

        [Tooltip("Fraction of speed kept after bouncing off something (0 = dead stop, 1 = perfect bounce).")]
        [Range(0f, 1f)]
        [SerializeField] private float shotBounciness = 0.6f;

        [Header("Kick — Arc Return (phase 3)")]
        [Tooltip("Extra height of the return arc's control point above the midpoint between shot end and origin.")]
        [SerializeField] private float returnArcHeight = 3f;

        [Tooltip("Time (seconds) the arc back to the origin takes.")]
        [SerializeField] private float returnDuration = 0.6f;

        [Tooltip("How much of the shot's leftover momentum bends the return arc (0 = ignore momentum).")]
        [SerializeField] private float returnMomentumInfluence = 0.15f;

        // References
        private FloatingBallController _controller;

        // Runtime — general
        private Vector3 _basePosition;
        private Vector3 _smoothVelocity;
        private float _hoverTimer;

        // Runtime — kick
        private bool _kickActive;
        private KickPhase _kickPhase;
        private float _phaseTimer;
        private Vector3 _kickOrigin;
        private Vector3 _kickForward;
        private Vector3 _windUpStart;
        private Vector3 _shotVelocity;
        private float _shotTimer;
        private Vector3 _returnStart;
        private Vector3 _returnControl;

        /// <summary>Un-bobbed position of the ball (read-only for other scripts).</summary>
        public Vector3 BasePosition => _basePosition;

        /// <summary>True if a player target exists.</summary>
        public bool HasTarget => playerTarget != null;

        /// <summary>World position the ball flies toward when following the player.</summary>
        public Vector3 TargetPosition => playerTarget != null ? playerTarget.position + targetOffset : _basePosition;

        /// <summary>Distance from the ball to its follow target. Infinity if there is no target.</summary>
        public float DistanceToTarget => playerTarget != null ? Vector3.Distance(_basePosition, TargetPosition) : float.PositiveInfinity;

        public Transform PlayerTarget
        {
            get => playerTarget;
            set => playerTarget = value;
        }

        private void Awake()
        {
            _controller = GetComponent<FloatingBallController>();
            _basePosition = transform.position;
            _hoverTimer = Random.Range(0f, 2f * Mathf.PI);
        }

        private void Start()
        {
            FindPlayerIfNeeded();
        }

        private void Update()
        {
            FindPlayerIfNeeded();

            // If the controller left the Kicked stage by itself, abandon the kick motion.
            if (_kickActive && _controller.CurrentStage != BallStage.Kicked)
            {
                _kickActive = false;
                _shotVelocity = Vector3.zero;
            }

            switch (_controller.CurrentStage)
            {
                case BallStage.HasPlayer:
                    _smoothVelocity = Vector3.zero;
                    break;

                case BallStage.Moving:
                    if (HasTarget) FlyTowardTarget();
                    break;

                case BallStage.Kicked:
                    if (!_kickActive) BeginKick(_controller.KickForward);
                    UpdateKick();
                    break;
            }

            ApplyHoverBobbing();
            UpdateFacing();
        }

        // ───────────────────────── Following ─────────────────────────

        private void FlyTowardTarget()
        {
            Vector3 destination = TargetPosition;

            if (useSmoothMovement)
            {
                _basePosition = Vector3.SmoothDamp(_basePosition, destination, ref _smoothVelocity, smoothTime, moveSpeed);
            }
            else
            {
                _basePosition = Vector3.MoveTowards(_basePosition, destination, moveSpeed * Time.deltaTime);
            }
        }

        // ───────────────────────── Facing ─────────────────────────

        /// <summary>
        /// Turns the ball so its front faces the player — only while Moving and while arcing back
        /// after a kick. In every other situation the rotation is left alone.
        /// </summary>
        private void UpdateFacing()
        {
            bool shouldFace =
                _controller.CurrentStage == BallStage.Moving ||
                (_controller.CurrentStage == BallStage.Kicked && _kickActive && _kickPhase == KickPhase.Return);

            if (!shouldFace || !HasTarget) return;

            Vector3 toTarget = TargetPosition - transform.position;
            if (toTarget.sqrMagnitude < 0.0001f) return;

            // Smallest rotation that swings the current front direction onto the player direction.
            Quaternion desired = Quaternion.FromToRotation(GetFrontDirection(), toTarget.normalized) * transform.rotation;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, desired, turnSpeed * Time.deltaTime);
        }

        /// <summary>World direction from the ball's pivot to its Front Point (falls back to transform.forward).</summary>
        private Vector3 GetFrontDirection()
        {
            if (frontPoint != null)
            {
                Vector3 dir = frontPoint.position - transform.position;
                if (dir.sqrMagnitude > 0.0001f) return dir.normalized;
            }
            return transform.forward;
        }

        // ───────────────────────── Kick sequence ─────────────────────────

        private void BeginKick(Vector3 forward)
        {
            _kickActive = true;
            _kickPhase = KickPhase.WindUp;
            _phaseTimer = 0f;

            _kickOrigin = _basePosition;   // record where the ball was
            _windUpStart = _basePosition;
            _kickForward = forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector3.forward;

            _smoothVelocity = Vector3.zero;
            _shotVelocity = Vector3.zero;
        }

        private void UpdateKick()
        {
            _phaseTimer += Time.deltaTime;

            switch (_kickPhase)
            {
                case KickPhase.WindUp: UpdateWindUp(); break;
                case KickPhase.Shoot:  UpdateShoot();  break;
                case KickPhase.Return: UpdateReturn(); break;
            }
        }

        private Vector3 GetWindUpPosition()
        {
            if (windUpPoint != null) return windUpPoint.position;
            return _kickOrigin - _kickForward * fallbackWindUpBack + Vector3.up * fallbackWindUpHeight;
        }

        private void UpdateWindUp()
        {
            Vector3 windUpPos = GetWindUpPosition(); // tracked live
            float t = Mathf.Clamp01(_phaseTimer / Mathf.Max(0.0001f, windUpDuration));
            float smooth = 1f - (1f - t) * (1f - t); // ease-out
            if (PropellerEffect != null) PropellerEffect.SetActive(false);

            _basePosition = t >= 1f ? windUpPos : Vector3.Lerp(_windUpStart, windUpPos, smooth);

            if (_phaseTimer >= windUpDuration + windUpHoldTime)
            {
                BeginShoot();
            }
        }

        private void BeginShoot()
        {
            float rad = shootUpAngle * Mathf.Deg2Rad;
            Vector3 dir = (_kickForward * Mathf.Cos(rad) + Vector3.up * Mathf.Sin(rad)).normalized;

            _shotVelocity = dir * shootSpeed; // initial momentum
            _shotTimer = 0f;
            _phaseTimer = 0f;
            _kickPhase = KickPhase.Shoot;
            if (SmokeObject != null) SmokeObject.SetActive(true);

            _controller.NotifyKickShot();
        }

        /// <summary>Simple projectile physics: momentum + gravity + drag + optional bounces.</summary>
        private void UpdateShoot()
        {
            float dt = Time.deltaTime;
            _shotTimer += dt;

            _shotVelocity += Physics.gravity * shotGravityScale * dt;
            _shotVelocity *= Mathf.Max(0f, 1f - shotDrag * dt);

            Vector3 step = _shotVelocity * dt;
            float stepLength = step.magnitude;

            if (stepLength > 0.0001f && collisionMask.value != 0 &&
                Physics.SphereCast(_basePosition, ballRadius, step / stepLength, out RaycastHit hit,
                                   stepLength, collisionMask, QueryTriggerInteraction.Ignore))
            {
                _basePosition += (step / stepLength) * hit.distance + hit.normal * 0.01f;
                _shotVelocity = Vector3.Reflect(_shotVelocity, hit.normal) * shotBounciness;
            }
            else
            {
                _basePosition += step;
            }

            if (_shotTimer >= maxShotTime || _shotVelocity.magnitude < minShotSpeed)
            {
                BeginReturn();
            }
        }

        private void BeginReturn()
        {
            if (PropellerEffect != null) PropellerEffect.SetActive(true);
            _returnStart = _basePosition;
            _returnControl = (_returnStart + _kickOrigin) * 0.5f
                             + Vector3.up * returnArcHeight
                             + _shotVelocity * returnMomentumInfluence;

            _phaseTimer = 0f;
            _kickPhase = KickPhase.Return;
        }

        private void UpdateReturn()
        {
            float t = Mathf.Clamp01(_phaseTimer / Mathf.Max(0.0001f, returnDuration));
            float smooth = Mathf.SmoothStep(0f, 1f, t);
            _basePosition = QuadraticBezier(_returnStart, _returnControl, _kickOrigin, smooth);

            if (t >= 1f)
            {
                _basePosition = _kickOrigin;
                _shotVelocity = Vector3.zero;
                _kickActive = false;
                if (SmokeObject != null) SmokeObject.SetActive(false);
                _controller.NotifyKickFinished(); // controller decides the next state
            }
        }

        private static Vector3 QuadraticBezier(Vector3 p0, Vector3 p1, Vector3 p2, float t)
        {
            float u = 1f - t;
            return u * u * p0 + 2f * u * t * p1 + t * t * p2;
        }

        // ───────────────────────── Misc ─────────────────────────

        private void ApplyHoverBobbing()
        {
            if (enableHoverBobbing && _controller.CurrentStage != BallStage.Kicked)
            {
                _hoverTimer += Time.deltaTime * hoverFrequency * Mathf.PI * 2f;
                transform.position = _basePosition + new Vector3(0f, Mathf.Sin(_hoverTimer) * hoverAmplitude, 0f);
            }
            else
            {
                transform.position = _basePosition;
            }
        }

        /// <summary>Teleports the ball and syncs the internal base position.</summary>
        public void SetPosition(Vector3 newPosition)
        {
            _basePosition = newPosition;
            transform.position = newPosition;
            _smoothVelocity = Vector3.zero;
            _shotVelocity = Vector3.zero;
        }

        private void FindPlayerIfNeeded()
        {
            if (playerTarget != null && playerTarget.gameObject.activeInHierarchy) return;

            if (!string.IsNullOrEmpty(playerTag))
            {
                GameObject playerObj = GameObject.FindGameObjectWithTag(playerTag);
                if (playerObj != null) playerTarget = playerObj.transform;
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // Follow target line
            if (playerTarget != null)
            {
                Vector3 center = Application.isPlaying ? _basePosition : transform.position;
                Gizmos.color = Color.gray;
                Gizmos.DrawLine(center, playerTarget.position + targetOffset);
            }

            // Front direction
            if (frontPoint != null)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(transform.position, frontPoint.position);
                Gizmos.DrawWireSphere(frontPoint.position, 0.08f);
            }

            // Wind-up point
            if (windUpPoint != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(windUpPoint.position, 0.2f);
            }

            if (Application.isPlaying && _kickActive)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(_kickOrigin, 0.2f);

                if (_kickPhase == KickPhase.Shoot)
                {
                    Gizmos.color = new Color(1f, 0.4f, 0f, 0.95f);
                    Gizmos.DrawLine(GetWindUpPosition(), _basePosition);
                    Gizmos.DrawRay(_basePosition, _shotVelocity * 0.1f); // momentum
                }
                else if (_kickPhase == KickPhase.Return)
                {
                    Gizmos.color = Color.magenta;
                    Vector3 prev = _returnStart;
                    for (int i = 1; i <= 24; i++)
                    {
                        Vector3 next = QuadraticBezier(_returnStart, _returnControl, _kickOrigin, i / 24f);
                        Gizmos.DrawLine(prev, next);
                        prev = next;
                    }
                    Gizmos.color = Color.red;
                    Gizmos.DrawWireSphere(_returnControl, 0.12f);
                }
            }
        }
#endif
    }
}