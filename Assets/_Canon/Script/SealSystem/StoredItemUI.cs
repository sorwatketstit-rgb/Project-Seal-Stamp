using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shows the stored item's icon. "root" must be a CHILD of this object (or another object),
/// not the object this script sits on, or hiding it would also disable this script.
/// </summary>
public class StoredItemUI : MonoBehaviour
{
    [SerializeField] SealStorage storage;
    [SerializeField] GameObject root;
    [SerializeField] Image icon;

    void OnEnable()
    {
        storage.ItemStored += Show;
        storage.ItemSpat += Hide;

        if (storage.HasItem) Show(storage.Stored);
        else root.SetActive(false);
    }

    void OnDisable()
    {
        storage.ItemStored -= Show;
        storage.ItemSpat -= Hide;
    }

    void Show(Suckable item)
    {
        icon.sprite = item.icon;
        icon.enabled = item.icon != null;
        root.SetActive(true);
    }

    void Hide(Suckable item)
    {
        root.SetActive(false);
    }
}
