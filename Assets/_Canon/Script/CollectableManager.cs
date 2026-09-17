using UnityEngine;
using System;

namespace SM64
{
    /// <summary>
    /// Persistent singleton that manages all collectable data across scenes.
    /// DontDestroyOnLoad - survives scene transitions, perfect for shop integration.
    ///
    /// Letter count is private; nothing outside this class can set it directly.
    /// Use AddLetters() / RemoveLetters() / ResetLetters() to modify.
    /// Subscribe to OnLetterCountChanged for UI updates.
    ///
    /// Add future collectables (coins, stamps, etc.) here following the same pattern.
    /// </summary>
    public class CollectableManager : MonoBehaviour
    {
        public static CollectableManager Instance { get; private set; }

        // -----------------------------------------------------------------------
        //  Letters - private backing field, only mutated through methods below
        // -----------------------------------------------------------------------
        [Header("Debug (Read-Only at Runtime)")]
        [SerializeField] private int letterCount = 0;

        /// <summary>Fired whenever the letter count changes. Passes the new total.</summary>
        public event Action<int> OnLetterCountChanged;

        /// <summary>Read-only access to the current letter count.</summary>
        public int LetterCount => letterCount;

        // -----------------------------------------------------------------------
        //  Singleton + DontDestroyOnLoad
        // -----------------------------------------------------------------------
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

        // -----------------------------------------------------------------------
        //  Letter API - the ONLY way to change letterCount
        // -----------------------------------------------------------------------

        /// <summary>
        /// Increases the letter count by the given amount and notifies listeners.
        /// Called by PlayerInventory when a Letter trigger is entered.
        /// </summary>
        public void AddLetters(int amount = 1)
        {
            if (amount <= 0) return;

            letterCount += amount;
            OnLetterCountChanged?.Invoke(letterCount);
            Debug.Log($"[CollectableManager] Letters: {letterCount} (+{amount})");
        }

        /// <summary>
        /// Decreases the letter count (clamped to 0) and notifies listeners.
        /// Called by the shop system when spending letters.
        /// </summary>
        public void RemoveLetters(int amount = 1)
        {
            if (amount <= 0) return;

            letterCount = Mathf.Max(0, letterCount - amount);
            OnLetterCountChanged?.Invoke(letterCount);
            Debug.Log($"[CollectableManager] Letters: {letterCount} (-{amount})");
        }

        /// <summary>
        /// Resets the letter count to zero and notifies listeners.
        /// </summary>
        public void ResetLetters()
        {
            letterCount = 0;
            OnLetterCountChanged?.Invoke(letterCount);
            Debug.Log("[CollectableManager] Letters reset to 0.");
        }

        /// <summary>
        /// Returns true if the player has at least the given number of letters.
        /// Useful for shop "can afford" checks.
        /// </summary>
        public bool HasLetters(int amount)
        {
            return letterCount >= amount;
        }

        // -----------------------------------------------------------------------
        //  Future collectables go here, following the same pattern:
        //    private int _coinCount;
        //    public event Action<int> OnCoinCountChanged;
        //    public void AddCoins(int amount) { ... }
        // -----------------------------------------------------------------------
    }
}
