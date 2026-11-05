using System;
using System.Collections;
using UnityEngine;

namespace SM64
{
    /// <summary>
    /// Manages player death, respawn, screen fading, and unstuck functionality.
    /// Respawns the player at the single SpawnPoint in the game with a smooth screen fade.
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

        [Header("Death Screen Fade")]
        [Tooltip("Time in seconds to fade to black upon dying.")]
        [SerializeField] private float deathFadeOutDuration = 0.35f;

        [Tooltip("Time in seconds the screen stays completely black before fading in.")]
        [SerializeField] private float deathFadeHoldDuration = 0.15f;

        [Tooltip("Time in seconds to fade back in from black after respawning.")]
        [SerializeField] private float deathFadeInDuration = 0.45f;

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
        private bool _isRespawning;

        public bool IsRespawning => _isRespawning;

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

            // Ensure ScreenFader is active in scene
            EnsureScreenFader();
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

        private void EnsureScreenFader()
        {
            if (ScreenFader.Instance == null)
            {
                ScreenFader fader = FindFirstObjectByType<ScreenFader>();
                if (fader == null)
                {
                    GameObject faderGo = new GameObject("ScreenFader");
                    fader = faderGo.AddComponent<ScreenFader>();
                }
                fader.EnsureOverlayExists();
            }
        }

        /// <summary>
        /// Handles player death (e.g. entering water while not in seal state).
        /// Triggers a screen fade to black, repositions the player at the spawn point, and fades back in.
        /// </summary>
        public void Die()
        {
            if (_isRespawning) return;

            if (playerController == null)
            {
                playerController = FindFirstObjectByType<SM64PlayerController>();
                if (playerController == null) return;
            }

            StartCoroutine(DeathFadeRoutine());
        }

        private IEnumerator DeathFadeRoutine()
        {
            _isRespawning = true;
            EnsureScreenFader();

            OnPlayerDied?.Invoke();

            if (deathVFXPrefab != null && playerController != null)
            {
                GameObject vfx = Instantiate(deathVFXPrefab, playerController.transform.position, Quaternion.identity);
                Destroy(vfx, deathVFXLifetime);
            }

            // Block player inputs during death sequence
            if (playerController != null && playerController.Input != null)
            {
                playerController.Input.IsInputBlocked = true;
            }

            // 1. Fade screen out to black
            if (ScreenFader.Instance != null && deathFadeOutDuration > 0f)
            {
                yield return ScreenFader.Instance.FadeOutRoutine(deathFadeOutDuration);
            }

            // 2. While screen is black, execute physical respawn and reset
            ExecuteRespawn();

            // 3. Optional hold on black screen
            if (deathFadeHoldDuration > 0f)
            {
                yield return new WaitForSeconds(deathFadeHoldDuration);
            }

            // 4. Fade screen back in from black
            if (ScreenFader.Instance != null && deathFadeInDuration > 0f)
            {
                yield return ScreenFader.Instance.FadeInRoutine(deathFadeInDuration);
            }

            // Restore player inputs
            if (playerController != null && playerController.Input != null)
            {
                playerController.Input.IsInputBlocked = false;
            }

            _isRespawning = false;
            OnPlayerRespawned?.Invoke();
        }

        /// <summary>
        /// Triggered when the unstuck button is clicked. Closes the menu if open and respawns the player with fade.
        /// </summary>
        public void Unstuck()
        {
            if (GenshinMenuManager.Instance != null && GenshinMenuManager.Instance.IsMenuOpen)
            {
                GenshinMenuManager.Instance.CloseMenu();
            }

            Die();
        }

        /// <summary>
        /// Instantly respawns the player without a fade (used when direct repositioning is needed).
        /// </summary>
        public void Respawn()
        {
            ExecuteRespawn();
            OnPlayerRespawned?.Invoke();
        }

        private void ExecuteRespawn()
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

            // 4. Clear any tracked water colliders and grant grace period
            playerController.ClearWaterColliders();
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
        }
    }
}
