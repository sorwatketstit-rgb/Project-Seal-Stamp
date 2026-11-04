using UnityEngine;

/// <summary>
/// One customer slot in a world. Holds NO personal info (no name/species/portrait),
/// only what the delivery system needs.
/// Create via: Assets > Create > Seal Game > Letter NPC Data
/// </summary>
[CreateAssetMenu(fileName = "NPC_New", menuName = "Seal Game/Letter NPC Data")]
public class LetterNPCData : ScriptableObject
{
    [Tooltip("Unique ID, e.g. city1_customer03. Used for saving. Never change after release.")]
    public string id;

    [Tooltip("Secret customers are the 3 hidden ones per world.")]
    public bool isSecret;

    [Header("Rewards")]
    public int letterReward = 5;
    [Tooltip("Optional cosmetic ID given on delivery (leave empty for none).")]
    public string cosmeticRewardId;

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(id))
            id = name.ToLowerInvariant().Replace(" ", "_");
    }
}
