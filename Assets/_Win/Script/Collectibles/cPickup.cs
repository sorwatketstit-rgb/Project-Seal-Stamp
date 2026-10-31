using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class cPickup : MonoBehaviour
{
    public cData picture;

    [Header("Player Detection")]
    public string playerTag = "Player";

    private bool playerInRange;
    private bool alreadyCollected;

    private void Start()
    {
        if (cManager.Instance != null && picture != null)
        {
            alreadyCollected = cManager.Instance.IsCollected(picture.id);

            if (alreadyCollected)
                gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (playerInRange && !alreadyCollected)
        {
            bool ePressed = false;

            #if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
                ePressed |= Keyboard.current.eKey.wasPressedThisFrame;
            #endif

            #if ENABLE_LEGACY_INPUT_MANAGER
            ePressed |= Input.GetKeyDown(KeyCode.E);
            #endif

            if (ePressed)
            {
                Interact();
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            playerInRange = true;
            Debug.Log($"[cPickup] Player entered trigger of '{gameObject.name}'. Press E to inspect.");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag))
            return;

        playerInRange = false;

        if (cPopupUI.Instance != null)
            cPopupUI.Instance.CloseIfShowing(this);
    }

    public void Interact()
    {
        if (alreadyCollected || !playerInRange || picture == null)
            return;

        if (cPopupUI.Instance != null)
        {
            cPopupUI.Instance.Show(this);
        }
        else
        {
            Debug.LogWarning("[cPickup] cPopupUI.Instance is null! Make sure cPopupUI script is in the scene.");
        }
    }

    public bool TryCollect()
    {
        if (alreadyCollected || picture == null)
            return false;

        if (cManager.Instance == null)
        {
            Debug.LogWarning("[cPickup] cManager is missing.");
            return false;
        }

        bool success = cManager.Instance.Collect(picture);

        if (success)
        {
            alreadyCollected = true;
            gameObject.SetActive(false);
        }

        return success;
    }
}