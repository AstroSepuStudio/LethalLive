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
    public AudioReverbPreset reverbPreset = AudioReverbPreset.Cave;

    public int MinItems = 3;
    public int MaxItems = 12;

    private Tier RollTier(Vector3 position, System.Random rng)
    {
        float multiplier = DungeonGenerator.Instance.GetDificultyMultiplier(position);
        Dictionary<Tier, float> modified = GetWeightedTiers(multiplier);

        float totalWeight = 0f;
        foreach (var w in modified.Values)
            totalWeight += w;

        float roll = (float)(rng.NextDouble() * totalWeight);

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

    public FurnitureDataSO GetWeigthedFurniture(Vector3 position, System.Random rng)
    {
        Tier selectedTier = RollTier(position, rng);

        List<FurnitureDataSO> candidates = new();
        foreach (var f in spawnableFurniture)
        {
            if (f.Tier == selectedTier)
                candidates.Add(f);
        }

        if (candidates.Count == 0)
            return spawnableFurniture[(int)(rng.NextDouble() * spawnableFurniture.Length)];

        return candidates[(int)(rng.NextDouble() * candidates.Count)];
    }

    public ItemSO GetWeightedItem(Vector3 position, System.Random rng)
    {
        Tier selectedTier = RollTier(position, rng);

        List<ItemSO> candidates = new();
        foreach (var f in spawnableItems)
        {
            if (f.Tier == selectedTier)
                candidates.Add(f);
        }

        if (candidates.Count == 0)
            return spawnableItems[(int)(rng.NextDouble() * spawnableItems.Length)];

        return candidates[(int)(rng.NextDouble() * candidates.Count)];
    }
}
