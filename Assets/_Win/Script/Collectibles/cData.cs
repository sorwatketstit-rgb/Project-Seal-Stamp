using UnityEngine;

[CreateAssetMenu(
    fileName = "Picture_01",
    menuName = "Seal Stamp/Collectible Picture"
)]
public class cData : ScriptableObject
{
    [Min(1)]
    public int id = 1;

    public string pictureName = "Unknown Picture";

    [Header("Popup Description (Inspection Modal)")]
    [TextArea(3, 8)]
    public string popupDescription;

    [Header("Journal Description (Album Details View)")]
    [TextArea(3, 8)]
    public string journalDescription;

    [HideInInspector]
    public string description; // Kept for backwards compatibility

    public Sprite image;

    public string locationFound = "Unknown location";

    // Fallbacks if one is not filled
    public string GetPopupDescription()
    {
        if (!string.IsNullOrEmpty(popupDescription))
            return popupDescription;
        return description;
    }

    public string GetJournalDescription()
    {
        if (!string.IsNullOrEmpty(journalDescription))
            return journalDescription;
        if (!string.IsNullOrEmpty(description))
            return description;
        return popupDescription;
    }
}