using System.Collections.Generic;
using Mirror;
using UnityEngine;

/// <summary>
/// Networked spawner for items/loot. Replacement for <c>DungeonGenerator.SpawnLoot</c>.
///
/// Because furniture can expose additional loot points after it spawns, call
/// <see cref="AddExtraPoints"/> between the furniture pass and this spawner's
/// <see cref="IDungeonSpawner.Spawn"/> call to include them.
/// </summary>
public class LootSpawner : NetworkDungeonSpawner
{
    [Header("Loot Spawner")]
    [SerializeField] Transform itemsParent;

    public List<ItemBase> SpawnedItems { get; } = new();
    public List<uint> ItemNetIds { get; } = new();

    // Extra points added after furniture spawning (furniture-interior loot slots)
    readonly List<DungeonSpawnPoint> extraPoints = new();

    /// <summary>
    /// Call this after furniture has spawned to register loot points found on
    /// furniture pieces. These are appended to the normal room-level points.
    /// </summary>
    public void AddExtraPoints(IEnumerable<DungeonSpawnPoint> points)
    {
        foreach (var p in points)
            if (p != null) extraPoints.Add(p);
    }

    protected override void OnCollected()
    {
        SpawnedItems.Clear();
        ItemNetIds.Clear();
        extraPoints.Clear();
    }

    public override void Spawn(DungeonGenerator generator)
    {
        // Run the normal base pass first (room-level points)
        base.Spawn(generator);

        // Then run the extra furniture-interior points
        foreach (var point in extraPoints)
        {
            for (int t = 0; t < point.tries; t++)
            {
                if (!EvaluateChance(point, generator)) continue;
                SpawnOne(point, generator);
            }
        }
    }

    protected override void SpawnOne(DungeonSpawnPoint point, DungeonGenerator generator)
    {
        if (!IsServer) return;

        var prog = GameManager.Instance.progressionMod;
        ItemSO item = generator.Theme.GetWeightedItem(point.transform.position, generator.RNG,
            prog.CurrentMinLootTier, prog.CurrentMaxLootTier);

        if (item == null) return;

        // Loot gets fully random rotation unlike furniture
        Quaternion rot = Quaternion.Euler(
            Random.Range(-180f, 180f),
            Random.Range(-180f, 180f),
            Random.Range(-180f, 180f));

        Vector3 pos = ResolvePosition(point);

        GameObject go = NetworkSpawn(item.itemPrefab, pos, rot, itemsParent);
        if (go == null) return;

        if (!go.TryGetComponent<ItemBase>(out var itemBase)) return;
        if (!go.TryGetComponent<NetworkIdentity>(out var ni)) return;

        SpawnedItems.Add(itemBase);
        ItemNetIds.Add(ni.netId);
    }

    protected override void OnSpawnComplete(int count) =>
        Debug.Log($"[LootSpawner] Spawned {count} item(s).");

    protected override void OnClear()
    {
        SpawnedItems.Clear();
        ItemNetIds.Clear();
        extraPoints.Clear();
    }
}
