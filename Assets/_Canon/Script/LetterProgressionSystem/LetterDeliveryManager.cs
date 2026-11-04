using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks which customers have received their letter, fires events, and saves progress.
/// Put one instance in your persistent/bootstrap scene.
/// </summary>
public class LetterDeliveryManager : MonoBehaviour
{
    public static LetterDeliveryManager Instance { get; private set; }

    [SerializeField] private WorldNPCSet currentWorld;
    [SerializeField] private bool autoSave = true;

    // Objective system, minimap and stamp UI subscribe to these.
    public event Action<LetterNPCData> OnDelivered;
    public event Action<int, int> OnProgressChanged;   // delivered, required
    public event Action<WorldNPCSet> OnWorldCompleted; // required count reached

    private readonly HashSet<string> delivered = new HashSet<string>();
    private bool worldCompletedFired;

    public WorldNPCSet CurrentWorld => currentWorld;
    public int DeliveredCount => delivered.Count;
    public int RequiredCount => currentWorld != null ? currentWorld.requiredDeliveries : 0;
    public int TotalCount => currentWorld != null ? currentWorld.npcs.Count : 0;
    public bool CanProgress => currentWorld != null && DeliveredCount >= RequiredCount;

    private string SaveKey => $"letter_npc_{(currentWorld != null ? currentWorld.worldId : "none")}";

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Load();
    }

    public bool IsDelivered(LetterNPCData npc) => npc != null && delivered.Contains(npc.id);

    /// <summary>Returns true if this was a NEW delivery.</summary>
    public bool TryDeliver(LetterNPCData npc)
    {
        if (npc == null || !delivered.Add(npc.id)) return false;

        // TODO: hook up your currency / cosmetic systems:
        // PlayerWallet.Instance.AddLetters(npc.letterReward);
        // if (!string.IsNullOrEmpty(npc.cosmeticRewardId)) CosmeticInventory.Unlock(npc.cosmeticRewardId);

        OnDelivered?.Invoke(npc);
        OnProgressChanged?.Invoke(DeliveredCount, RequiredCount);

        if (!worldCompletedFired && CanProgress)
        {
            worldCompletedFired = true;
            OnWorldCompleted?.Invoke(currentWorld);
        }

        if (autoSave) Save();
        return true;
    }

    public void SetWorld(WorldNPCSet world)
    {
        currentWorld = world;
        Load();
        OnProgressChanged?.Invoke(DeliveredCount, RequiredCount);
    }

    // ---- Saving (PlayerPrefs JSON; swap for your own save system later) ----

    [Serializable] private class SaveData { public List<string> ids = new List<string>(); }

    public void Save()
    {
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(new SaveData { ids = new List<string>(delivered) }));
        PlayerPrefs.Save();
    }

    public void Load()
    {
        delivered.Clear();
        worldCompletedFired = false;

        if (currentWorld != null && PlayerPrefs.HasKey(SaveKey))
        {
            var data = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(SaveKey));
            if (data?.ids != null) foreach (var id in data.ids) delivered.Add(id);
        }

        worldCompletedFired = CanProgress;
    }

    [ContextMenu("Reset Saved Progress")]
    public void ResetProgress()
    {
        PlayerPrefs.DeleteKey(SaveKey);
        delivered.Clear();
        worldCompletedFired = false;
        OnProgressChanged?.Invoke(0, RequiredCount);
    }
}
