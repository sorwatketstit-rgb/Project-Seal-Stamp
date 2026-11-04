using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// All customers of one world/city: 9 normal + 3 secret.
/// Create via: Assets > Create > Seal Game > World NPC Set
/// </summary>
[CreateAssetMenu(fileName = "WorldNPCSet_New", menuName = "Seal Game/World NPC Set")]
public class WorldNPCSet : ScriptableObject
{
    public string worldId = "city1";
    public string worldDisplayName = "City 1";

    [Tooltip("Deliveries needed (out of all NPCs, secret included) to unlock the next city.")]
    public int requiredDeliveries = 9;

    public List<LetterNPCData> npcs = new List<LetterNPCData>();

    public int NormalCount => npcs.Count(n => n != null && !n.isSecret);
    public int SecretCount => npcs.Count(n => n != null && n.isSecret);

    private void OnValidate()
    {
        // Soft warning only: the design is 9 normal + 3 secret, but don't block iteration.
        if (npcs.Count > 0 && (NormalCount != 9 || SecretCount != 3))
        {
            Debug.LogWarning(
                $"[{name}] Expected 9 normal + 3 secret NPCs, found {NormalCount} + {SecretCount}.", this);
        }

        var ids = npcs.Where(n => n != null).Select(n => n.id).ToList();
        if (ids.Count != ids.Distinct().Count())
            Debug.LogWarning($"[{name}] Duplicate NPC ids found.", this);
    }
}
