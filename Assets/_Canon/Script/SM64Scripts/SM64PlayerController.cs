using UnityEngine;

namespace SM64
{
    public enum MovementSubState
    {
        Walking,
        Running
    }

    public enum PlayerVisualModel
    {
        Default,
        Crouch,
        Seal
    }

    /// <summary>
    /// SM64-style character controller powered by a modular Finite State Machine.
    /// Handles grounded movement (Walking and Running sub-states), responsive double jumping,
    /// long-jumps, backflips, wall-jumps, wall-sliding with cling, ledge-grabbing, and ground-pounds.
    /// </summary>
    [RequireComponent(typeof(CharacterController), typeof(SM64PlayerInput))]
    public class SM64PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        public float walkSpeed = 3.5f;
        public float runSpeed = 7.0f;
        public float acceleration = 12f;
        public float deceleration = 16f;
        public float turnSmoothTime = 0.08f;

        [Header("Crouch & Seal Settings")]
        [Tooltip("Movement speed multiplier applied to walkSpeed while crouching or in seal state (0.5 = half speed).")]
        public float crouchSpeedMultiplier = 0.5f;
        public float CrouchSpeed => walkSpeed * crouchSpeedMultiplier;

        [Header("[TEMPORARY] Visual Models (Inspector Assigned)")]
        [Tooltip("Default character model / visual GameObject.")]
        [SerializeField] private GameObject defaultModel;

        [Tooltip("[TEMPORARY] Visual model GameObject shown while in Crouching state.")]
        [SerializeField] private GameObject crouchModel;

        [Tooltip("[TEMPORARY] Visual model GameObject shown while in Seal state.")]
        [SerializeField] private GameObject sealModel;

        [Header("Transformation VFX")]
        [Tooltip("Particle effect (ParticleSystem or Prefab) to play when transforming into the Seal state.")]
        [SerializeField] private ParticleSystem sealTransformParticle;
        [Tooltip("Lifetime in seconds before the seal transformation particle effect despawns.")]
        [SerializeField] private float sealTransformParticleLifetime = 1.5f;
        [Tooltip("Particle effect (ParticleSystem or Prefab) to play when reverting back from the Seal state. If unassigned, defaults to sealTransformParticle.")]
        [SerializeField] private ParticleSystem sealRevertParticle;
        [Tooltip("Lifetime in seconds before the seal revert particle effect despawns.")]
        [SerializeField] private float sealRevertParticleLifetime = 1.5f;

        [Header("Ground Pound VFX")]
        [Tooltip("Particle effect (GameObject or ParticleSystem) spawned when ground pound hits the ground.")]
        [SerializeField] private GameObject groundPoundLandParticle;
        [Tooltip("Lifetime in seconds before the ground pound impact particle despawns.")]
        [SerializeField] private float groundPoundLandParticleLifetime = 1.5f;

        [Header("Running VFX")]
        [Tooltip("Particle effect prefab (GameObject or ParticleSystem) spawned periodically while running.")]
        [SerializeField] private GameObject runParticlePrefab;
        [Tooltip("Target object/transform where the running particle is spawned. If null, spawns at the player's position.")]
        [SerializeField] private Transform runParticleSpawnTarget;
        [Tooltip("Interval in seconds between particle spawns while in the Running state.")]
        [SerializeField] private float runParticleInterval = 0.5f;
        [Tooltip("Lifetime in seconds before the spawned running particle despawns.")]
        [SerializeField] private float runParticleLifetime = 1.0f;

        [Header("Running Mechanics")]
        [Tooltip("Continuous hold time (seconds) in 1 direction required to activate running.")]
        public float runActivationTime = 3.0f;
        [Tooltip("Rapid braking deceleration rate when movement keys are released in Running state.")]
        public float rapidDeceleration = 32.0f;
        [Tooltip("Delay in seconds after releasing movement keys in Running state before switching back to grounded walking state.")]
        public float runStopDelay = 0.25f;
        [Tooltip("Rate at which horizontal speed gradually decays while in the air.")]
        public float airSpeedDecay = 1.2f;

