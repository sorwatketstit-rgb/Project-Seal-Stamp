using System;
using UnityEngine;

namespace SM64
{
    /// <summary>
    /// Manages player death, respawn, and unstuck functionality.
    /// Respawns the player at the single SpawnPoint in the game.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerRespawnManager : MonoBehaviour
    {
        public static PlayerRespawnManager Instance { get; private set; }

        public static event Action OnPlayerDied;
        public static event Action OnPlayerRespawned;

        [Header("References")]
        [Tooltip("The player controller. If empty, automatically located in scene.")]
        [SerializeField] private SM64PlayerController playerController;

        [Tooltip("The designated spawn point. If unassigned, automatically located in scene.")]
        [SerializeField] private SpawnPoint spawnPoint;

        [Header("Effects & Feedback")]
        [Tooltip("Optional VFX spawned at player position upon death.")]
        [SerializeField] private GameObject deathVFXPrefab;
        [SerializeField] private float deathVFXLifetime = 2.0f;

        [Tooltip("Optional VFX spawned at the spawn point upon respawning.")]
        [SerializeField] private GameObject respawnVFXPrefab;
        [SerializeField] private float respawnVFXLifetime = 2.0f;

        [Header("Unstuck Key (Optional)")]
        [Tooltip("Optional hotkey to trigger Unstuck (default None; can be set to e.g. U for quick testing).")]
        [SerializeField] private KeyCode unstuckHotkey = KeyCode.None;

        private Vector3 _initialPlayerPosition;
        private Quaternion _initialPlayerRotation;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (playerController == null)
            {
                playerController = GetComponent<SM64PlayerController>() ?? FindFirstObjectByType<SM64PlayerController>();
            }

            if (playerController != null)
            {
                _initialPlayerPosition = playerController.transform.position;
                _initialPlayerRotation = playerController.transform.rotation;
            }
        }

        private void Start()
        {
            if (spawnPoint == null)
            {
                spawnPoint = SpawnPoint.GetSpawnPoint();
            }
        }

        private void Update()
        {
            if (unstuckHotkey != KeyCode.None && Input.GetKeyDown(unstuckHotkey))
            {
                Unstuck();
            }
        }

        /// <summary>
        /// Handles player death (e.g. entering water while not in seal state) and initiates respawn.
        /// </summary>
        public void Die()
        {
            if (playerController == null) return;

            OnPlayerDied?.Invoke();

            if (deathVFXPrefab != null)
            {
                GameObject vfx = Instantiate(deathVFXPrefab, playerController.transform.position, Quaternion.identity);
                Destroy(vfx, deathVFXLifetime);
            }

            Respawn();
        }

        /// <summary>
        /// Triggered when the unstuck button is clicked. Closes the menu if open and respawns the player.
        /// </summary>
        public void Unstuck()
        {
            if (GenshinMenuManager.Instance != null && GenshinMenuManager.Instance.IsMenuOpen)
            {
                GenshinMenuManager.Instance.CloseMenu();
            }

            Respawn();
        }

        /// <summary>
        /// Respawns the player at the SpawnPoint (or fallback initial position).
        /// Resets character controller, velocities, states, and visual models.
        /// </summary>
        public void Respawn()
        {
            if (playerController == null)
            {
                playerController = FindFirstObjectByType<SM64PlayerController>();
                if (playerController == null) return;
            }

            if (spawnPoint == null)
            {
                spawnPoint = SpawnPoint.GetSpawnPoint();
            }

            Vector3 targetPos = spawnPoint != null ? spawnPoint.Position : _initialPlayerPosition;
            Quaternion targetRot = spawnPoint != null ? spawnPoint.Rotation : _initialPlayerRotation;

            // 1. Temporarily disable CharacterController for clean teleportation without collision interference
            CharacterController cc = playerController.CharacterController;
            if (cc != null)
            {
                cc.enabled = false;
            }

            playerController.transform.position = targetPos;
            playerController.transform.rotation = targetRot;

            if (cc != null)
            {
                cc.enabled = true;
            }

            // 2. Reset physical velocities and jump states
            playerController.HorizontalVelocity = Vector3.zero;
            playerController.VerticalVelocity = 0f;
            playerController.ResetAirJumps();

            // 3. Reset State Machine: revert to human model and grounded walking state
            playerController.SetVisualModel(PlayerVisualModel.Default);

            BallKicking kicking = playerController.GetComponent<BallKicking>();
            if (kicking != null) kicking.enabled = true;

            SealStateMachine seal = playerController.GetComponent<SealStateMachine>();
            if (seal != null) seal.EndSucking();

            if (playerController.StateMachine != null)
            {
                playerController.StateMachine.ChangeState(playerController.WalkingState);
            }

            // 4. Grant grace period against immediate water death
            playerController.GrantWaterGracePeriod(0.75f);

            // 5. Spawn respawn particle if assigned
            if (respawnVFXPrefab != null)
            {
                GameObject vfx = Instantiate(respawnVFXPrefab, targetPos, targetRot);
                Destroy(vfx, respawnVFXLifetime);
            }

            // 6. Camera reset
            SM64CameraController cam = FindFirstObjectByType<SM64CameraController>();
            if (cam != null && cam.target != null)
            {
                cam.transform.position = targetPos - targetRot * Vector3.forward * 4.0f + Vector3.up * 1.5f;
            }

            OnPlayerRespawned?.Invoke();
        }
    }
}

