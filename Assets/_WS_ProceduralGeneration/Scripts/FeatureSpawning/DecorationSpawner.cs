using UnityEngine;

/// <summary>
/// Networked spawner for decoration pieces. Replacement for <c>DungeonGenerator.SpawnDecoration</c>.
///
/// Decoration uses an accumulating chance (each failed roll increases the next one),
/// which is slightly different from the flat per-point model — override
/// <see cref="DungeonSpawner.EvaluateChance"/> here to keep that behaviour.
/// </summary>
public class DecorationSpawner : NetworkDungeonSpawner
{
    [Header("Decoration Spawner")]
    [SerializeField] Transform decoParent;
    [SerializeField] float baseChance = 1f;
    float accumulatedChance;

    protected override void OnCollected()
    {
        accumulatedChance = baseChance;
    }

    // Decoration skips the default EvaluateChance and manages its own accumulating roll
    public override void Spawn(DungeonGenerator generator)
    {
        accumulatedChance = baseChance;
        int spawned = 0;

        foreach (var point in CollectedPoints)
        {
            if (!EvaluateAccumulating(point, generator))
            {
                accumulatedChance += baseChance;
                continue;
            }

            accumulatedChance = baseChance;
            SpawnOne(point, generator);
            spawned++;
        }

        OnSpawnComplete(spawned);
    }

    bool EvaluateAccumulating(DungeonSpawnPoint point, DungeonGenerator generator)
    {
        float roll = (float)(generator.RNG.NextDouble() * 100f);
        float effective = accumulatedChance * generator.GetDificultyMultiplier(point.transform.position);
        return roll <= effective;
    }

    protected override void SpawnOne(DungeonSpawnPoint point, DungeonGenerator generator)
    {
        if (!IsServer) return;

        DecorationSpawnPoint docSpawn = point as DecorationSpawnPoint;
        if (docSpawn == null) return;

        DecorationDataSO deco = generator.Theme.GetWeightedDecoration(
            point.transform.position, docSpawn.maxSize, generator.RNG);
        if (deco == null) return;

        NetworkSpawn(deco.Prefab, ResolvePosition(point, generator.RNG), ResolveRotation(point, generator.RNG), decoParent);
    }

    protected override void OnClear()
    {
        DestroyChildren(decoParent, true, IsServer);
    }

    protected override void OnSpawnComplete(int count) =>
        Debug.Log($"[DecorationSpawner] Spawned {count} decoration(s).");
}
