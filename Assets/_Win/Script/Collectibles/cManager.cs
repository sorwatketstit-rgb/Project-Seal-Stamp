
using System.Collections.Generic;
using UnityEngine;

public class cManager : MonoBehaviour
{
    public static cManager Instance { get; private set; }

    [Header("All 18 pictures")]
    public List<cData> allPictures =
        new List<cData>();

    private readonly HashSet<int> collectedIds =
        new HashSet<int>();

    public int CollectedCount => collectedIds.Count;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public cData GetPictureById(int id)
    {
        return allPictures.Find(
            picture => picture != null && picture.id == id
        );
    }

    public bool IsCollected(int id)
    {
        return collectedIds.Contains(id);
    }

    public bool Collect(cData picture)
    {
        if (picture == null)
            return false;

        if (GetPictureById(picture.id) == null)
        {
            Debug.LogWarning(
                $"Picture #{picture.id} is missing from the manager."
            );
            return false;
        }

        // HashSet.Add returns false if the ID already exists.
        if (!collectedIds.Add(picture.id))
            return false;

        Debug.Log($"Collected #{picture.id}: {picture.pictureName}");

        return true;
    }
}
