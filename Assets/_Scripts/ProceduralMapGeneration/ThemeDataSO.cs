using System.Collections.Generic;
using UnityEngine;
using static LL_Tier;

[CreateAssetMenu(menuName = "LethalLive/ThemeData")]
public class ThemeDataSO : ScriptableObject
{
    public enum SpawnableSize { Tiny, Small, Medium, Large, ExtraLarge, Colossal }

    [System.Serializable]
    public struct TierEntityGroup
    {
        public Tier tier;
        public EntitySpawn[] entities;
    }

    [System.Serializable]
    public struct EntitySpawn
    {
        public GameObject entityPrefab;
        public float spawnWeight;
        [Range(1, 5)] public int minSpawnCount;
        [Range(1, 5)] public int maxSpawnCount;
    }

    public string levelName;

    [Header("Room Generation")]
    public RoomDataSO startingRoom;
    public RoomDataSO[] spawnableRooms;
    public RoomDataSO[] deadEndRooms;

    [Header("Features Generation")]
    public ItemSO[] spawnableItems;
    public FurnitureDataSO[] spawnableFurniture;
    public TierEntityGroup[] entitySpawnsByTier;
    public DecorationDataSO[] spawnableDecoration;

    public int maxEntities;

    [Header("Ambient")]
    public Color AmbienceClr;
    public float AmbienceIntensity;
    public AudioSFX loopingMusic;
    public AudioSFX[] eerySFX;
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

    public DecorationDataSO GetWeightedDecoration(Vector3 position, SpawnableSize maxSize, System.Random rng)
    {
        Tier selectedTier = RollTier(position, rng);

        List<DecorationDataSO> candidates = new();
        foreach (var f in spawnableDecoration)
        {
            if ((int)f.Size > (int)maxSize) continue;
            if (f.Tier == selectedTier)
                candidates.Add(f);
        }

        if (candidates.Count == 0)
        {
            foreach (var f in spawnableDecoration)
            {
                if ((int)f.Size > (int)maxSize) continue;
                return f;
            }
        }

        return candidates[(int)(rng.NextDouble() * candidates.Count)];
    }

    public EntitySpawn GetWeightedEntitySpawn(Vector3 position, System.Random rng)
    {
        Tier selectedTier = RollTier(position, rng);

        EntitySpawn[] candidates = null;
        foreach (var group in entitySpawnsByTier)
        {
            if (group.tier == selectedTier)
            {
                candidates = group.entities;
                break;
            }
        }

        if (candidates == null || candidates.Length == 0)
        {
            List<EntitySpawn> all = new();
            foreach (var group in entitySpawnsByTier)
                all.AddRange(group.entities);

            float totalWeight = 0f;
            foreach (var spawn in all)
                totalWeight += spawn.spawnWeight;

            float roll = (float)(rng.NextDouble() * totalWeight);
            float cumulative = 0f;
            foreach (var spawn in all)
            {
                cumulative += spawn.spawnWeight;
                if (roll <= cumulative)
                    return spawn;
            }

            return all[0];
        }

        float totalCandidateWeight = 0f;
        foreach (var e in candidates)
            totalCandidateWeight += e.spawnWeight;

        float candidateRoll = (float)(rng.NextDouble() * totalCandidateWeight);
        float cumulativeCandidate = 0f;
        foreach (var e in candidates)
        {
            cumulativeCandidate += e.spawnWeight;
            if (candidateRoll <= cumulativeCandidate)
                return e;
        }

        return candidates[^1];
    }
}
