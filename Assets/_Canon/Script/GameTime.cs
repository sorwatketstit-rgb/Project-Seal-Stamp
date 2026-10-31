using System;
using UnityEngine;

namespace SM64
{
    /// <summary>
    /// Centralized public access container and manager for simulated game time.
    /// Provides globally accessible game time values (GameTime.Time, GameTime.DeltaTime, etc.)
    /// decoupled from Unity's Time.timeScale to support custom pausing, frame stepping,
    /// and client/server desync reconciliation.
    /// </summary>
    public static class GameTime
    {
        /// <summary>
        /// Total accumulated in-game elapsed simulation time in seconds.
        /// Does not advance while paused. Can be explicitly set to reconcile state desyncs.
        /// </summary>
        public static float Time { get; set; } = 0f;

        /// <summary>
        /// Frame delta time for in-game simulation in seconds (0 when paused).
        /// </summary>
        public static float DeltaTime { get; set; } = 0f;

        /// <summary>
        /// Physics / fixed simulation time delta in seconds (0 when paused).
        /// </summary>
        public static float FixedDeltaTime { get; set; } = 0f;

        /// <summary>
        /// Multiplier applied to game progression speed (default 1.0f).
        /// </summary>
        public static float TimeScale { get; set; } = 1.0f;

        /// <summary>
        /// Whether in-game simulation time is currently paused.
        /// </summary>
        public static bool IsPaused { get; set; } = false;

        /// <summary>
        /// Total in-game simulation frame/tick count.
        /// </summary>
        public static ulong TickCount { get; set; } = 0;

        /// <summary>
        /// Event invoked whenever the game is paused or unpaused (passes isPaused).
        /// </summary>
        public static event Action<bool> OnPauseStateChanged;

        /// <summary>
        /// Event invoked whenever game time is explicitly resynchronized or modified.
        /// </summary>
        public static event Action<float> OnTimeResynced;

        /// <summary>
        /// Advances game time by raw delta seconds scaled by TimeScale.
        /// Automatically called by GameTimeManager every frame.
        /// </summary>
        public static void Advance(float rawDeltaTime)
        {
            if (IsPaused)
            {
                DeltaTime = 0f;
                return;
            }

            DeltaTime = rawDeltaTime * TimeScale;
            Time += DeltaTime;
            TickCount++;
        }

        /// <summary>
        /// Explicitly resynchronizes or sets the game time to a specific timestamp.
        /// Critical for network state synchronization, replay seeking, and desync reconciliation.
        /// </summary>
        public static void SetTime(float newTime)
        {
            Time = Mathf.Max(0f, newTime);
            OnTimeResynced?.Invoke(Time);
        }

        /// <summary>
        /// Pauses game time progression.
        /// </summary>
        public static void Pause()
        {
            if (!IsPaused)
            {
                IsPaused = true;
                DeltaTime = 0f;
                OnPauseStateChanged?.Invoke(true);
            }
        }

        /// <summary>
        /// Resumes game time progression.
        /// </summary>
        public static void Resume()
        {
            if (IsPaused)
            {
                IsPaused = false;
                OnPauseStateChanged?.Invoke(false);
            }
        }

        /// <summary>
        /// Toggles pause on/off.
        /// </summary>
        public static void TogglePause()
        {
            if (IsPaused) Resume();
            else Pause();
        }

        /// <summary>
        /// Resets game time, delta, and ticks to 0.
        /// </summary>
        public static void Reset()
        {
            Time = 0f;
            DeltaTime = 0f;
            TickCount = 0;
            IsPaused = false;
        }
    }

    /// <summary>
    /// Automatic updater MonoBehaviour that ticks GameTime every Update and FixedUpdate.
    /// Automatically instantiated before scenes load if not present.
    /// </summary>
    [DisallowMultipleComponent]
    public class GameTimeManager : MonoBehaviour
    {
        public static GameTimeManager Instance { get; private set; }

        [Header("Settings")]
        [Tooltip("Whether to pause game time automatically when the application loses focus.")]
        [SerializeField] private bool pauseOnFocusLoss = false;

        [Header("Runtime Info (Inspector View)")]
        [SerializeField] private float currentGameTime;
        [SerializeField] private float currentDeltaTime;
        [SerializeField] private bool isGamePaused;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoInitialize()
        {
            if (Instance == null)
            {
                GameObject go = new GameObject("[GameTimeManager]");
                DontDestroyOnLoad(go);
                Instance = go.AddComponent<GameTimeManager>();
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            GameTime.Advance(UnityEngine.Time.unscaledDeltaTime);
            currentGameTime = GameTime.Time;
            currentDeltaTime = GameTime.DeltaTime;
            isGamePaused = GameTime.IsPaused;
        }

        private void FixedUpdate()
        {
            GameTime.FixedDeltaTime = GameTime.IsPaused ? 0f : UnityEngine.Time.fixedUnscaledDeltaTime * GameTime.TimeScale;
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (pauseOnFocusLoss && !hasFocus)
            {
                GameTime.Pause();
            }
        }
    }
}

