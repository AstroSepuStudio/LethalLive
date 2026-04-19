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

    [Header("Ambient")]
    public Color AmbienceClr;
    public float AmbienceIntensity;
    public AudioSFX loopingMusic;
    public AudioSFX[] eerySFX;
    public AudioReverbPreset reverbPreset = AudioReverbPreset.Cave;

    public int MinItems = 3;
    public int MaxItems = 12;

    private Tier RollTier(Vector3 position, System.Random rng, Tier min, Tier max)
    {
        float multiplier = DungeonGenerator.Instance.GetDificultyMultiplier(position);
        Dictionary<Tier, float> modified = GetWeightedTiers(multiplier);

        float totalWeight = 0f;
        foreach (var kvp in modified)
            if (kvp.Key >= min && kvp.Key <= max) totalWeight += kvp.Value;

        if (totalWeight <= 0f) return min;

        float roll = (float)(rng.NextDouble() * totalWeight);
        foreach (var kvp in modified)
        {
            if (kvp.Key < min || kvp.Key > max) continue;
            roll -= kvp.Value;
            if (roll <= 0f) return kvp.Key;
        }

        return min;
    }

    public FurnitureDataSO GetWeigthedFurniture( Vector3 position, System.Random rng,
        Tier min = Tier.Common, Tier max = Tier.Legendary)
    {
        Tier selectedTier = RollTier(position, rng, min, max);

        List<FurnitureDataSO> candidates = new();
        foreach (var f in spawnableFurniture)
            if (f.Tier >= min && f.Tier <= max && f.Tier == selectedTier)
                candidates.Add(f);

        if (candidates.Count == 0)
            foreach (var f in spawnableFurniture)
                if (f.Tier >= min && f.Tier <= max)
                    candidates.Add(f);

        if (candidates.Count == 0)
            return spawnableFurniture[(int)(rng.NextDouble() * spawnableFurniture.Length)];

        return candidates[(int)(rng.NextDouble() * candidates.Count)];
    }

    public ItemSO GetWeightedItem(Vector3 position, System.Random rng,
        Tier min = Tier.Common, Tier max = Tier.Legendary)
    {
        Tier selectedTier = RollTier(position, rng, min, max);

        List<ItemSO> candidates = new();
        foreach (var item in spawnableItems)
            if (item.Tier >= min && item.Tier <= max && item.Tier == selectedTier)
                candidates.Add(item);

        if (candidates.Count == 0)
            foreach (var item in spawnableItems)
                if (item.Tier >= min && item.Tier <= max)
                    candidates.Add(item);

        if (candidates.Count == 0)
            return spawnableItems[(int)(rng.NextDouble() * spawnableItems.Length)];

        return candidates[(int)(rng.NextDouble() * candidates.Count)];
    }

    public DecorationDataSO GetWeightedDecoration(Vector3 position, SpawnableSize maxSize, System.Random rng,
        Tier min = Tier.Common, Tier max = Tier.Legendary)
    {
        Tier selectedTier = RollTier(position, rng, min, max);

        List<DecorationDataSO> candidates = new();
        foreach (var d in spawnableDecoration)
        {
            if ((int)d.Size > (int)maxSize) continue;
            if (d.Tier >= min && d.Tier <= max && d.Tier == selectedTier)
                candidates.Add(d);
        }

        if (candidates.Count == 0)
            foreach (var d in spawnableDecoration)
            {
                if ((int)d.Size > (int)maxSize) continue;
                if (d.Tier >= min && d.Tier <= max)
                    candidates.Add(d);
            }

        if (candidates.Count == 0)
            foreach (var d in spawnableDecoration)
                if ((int)d.Size <= (int)maxSize)
                    return d;

        if (candidates.Count == 0) return null;
        return candidates[(int)(rng.NextDouble() * candidates.Count)];
    }

    public EntitySpawn GetWeightedEntitySpawn(
        Vector3 position, System.Random rng,
        Tier min = Tier.Common, Tier max = Tier.Legendary)
    {
        Tier selectedTier = RollTier(position, rng, min, max);

        EntitySpawn[] tieredCandidates = null;
        foreach (var group in entitySpawnsByTier)
        {
            if (group.tier == selectedTier && group.tier >= min && group.tier <= max)
            {
                tieredCandidates = group.entities;
                break;
            }
        }

        if (tieredCandidates == null || tieredCandidates.Length == 0)
        {
            List<EntitySpawn> windowCandidates = new();
            foreach (var group in entitySpawnsByTier)
            {
                if (group.tier < min || group.tier > max) continue;
                windowCandidates.AddRange(group.entities);
            }

            if (windowCandidates.Count > 0)
                return RollWeighted(windowCandidates, rng);

            List<EntitySpawn> all = new();
            foreach (var group in entitySpawnsByTier)
                all.AddRange(group.entities);

            return all.Count > 0 ? RollWeighted(all, rng) : default;
        }

        return RollWeighted(new List<EntitySpawn>(tieredCandidates), rng);
    }

    static EntitySpawn RollWeighted(List<EntitySpawn> pool, System.Random rng)
    {
        float total = 0f;
        foreach (var e in pool) total += e.spawnWeight;

        float roll = (float)(rng.NextDouble() * total);
        foreach (var e in pool)
        {
            roll -= e.spawnWeight;
            if (roll <= 0f) return e;
        }

        return pool[^1];
    }
}
