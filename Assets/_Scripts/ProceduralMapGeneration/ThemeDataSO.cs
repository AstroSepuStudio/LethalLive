using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using static LL_Tier;

[CreateAssetMenu(menuName = "LethalLive/ThemeData")]
public class ThemeDataSO : ScriptableObject
{
    [System.Serializable]
    public struct EntitySpawn
    {
        public GameObject entityPrefab;
        public float spawnWeight;
        public int minSpawnCount;
        public int maxSpawnCount;
    }

    [System.Serializable]
    public struct EerySFX
    {
        public AudioSFX clip;
        public float weight;
    }

    public string levelName;

    [Header("Room Generation")]
    public RoomDataSO startingRoom;
    public RoomDataSO[] spawnableRooms;
    public RoomDataSO[] deadEndRooms;

    [Header("Features Generation")]
    public ItemSO[] spawnableItems;
    public FurnitureDataSO[] spawnableFurniture;
    public EntitySpawn[] entitySpawns;

    [Header("Ambient")]
    public AudioSFX loopingMusic;
    public EerySFX[] eerySFX;

    public int MinItems = 3;
    public int MaxItems = 12;

    private Tier RollTier(Vector3 position)
    {
        float multiplier = DungeonGenerator.Instance.GetDificultyMultiplier(position);
        Dictionary<Tier, float> modified = GetWeightedTiers(multiplier);

        float totalWeight = 0f;
        foreach (var w in modified.Values)
            totalWeight += w;

        float roll = Random.value * totalWeight;

        Tier selectedTier = Tier.Common;
        foreach (var kvp in modified)
        {
            roll -= kvp.Value;
            if (roll <= 0f)
            {
                selectedTier = kvp.Key;
                break;
            }
        }

        return selectedTier;
    }

    public FurnitureDataSO GetWeigthedFurniture(Vector3 position)
    {
        Tier selectedTier = RollTier(position);

        List<FurnitureDataSO> candidates = new();
        foreach (var f in spawnableFurniture)
        {
            if (f.Tier == selectedTier)
                candidates.Add(f);
        }

        if (candidates.Count == 0)
            return spawnableFurniture[Random.Range(0, spawnableFurniture.Length)];

        return candidates[Random.Range(0, candidates.Count)];
    }

    public ItemSO GetWeightedItem(Vector3 position)
    {
        Tier selectedTier = RollTier(position);

        List<ItemSO> candidates = new();
        foreach (var f in spawnableItems)
        {
            if (f.Tier == selectedTier)
                candidates.Add(f);
        }

        if (candidates.Count == 0)
            return spawnableItems[Random.Range(0, spawnableFurniture.Length)];

        return candidates[Random.Range(0, candidates.Count)];
    }
}