        [Header("Jumping & Double Jump")]
        public float jumpForce = 7.0f;
        public float doubleJumpForce = 6.5f;
        public float tripleJumpForce = 9.0f;
        public float longJumpForce = 8.5f;
        public float backflipForce = 8.0f;
        public float wallJumpForce = 8.0f;
        public float leapForce = 6.0f;
        [Tooltip("Multiplier applied to runSpeed for the leap's horizontal impulse.")]
        public float leapSpeedMultiplier = 1.6f;
        public float gravity = -9.81f;
        public float maxFallSpeed = -20f;

        [Header("Water & Swimming Settings")]
        [Tooltip("Layer representing water colliders (default Water layer 4).")]
        public LayerMask waterLayerMask;
        [Tooltip("Swim speed while in Seal Swim state.")]
        public float swimSpeed = 14.0f;
        [Tooltip("Acceleration rate while swimming.")]
        public float swimAcceleration = 24.0f;
        [Tooltip("Deceleration rate when stopping swimming.")]
        public float swimDeceleration = 16.0f;
        [Tooltip("Vertical offset above the water collider surface to float at.")]
        public float swimFloatHeightOffset = 0.05f;
        [Tooltip("Spring strength for keeping the seal floating at the water surface.")]
        public float swimBuoyancySpring = 12.0f;
        [Tooltip("Upward fling force when reverting to human while swimming.")]
        public float swimFlingUpwardForce = 11.0f;
        [Tooltip("Forward fling force when reverting to human while swimming.")]
        public float swimFlingForwardForce = 9.0f;
        [Tooltip("Upward fling force when reverting to human from normal seal state on land.")]
        public float sealRevertFlingUpForce = 8.0f;
        [Tooltip("Forward fling force when reverting to human from normal seal state on land.")]
        public float sealRevertFlingForwardForce = 6.0f;

        [Header("Swim Particle VFX")]
        [Tooltip("Particle effect prefab spawned behind the seal while swimming. If null, defaults to runParticlePrefab.")]
        public GameObject swimParticlePrefab;
        [Tooltip("Target transform where swim particles spawn. If null, spawns behind the player.")]
        public Transform swimParticleSpawnTarget;
        [Tooltip("Interval in seconds between swimming particle spawns.")]
        public float swimParticleInterval = 0.18f;
        [Tooltip("Lifetime in seconds of spawned swimming particles.")]
        public float swimParticleLifetime = 0.8f;

        [Header("Spawn & Respawn Settings")]
        [Tooltip("The designated spawn point for the player. If unassigned, automatically looks for SpawnPoint in scene.")]
        public Transform spawnPoint;
        [Tooltip("Optional death visual effect spawned at player position upon death.")]
        public GameObject deathVFXPrefab;
        public float deathVFXLifetime = 2.0f;
        [Tooltip("Optional respawn visual effect spawned at spawn point upon respawn.")]
        public GameObject respawnVFXPrefab;
        public float respawnVFXLifetime = 2.0f;

        [Header("Aiming & Slow-Fall")]
        [Tooltip("Multiplier applied to gravity while aiming with Shift (0.2 = falls 5x slower).")]
        public float aimGravityMultiplier = 0.2f;
        [Tooltip("Maximum downward fall speed while aiming.")]
        public float aimMaxFallSpeed = -2.5f;

        /// <summary>True while player is holding Shift to aim the floating ball.</summary>
        public bool IsAiming { get; set; } = false;

        [Header("Double Jump Optimization & Responsiveness")]
        [Tooltip("Number of jumps allowed in mid-air (1 = double jump, 2 = triple jump)")]
        public int maxAirJumps = 1;
        [Tooltip("Grace period (seconds) to jump after running off an edge")]
        public float coyoteTime = 0.15f;
        [Tooltip("Window (seconds) to buffer a jump input prior to landing")]
        public float jumpBufferTime = 0.15f;

