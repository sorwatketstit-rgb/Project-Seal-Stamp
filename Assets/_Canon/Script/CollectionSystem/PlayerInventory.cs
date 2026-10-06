using UnityEngine;

namespace SM64
{
    /// <summary>
    /// Handles trigger detection for picking up collectables.
    /// Does NOT store any collectable data - it fires functions on
    /// CollectableManager which owns the actual counts.
    ///
    /// Attach this to the same GameObject as SM64PlayerController.
    /// The player must have the "Player" tag.
    /// </summary>
    public class PlayerInventory : MonoBehaviour
    {
        // -----------------------------------------------------------------------
        //  Trigger detection - detects Letter collectables and tells the
        //  CollectableManager to increment the count. This script never
        //  reads or writes the letter amount directly.
        // -----------------------------------------------------------------------
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Letter"))
            {
                HandleLetterPickup(other);
            }

            // Future collectables:
            // if (other.CompareTag("Coin")) { HandleCoinPickup(other); }
        }

        // -----------------------------------------------------------------------
        //  Pickup handlers - validate and fire the manager function
        // -----------------------------------------------------------------------
        private void HandleLetterPickup(Collider letterCollider)
        {
            MailLetterCollectable letter = letterCollider.GetComponent<MailLetterCollectable>();
            if (letter == null || !letter.CanBeCollected) return;

            // Tell the letter to play its effects and destroy itself
            letter.Collect();

            // Fire the manager function - we never touch the count ourselves
            if (CollectableManager.Instance != null)
            {
                CollectableManager.Instance.AddLetters(1);
            }
            else
            {
                Debug.LogWarning("[PlayerInventory] CollectableManager.Instance is null! " +
                                 "Make sure a CollectableManager exists in the scene.");
            }
        }
    }
}
