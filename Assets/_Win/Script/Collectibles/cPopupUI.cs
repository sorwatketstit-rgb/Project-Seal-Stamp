using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class cPopupUI : MonoBehaviour
{
    public static cPopupUI Instance { get; private set; }

    [Header("Popup")]
    public GameObject popupRoot;
    public Image pictureImage;
    public TMP_Text pictureNameText;
    public TMP_Text descriptionText;
    public Button collectButton;
    public Button cancelButton;

    [Header("Gameplay Controls")]
    [Tooltip("Add your player movement and camera look scripts here so player can't move/look while popup is open.")]
    public MonoBehaviour[] behavioursToDisableWhileOpen;

    private cPickup currentPickup;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (collectButton != null)
            collectButton.onClick.AddListener(CollectCurrent);

        if (cancelButton != null)
            cancelButton.onClick.AddListener(Close);

        if (popupRoot != null)
            popupRoot.SetActive(false);
    }

    public void Show(cPickup pickup)
    {
        if (pickup == null || pickup.picture == null)
            return;

        // Close the book before showing the pickup popup.
        if (cBookUI.Instance != null)
            cBookUI.Instance.CloseBook();

        currentPickup = pickup;
        cData data = pickup.picture;

        if (pictureImage != null)
        {
            pictureImage.sprite = data.image;
            pictureImage.preserveAspect = true;
            pictureImage.enabled = data.image != null;
        }

        if (pictureNameText != null)
            pictureNameText.text = data.pictureName;

        if (descriptionText != null)
            descriptionText.text = data.GetPopupDescription();

        if (popupRoot != null)
            popupRoot.SetActive(true);

        // Unlock and show cursor so player can click buttons
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SetGameplayEnabled(false);
    }

    private void CollectCurrent()
    {
        if (currentPickup != null)
            currentPickup.TryCollect();

        Close();
    }

    public void CloseIfShowing(cPickup pickup)
    {
        if (currentPickup == pickup)
            Close();
    }

    public void Close()
    {
        currentPickup = null;

        if (popupRoot != null)
            popupRoot.SetActive(false);

        // Re-lock cursor and re-enable player controls
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        SetGameplayEnabled(true);
    }

    private void SetGameplayEnabled(bool enabled)
    {
        if (behavioursToDisableWhileOpen == null)
            return;

        foreach (MonoBehaviour b in behavioursToDisableWhileOpen)
        {
            if (b != null)
                b.enabled = enabled;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}