        [Header("Wall Slide & Cling Settings")]
        [Tooltip("Layer considered as wall surfaces for sliding and ledge grabbing.")]
        public LayerMask wallLayerMask;
        [Tooltip("Duration in seconds the character clings to the wall before sliding down.")]
        public float wallClingDuration = 0.35f;
        [Tooltip("Downward speed while sliding down a wall.")]
        public float wallSlideSpeed = 2.0f;

        [Header("Ledge Grab Settings")]
        [Tooltip("Offset relative to the detected ledge edge where the character hangs (X=lateral, Y=height below edge, Z=distance from wall).")]
        public Vector3 ledgeHangOffset = new Vector3(0f, -1.0f, 0.35f);

        [Header("Sensor Settings (Top & Middle Dots)")]
        [Tooltip("Forward ray distance for the top and middle wall sensors.")]
        public float sensorDistance = 0.65f;
        [Tooltip("Margin from the very top of the collider for the upper sensor dot.")]
        public float topSensorMargin = 0.06f;

        [Header("Ground / Collision")]
        public float groundSnapDistance = 0.2f;
        public LayerMask groundMask = ~0;
        public float wallCheckDistance = 0.6f;
        public float airControl = 0.4f;

        [Header("State Machine Debug")]
        [SerializeField] private string activeState;
        [SerializeField] private MovementSubState currentMovementSubState = MovementSubState.Walking;

        // References & State Machine
        public CharacterController CharacterController { get; private set; }
        public SM64PlayerInput Input { get; private set; }
        public PlayerStateMachine StateMachine { get; private set; }

        // States
        public PlayerWalkingState WalkingState { get; private set; }
        public PlayerRunningState RunningState { get; private set; }
        public PlayerGroundedState GroundedState => WalkingState; // Backwards compatibility alias
        public PlayerAirborneState AirborneState { get; private set; }
        public PlayerWallSlideState WallSlideState { get; private set; }
        public PlayerLedgeGrabState LedgeGrabState { get; private set; }
        public PlayerLongJumpState LongJumpState { get; private set; }
        public PlayerBackflipState BackflipState { get; private set; }
        public PlayerWallJumpState WallJumpState { get; private set; }
        public PlayerGroundPoundState GroundPoundState { get; private set; }
        public PlayerLeapState LeapState { get; private set; }
        public PlayerCrouchingState CrouchingState { get; private set; }
        public PlayerSealState SealState { get; private set; }
        public PlayerSealSwimState SealSwimState => SealState?.SwimState;

        // Movement Sub-State
        public MovementSubState CurrentMovementSubState
        {
            get => currentMovementSubState;
            set => currentMovementSubState = value;
        }

        public float RunLandingThreshold => walkSpeed * 1.5f;

        // Public Game Time Access (for pause, animation timing, and desync reconciliation)
        public float GameTime => SM64.GameTime.Time;
        public float GameDeltaTime => SM64.GameTime.DeltaTime;
        public bool IsGamePaused => SM64.GameTime.IsPaused;

        // Runtime Velocity & Counters
        public Vector3 HorizontalVelocity { get; set; }
        public float VerticalVelocity { get; set; }
        public int AirJumpsRemaining { get; set; }

        private float _jumpBufferCounter;
        private float _turnSmoothVelocity;

