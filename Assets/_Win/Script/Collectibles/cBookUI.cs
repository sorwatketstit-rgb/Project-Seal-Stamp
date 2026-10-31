using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class cBookUI : MonoBehaviour
{
    public static cBookUI Instance { get; private set; }

    [System.Serializable]
    public class PictureSlot
    {
        public Button button;
        public Image pictureImage;
        public TMP_Text numberText;
        public GameObject lockedOverlay;
    }

    [Header("Book")]
    [SerializeField] private GameObject bookRoot;
    [SerializeField] private List<PictureSlot> pictureSlots =
        new List<PictureSlot>();

    [SerializeField] private TMP_Text pageText;
    [SerializeField] private TMP_Text collectionCountText;
    [SerializeField] private Button previousPageButton;
    [SerializeField] private Button nextPageButton;

    [Header("Picture Details")]
    [SerializeField] private GameObject detailsRoot;
    [SerializeField] private Image detailsImage;
    [SerializeField] private TMP_Text detailsNameText;
    [SerializeField] private TMP_Text detailsDescriptionText;
    [SerializeField] private TMP_Text detailsNumberText;
    [SerializeField] private Button closeDetailsButton;

    [Header("Gameplay Controls")]
    [Tooltip("Add your player movement and camera look scripts here.")]
    [SerializeField] private MonoBehaviour[] behavioursToDisableWhileOpen;

    [SerializeField] private bool lockCursorWhenClosed = true;

    private int currentPage = 0;
    private bool isOpen = false;
    private bool escapeWasPressed = false;

    private const int SLOTS_PER_PAGE = 9;

    public bool IsOpen => isOpen;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"[cBookUI] Duplicate cBookUI detected on '{gameObject.name}'! Destroying this component so it won't delete the GameObject.");
            Destroy(this); // DESTROY THE COMPONENT, NOT THE ENTIRE GAMEOBJECT!
            return;
        }

        Instance = this;

        if (bookRoot != null)
            bookRoot.SetActive(false);

        if (detailsRoot != null)
            detailsRoot.SetActive(false);

        if (previousPageButton != null)
            previousPageButton.onClick.AddListener(PreviousPage);

        if (nextPageButton != null)
            nextPageButton.onClick.AddListener(NextPage);

        if (closeDetailsButton != null)
            closeDetailsButton.onClick.AddListener(CloseDetails);

        SetupPictureSlots();
        SetGameplayEnabled(true);
        SetCursorState(false);
    }

    private void OnDestroy()
    {
        Debug.LogWarning($"[cBookUI] OnDestroy called on '{gameObject.name}'!");
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        bool togglePressed = false;
        bool escapePressed = false;

        #if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            togglePressed |= Keyboard.current.jKey.wasPressedThisFrame;
            escapePressed |= Keyboard.current.escapeKey.wasPressedThisFrame;
        }
        #endif

        #if ENABLE_LEGACY_INPUT_MANAGER
        togglePressed |= Input.GetKeyDown(KeyCode.J);
        escapePressed |= Input.GetKeyDown(KeyCode.Escape);
        #endif

        if (togglePressed)
        {
            Debug.Log($"[cBookUI] J key pressed! Current isOpen = {isOpen}. bookRoot is {(bookRoot != null ? bookRoot.name : "NULL")}");

            if (isOpen)
                CloseBook();
            else
                OpenBook();
        }

        if (escapePressed && !escapeWasPressed)
        {
            if (detailsRoot != null && detailsRoot.activeSelf)
                CloseDetails();
            else if (isOpen)
                CloseBook();
        }

        escapeWasPressed = escapePressed;
    }

    private void SetupPictureSlots()
    {
        for (int i = 0; i < pictureSlots.Count; i++)
        {
            int slotIndex = i;

            if (pictureSlots[i].button != null)
            {
                pictureSlots[i].button.onClick.AddListener(
                    () => SelectPicture(slotIndex)
                );
            }
        }
    }

    public void OpenBook()
    {
        if (isOpen)
            return;

        isOpen = true;
        currentPage = Mathf.Max(0, currentPage);

        if (bookRoot != null)
        {
            bookRoot.SetActive(true);
            Debug.Log($"[cBookUI] Set bookRoot '{bookRoot.name}' active = true");
        }
        else
        {
            Debug.LogError("[cBookUI] Cannot open book because bookRoot is NULL!");
        }

        if (detailsRoot != null)
            detailsRoot.SetActive(false);

        SetGameplayEnabled(false);
        SetCursorState(true);
        RefreshPage();
    }

    public void CloseBook()
    {
        if (!isOpen)
            return;

        isOpen = false;

        if (bookRoot != null)
            bookRoot.SetActive(false);

        if (detailsRoot != null)
            detailsRoot.SetActive(false);

        SetGameplayEnabled(true);
        SetCursorState(false);
    }

    public void PreviousPage()
    {
        if (currentPage > 0)
        {
            currentPage--;
            RefreshPage();
        }
    }

    public void NextPage()
    {
        int totalPages = GetTotalPages();

        if (currentPage < totalPages - 1)
        {
            currentPage++;
            RefreshPage();
        }
    }

    public void RefreshPage()
    {
        if (cManager.Instance == null)
        {
            Debug.LogWarning(
                "cBookUI: cManager.Instance was not found."
            );
            return;
        }

        List<cData> pictures = cManager.Instance.allPictures;

        int totalPictures = pictures.Count;
        int totalPages = GetTotalPages();

        currentPage = Mathf.Clamp(currentPage, 0, totalPages - 1);

        int collectedCount = 0;

        for (int i = 0; i < totalPictures; i++)
        {
            if (pictures[i] != null &&
                cManager.Instance.IsCollected(pictures[i].id))
            {
                collectedCount++;
            }
        }

        if (collectionCountText != null)
        {
            collectionCountText.text =
                $"Collected: {collectedCount}/{totalPictures}";
        }

        if (pageText != null)
        {
            pageText.text =
                $"Page {currentPage + 1}/{totalPages}";
        }

        if (previousPageButton != null)
            previousPageButton.interactable = currentPage > 0;

        if (nextPageButton != null)
            nextPageButton.interactable = currentPage < totalPages - 1;

        for (int slotIndex = 0; slotIndex < pictureSlots.Count; slotIndex++)
        {
            PictureSlot slot = pictureSlots[slotIndex];

            int pictureIndex =
                currentPage * SLOTS_PER_PAGE + slotIndex;

            bool hasPicture = pictureIndex < totalPictures;

            cData data = hasPicture ? pictures[pictureIndex] : null;

            bool collected = data != null &&
                cManager.Instance.IsCollected(data.id);

            if (slot.button != null)
                slot.button.interactable = collected;

            if (slot.pictureImage != null)
            {
                slot.pictureImage.sprite =
                    collected ? data.image : null;

                slot.pictureImage.enabled =
                    collected && data.image != null;
            }

            if (slot.numberText != null)
            {
                slot.numberText.text = hasPicture
                    ? $"#{pictureIndex + 1}"
                    : "";
            }

            if (slot.lockedOverlay != null)
            {
                slot.lockedOverlay.SetActive(
                    hasPicture && !collected
                );
            }

            if (slot.button != null)
                slot.button.gameObject.SetActive(hasPicture);
        }
    }

    private void SelectPicture(int slotIndex)
    {
        if (cManager.Instance == null)
            return;

        List<cData> pictures = cManager.Instance.allPictures;

        int pictureIndex =
            currentPage * SLOTS_PER_PAGE + slotIndex;

        if (pictureIndex < 0 || pictureIndex >= pictures.Count)
            return;

        cData data = pictures[pictureIndex];

        if (data == null ||
            !cManager.Instance.IsCollected(data.id))
        {
            return;
        }

        ShowDetails(data, pictureIndex + 1);
    }

    private void ShowDetails(cData data, int entryNumber)
    {
        if (detailsRoot == null || data == null)
            return;

        detailsRoot.SetActive(true);

        if (detailsImage != null)
        {
            detailsImage.sprite = data.image;
            detailsImage.enabled = data.image != null;
        }

        if (detailsNameText != null)
            detailsNameText.text = data.pictureName;

        if (detailsDescriptionText != null)
            detailsDescriptionText.text = data.GetJournalDescription();

        if (detailsNumberText != null)
            detailsNumberText.text = $"#{entryNumber}";
    }

    public void CloseDetails()
    {
        if (detailsRoot != null)
            detailsRoot.SetActive(false);
    }

    private int GetTotalPages()
    {
        int totalPictures = 0;

        if (cManager.Instance != null &&
            cManager.Instance.allPictures != null)
        {
            totalPictures = cManager.Instance.allPictures.Count;
        }

        return Mathf.Max(
            1,
            Mathf.CeilToInt((float)totalPictures / SLOTS_PER_PAGE)
        );
    }

    private void SetGameplayEnabled(bool enabled)
    {
        if (behavioursToDisableWhileOpen == null)
            return;

        foreach (MonoBehaviour behaviour in behavioursToDisableWhileOpen)
        {
            if (behaviour != null)
                behaviour.enabled = enabled;
        }
    }

    private void SetCursorState(bool bookIsOpen)
    {
        if (bookIsOpen)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = lockCursorWhenClosed
                ? CursorLockMode.Locked
                : CursorLockMode.None;

            Cursor.visible = !lockCursorWhenClosed;
        }
    }
}