        private void Awake()
        {
            CharacterController = GetComponent<CharacterController>();
            Input = GetComponent<SM64PlayerInput>();
            if (Input == null)
            {
                Input = gameObject.AddComponent<SM64PlayerInput>();
            }

            // Default to "Wall" layer (Layer 6 in TagManager) if not assigned
            if (wallLayerMask.value == 0)
            {
                int wallLayer = LayerMask.NameToLayer("Wall");
                wallLayerMask = wallLayer != -1 ? (1 << wallLayer) : (1 << 6);
            }

            // Default to "Water" layer (Layer 4 in TagManager) if not assigned
            if (waterLayerMask.value == 0)
            {
                int waterLayer = LayerMask.NameToLayer("Water");
                waterLayerMask = waterLayer != -1 ? (1 << waterLayer) : (1 << 4);
            }

            // Discover designated spawn point if unassigned
            if (spawnPoint == null)
            {
                SpawnPoint sp = SpawnPoint.GetSpawnPoint();
                if (sp != null)
                {
                    spawnPoint = sp.transform;
                }
            }

            // Setup temporary models if prefabs are assigned from project assets
            if (crouchModel != null && !crouchModel.scene.IsValid())
            {
                crouchModel = Instantiate(crouchModel, transform);
                crouchModel.transform.localPosition = Vector3.zero;
                crouchModel.transform.localRotation = Quaternion.identity;
                crouchModel.SetActive(false);
            }

            if (sealModel != null && !sealModel.scene.IsValid())
            {
                sealModel = Instantiate(sealModel, transform);
                sealModel.transform.localPosition = Vector3.zero;
                sealModel.transform.localRotation = Quaternion.identity;
                sealModel.SetActive(false);
            }

            SetVisualModel(PlayerVisualModel.Default);

            // Initialize State Machine and concrete states
            StateMachine = new PlayerStateMachine();
            WalkingState = new PlayerWalkingState(this, StateMachine);
            RunningState = new PlayerRunningState(this, StateMachine);
            AirborneState = new PlayerAirborneState(this, StateMachine);
            WallSlideState = new PlayerWallSlideState(this, StateMachine);
            LedgeGrabState = new PlayerLedgeGrabState(this, StateMachine);
            LongJumpState = new PlayerLongJumpState(this, StateMachine);
            BackflipState = new PlayerBackflipState(this, StateMachine);
            WallJumpState = new PlayerWallJumpState(this, StateMachine);
            GroundPoundState = new PlayerGroundPoundState(this, StateMachine);
            LeapState = new PlayerLeapState(this, StateMachine);
            CrouchingState = new PlayerCrouchingState(this, StateMachine);
            SealState = new PlayerSealState(this, StateMachine);

            StateMachine.OnStateChanged += state => activeState = state.GetType().Name;
        }

        #region Visual Model Swapping (Temporary)

        /// <summary>
        /// [TEMPORARY] Switches active visual model between Default, Crouch, and Seal models.
        /// </summary>
        public void SetVisualModel(PlayerVisualModel modelType)
        {
            if (defaultModel != null)
            {
                defaultModel.SetActive(modelType == PlayerVisualModel.Default);
            }

            if (crouchModel != null)
            {
                crouchModel.SetActive(modelType == PlayerVisualModel.Crouch);
            }
            else if (modelType == PlayerVisualModel.Crouch && defaultModel != null)
            {
                defaultModel.SetActive(true);
            }

            if (sealModel != null)
            {
                sealModel.SetActive(modelType == PlayerVisualModel.Seal);
            }
            else if (modelType == PlayerVisualModel.Seal && defaultModel != null)
            {
                defaultModel.SetActive(true);
            }
        }

        public float RunParticleInterval => runParticleInterval;
        public float RunParticleLifetime => runParticleLifetime;
        public Transform RunParticleSpawnTarget => runParticleSpawnTarget;
        public GameObject RunParticlePrefab => runParticlePrefab;

        /// <summary>
        /// Plays the seal transformation particle effect at the player's position.
        /// Handles both scene instances and prefab assets, despawning after sealTransformParticleLifetime (1.5s).
        /// </summary>
        public void PlaySealTransformParticle()
        {
            if (sealTransformParticle == null)
                return;

            ParticleSystem spawned = Instantiate(sealTransformParticle, transform.position, Quaternion.identity);
            spawned.gameObject.SetActive(true);
            spawned.Play();
            Destroy(spawned.gameObject, sealTransformParticleLifetime);
        }

        /// <summary>
        /// Plays the particle effect when reverting back from the Seal state.
        /// Falls back to sealTransformParticle if sealRevertParticle is not assigned in Inspector.
        /// Handles both scene instances and prefab assets, despawning after sealRevertParticleLifetime.
        /// </summary>
        public void PlaySealRevertParticle()
        {
            ParticleSystem particleToPlay = sealRevertParticle != null ? sealRevertParticle : sealTransformParticle;
            if (particleToPlay == null)
                return;

            ParticleSystem spawned = Instantiate(particleToPlay, transform.position, Quaternion.identity);
            spawned.gameObject.SetActive(true);
            spawned.Play();
            Destroy(spawned.gameObject, sealRevertParticleLifetime);
        }

        /// <summary>
        /// Plays the particle effect when the player slams into the ground during Ground Pound.
        /// Spawns at the player's ground contact position and despawns after groundPoundLandParticleLifetime.
        /// </summary>
        public void PlayGroundPoundLandParticle()
        {
            if (groundPoundLandParticle == null)
                return;

            Vector3 spawnPosition = transform.position;
            GameObject spawned = Instantiate(groundPoundLandParticle, spawnPosition, Quaternion.identity);
            ParticleSystem ps = spawned.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ps.Play();
            }
            Destroy(spawned, groundPoundLandParticleLifetime);
        }

        /// <summary>
        /// Spawns a running particle effect at the specified target (or player position if null)
        /// and destroys it after runParticleLifetime (1.0s).
        /// </summary>
        public void SpawnRunningParticle()
        {
            if (runParticlePrefab == null)
                return;

            Vector3 spawnPosition = runParticleSpawnTarget != null
                ? runParticleSpawnTarget.position
                : transform.position;

            Quaternion spawnRotation = runParticleSpawnTarget != null
                ? runParticleSpawnTarget.rotation
                : transform.rotation;

            GameObject spawned = Instantiate(runParticlePrefab, spawnPosition, spawnRotation);
            Destroy(spawned, runParticleLifetime);
        }

        public float SwimParticleInterval => swimParticleInterval;
        public float SwimParticleLifetime => swimParticleLifetime;
        public GameObject SwimParticlePrefab => swimParticlePrefab;

        /// <summary>
        /// Spawns a swim wake particle behind the seal while in Seal Swim state.
        /// </summary>
        public void SpawnSwimParticle()
        {
            GameObject prefabToSpawn = swimParticlePrefab != null ? swimParticlePrefab : runParticlePrefab;
            if (prefabToSpawn == null)
                return;

            Vector3 spawnPosition = swimParticleSpawnTarget != null
                ? swimParticleSpawnTarget.position
                : transform.position - transform.forward * 0.6f + Vector3.up * 0.05f;

            Quaternion spawnRotation = swimParticleSpawnTarget != null
                ? swimParticleSpawnTarget.rotation
                : Quaternion.LookRotation(-transform.forward, Vector3.up);

            GameObject spawned = Instantiate(prefabToSpawn, spawnPosition, spawnRotation);
            Destroy(spawned, swimParticleLifetime);
        }

        #endregion

        private void Start()
        {
            StateMachine.Initialize(WalkingState);
        }

        private void Update()
        {
            // Water grace timer countdown
            if (_waterGraceTimer > 0f)
            {
                _waterGraceTimer -= Time.deltaTime;
            }

            // Jump buffer timer
            if (_jumpBufferCounter > 0f)
            {
                _jumpBufferCounter -= Time.deltaTime;
            }

            // State Machine frame execution
            StateMachine.HandleInput();
            StateMachine.LogicUpdate();
            StateMachine.PhysicsUpdate();

            // Continuous water check (catches triggers, volumes, and walking into water)
            if (CheckWaterContact(out _))
            {
                HandleWaterCollision();
            }

            // Execute movement through CharacterController if enabled
            if (CharacterController.enabled)
            {
                Vector3 finalVelocity = new Vector3(HorizontalVelocity.x, VerticalVelocity, HorizontalVelocity.z);
                CharacterController.Move(finalVelocity * Time.deltaTime);
            }
        }

        /// <summary>
        /// Evaluates whether landing from airborne keeps Running or returns to Walking
        /// based on horizontal speed >= (walkSpeed + half of walkSpeed = 1.5 * walkSpeed).
        /// </summary>
        public IPlayerState GetLandingMovementState()
        {
            float currentSpeed = HorizontalVelocity.magnitude;
            if (CurrentMovementSubState == MovementSubState.Running && currentSpeed >= RunLandingThreshold)
            {
                return RunningState;
            }

            CurrentMovementSubState = MovementSubState.Walking;
            return WalkingState;
        }

        #region Wall & Ledge Sensor Detection

        public Vector3 GetTopSensorOrigin()
        {
            float halfHeight = CharacterController != null ? CharacterController.height * 0.5f : 1.0f;
            float centerY = CharacterController != null ? CharacterController.center.y : 0f;
            return transform.position + Vector3.up * (centerY + halfHeight - topSensorMargin);
        }

        public Vector3 GetMiddleSensorOrigin()
        {
            float centerY = CharacterController != null ? CharacterController.center.y : 0f;
            return transform.position + Vector3.up * centerY;
        }

        public bool CheckWallSensors(out bool middleHit, out bool topHit, out RaycastHit middleHitInfo, out RaycastHit topHitInfo)
        {
            Vector3 topOrigin = GetTopSensorOrigin();
            Vector3 middleOrigin = GetMiddleSensorOrigin();
            Vector3 castDir = transform.forward;

            topHit = Physics.Raycast(topOrigin, castDir, out topHitInfo, sensorDistance, wallLayerMask, QueryTriggerInteraction.Ignore);
            middleHit = Physics.Raycast(middleOrigin, castDir, out middleHitInfo, sensorDistance, wallLayerMask, QueryTriggerInteraction.Ignore);

            return middleHit || topHit;
        }

        public bool FindLedgePositions(RaycastHit middleHitInfo, out Vector3 hangPos, out Vector3 climbPos, out Vector3 wallNormal)
        {
            wallNormal = middleHitInfo.normal;

            // 1. Check if the wall has an explicit LedgeMarker plane attached
            LedgeMarker marker = middleHitInfo.collider.GetComponentInParent<LedgeMarker>() ??
                                 middleHitInfo.collider.GetComponentInChildren<LedgeMarker>();
            if (marker != null)
            {
                hangPos = marker.GetHangPosition();
                climbPos = marker.GetClimbPosition();
                wallNormal = marker.GetWallNormal();
                return true;
            }

            // 2. Automatic geometric edge finding
            Vector3 topOrigin = GetTopSensorOrigin();
            Vector3 downCastOrigin = topOrigin + transform.forward * (middleHitInfo.distance + 0.12f) + Vector3.up * 0.2f;

            if (Physics.Raycast(downCastOrigin, Vector3.down, out RaycastHit topSurfaceHit, CharacterController.height * 0.8f, wallLayerMask, QueryTriggerInteraction.Ignore))
            {
                float edgeY = topSurfaceHit.point.y;
                Vector3 edgePoint = new Vector3(middleHitInfo.point.x, edgeY, middleHitInfo.point.z);

                hangPos = edgePoint + wallNormal * ledgeHangOffset.z + Vector3.up * ledgeHangOffset.y;
                climbPos = topSurfaceHit.point + Vector3.up * (CharacterController.height * 0.5f) + (-wallNormal * 0.4f);
                return true;
            }

            hangPos = Vector3.zero;
            climbPos = Vector3.zero;
            return false;
        }

        #endregion

        #region Helpers & Physics Utilities

        public void ResetAirJumps()
        {
            AirJumpsRemaining = maxAirJumps;
        }

        public void BufferJump()
        {
            _jumpBufferCounter = jumpBufferTime;
        }

        public bool ConsumeBufferedJump()
        {
            if (_jumpBufferCounter > 0f)
            {
                _jumpBufferCounter = 0f;
                return true;
            }
            return false;
        }

        public bool IsGrounded()
        {
            if (CharacterController.isGrounded)
                return true;

            return Physics.Raycast(transform.position, Vector3.down, groundSnapDistance + CharacterController.skinWidth, groundMask, QueryTriggerInteraction.Ignore);
        }

        public bool CheckWall(out Vector3 wallNormal)
        {
            Vector3 origin = transform.position + Vector3.up * (CharacterController.height * 0.5f);
            Vector3[] directions = { transform.forward, -transform.forward, transform.right, -transform.right };

            foreach (var dir in directions)
            {
                if (Physics.Raycast(origin, dir, out RaycastHit hit, wallCheckDistance, groundMask, QueryTriggerInteraction.Ignore))
                {
                    wallNormal = hit.normal;
                    return true;
                }
            }

            wallNormal = Vector3.zero;
            return false;
        }

        public Vector3 GetCameraRelativeInput(Vector2 moveInput)
        {
            if (moveInput.sqrMagnitude < 0.001f)
                return Vector3.zero;

            Transform cam = Camera.main != null ? Camera.main.transform : null;
            Vector3 camForward = cam ? Vector3.Scale(cam.forward, new Vector3(1, 0, 1)).normalized : Vector3.forward;
            Vector3 camRight = cam ? Vector3.Scale(cam.right, new Vector3(1, 0, 1)).normalized : Vector3.right;

            return (camForward * moveInput.y + camRight * moveInput.x).normalized;
        }

        public void RotateTowards(Vector3 direction, float smoothTime)
        {
            if (direction.sqrMagnitude < 0.001f)
                return;

            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref _turnSmoothVelocity, smoothTime);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);
        }

        public void ApplyGravity(float multiplier = 1f)
        {
            if (IsGrounded() && VerticalVelocity < 0f)
            {
                VerticalVelocity = -2f;
                return;
            }

            if (IsAiming)
            {
                // Decreased gravity for floaty slow fall while aiming
                float slowGrav = gravity * aimGravityMultiplier;
                VerticalVelocity = Mathf.Max(VerticalVelocity + slowGrav * Time.deltaTime, aimMaxFallSpeed);
            }
            else
            {
                VerticalVelocity = Mathf.Max(VerticalVelocity + gravity * multiplier * Time.deltaTime, maxFallSpeed);
            }
        }

        #endregion

        #region Water Collision & Respawn

        private float _waterGraceTimer;
        private bool _isDead;

        /// <summary>
        /// Grants temporary immunity from water collision death (e.g. during water breach fling or post-respawn).
        /// </summary>
        public void GrantWaterGracePeriod(float duration)
        {
            _waterGraceTimer = Mathf.Max(_waterGraceTimer, duration);
        }

        /// <summary>
        /// Evaluates whether the player is currently contacting or submerged in the water collision layer.
        /// </summary>
        public bool CheckWaterContact(out RaycastHit surfaceHit)
        {
            surfaceHit = default;
            if (_waterGraceTimer > 0f || _isDead)
                return false;

            Vector3 playerPos = transform.position;
            float radius = CharacterController != null ? CharacterController.radius : 0.5f;
            float height = CharacterController != null ? CharacterController.height : 2.0f;

            // 1. Raycast downward across player height
            Vector3 rayStart = playerPos + Vector3.up * 1.5f;
            float rayDistance = height + 1.8f;
            if (Physics.Raycast(rayStart, Vector3.down, out surfaceHit, rayDistance, waterLayerMask, QueryTriggerInteraction.Collide))
            {
                if (playerPos.y <= surfaceHit.point.y + 0.35f)
                {
                    return true;
                }
            }

            // 2. Overlap sphere at player base/center
            Vector3 checkCenter = playerPos + Vector3.up * (height * 0.3f);
            if (Physics.CheckSphere(checkCenter, radius + 0.15f, waterLayerMask, QueryTriggerInteraction.Collide))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Handles water contact:
        /// - If player is in Seal state, activates Seal Swim state.
        /// - If player is in Human state (not seal), triggers death and respawn.
        /// </summary>
        public void HandleWaterCollision()
        {
            if (_waterGraceTimer > 0f || _isDead)
                return;

            if (StateMachine != null && StateMachine.CurrentState is PlayerSealState)
            {
                // In seal state: transition to SealSwimState if not already swimming
                if (!(StateMachine.CurrentState is PlayerSealSwimState))
                {
                    SealState.EnterSwim();
                }
            }
            else
            {
                // Not in seal state: Die and respawn!
                Die();
            }
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (((1 << hit.gameObject.layer) & waterLayerMask) != 0 || hit.collider.CompareTag("Water"))
            {
                HandleWaterCollision();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (((1 << other.gameObject.layer) & waterLayerMask) != 0 || other.CompareTag("Water"))
            {
                HandleWaterCollision();
            }
        }

        private void OnTriggerStay(Collider other)
        {
            if (((1 << other.gameObject.layer) & waterLayerMask) != 0 || other.CompareTag("Water"))
            {
                HandleWaterCollision();
            }
        }

        /// <summary>
        /// Kills the player and triggers respawn at the spawn point.
        /// </summary>
        public void Die()
        {
            if (_isDead) return;
            _isDead = true;

            if (deathVFXPrefab != null)
            {
                GameObject vfx = Instantiate(deathVFXPrefab, transform.position, Quaternion.identity);
                Destroy(vfx, deathVFXLifetime);
            }

            if (PlayerRespawnManager.Instance != null)
            {
                PlayerRespawnManager.Instance.Die();
            }
            else
            {
                Respawn();
            }
        }

        /// <summary>
        /// Respawns the player at the single SpawnPoint in the game.
        /// </summary>
        public void Respawn()
        {
            _isDead = false;

            // Find target spawn position and rotation
            Vector3 targetPos = transform.position;
            Quaternion targetRot = transform.rotation;

            if (spawnPoint != null)
            {
                targetPos = spawnPoint.position;
                targetRot = spawnPoint.rotation;
            }
            else if (SpawnPoint.Instance != null)
            {
                targetPos = SpawnPoint.Instance.Position;
                targetRot = SpawnPoint.Instance.Rotation;
            }
            else
            {
                SpawnPoint sp = SpawnPoint.GetSpawnPoint();
                if (sp != null)
                {
                    targetPos = sp.Position;
                    targetRot = sp.Rotation;
                }
            }

            // Temporarily disable CharacterController for clean teleportation
            if (CharacterController != null)
            {
                CharacterController.enabled = false;
            }

            transform.position = targetPos;
            transform.rotation = targetRot;

            if (CharacterController != null)
            {
                CharacterController.enabled = true;
            }

            // Reset dynamic velocities
            HorizontalVelocity = Vector3.zero;
            VerticalVelocity = 0f;
            ResetAirJumps();

            // Reset visual model and state
            SetVisualModel(PlayerVisualModel.Default);

            BallKicking kicking = GetComponent<BallKicking>();
            if (kicking != null) kicking.enabled = true;

            SealStateMachine seal = GetComponent<SealStateMachine>();
            if (seal != null) seal.EndSucking();

            if (StateMachine != null)
            {
                StateMachine.ChangeState(WalkingState);
            }

            GrantWaterGracePeriod(0.75f);

            if (respawnVFXPrefab != null)
            {
                GameObject vfx = Instantiate(respawnVFXPrefab, targetPos, targetRot);
                Destroy(vfx, respawnVFXLifetime);
            }
        }

        #endregion

        #region Scene Gizmos

        private void OnDrawGizmosSelected()
        {
            Vector3 topOrigin = GetTopSensorOrigin();
            Vector3 middleOrigin = GetMiddleSensorOrigin();

            // Draw Middle Sensor Dot & Ray
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(middleOrigin, 0.06f);
            Gizmos.DrawRay(middleOrigin, transform.forward * sensorDistance);

            // Draw Top Sensor Dot & Ray
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(topOrigin, 0.06f);
            Gizmos.DrawRay(topOrigin, transform.forward * sensorDistance);
        }

        #endregion
    }
}
