using System.Collections.Generic;
using Mirror;
using UnityEngine;

/// <summary>
/// Networked spawner for furniture. Drop this as a child of the DungeonGenerator
/// GameObject, assign the "Furniture" <see cref="SpawnChannel"/> asset, and wire
/// up the parent transform and theme reference.
/// </summary>
public class FurnitureSpawner : NetworkDungeonSpawner
{
    [Header("Furniture Spawner")]
    [SerializeField] Transform furnitureParent;

    public List<FurnitureEntity> SpawnedFurniture { get; } = new();
    public List<uint> FurnitureNetIds { get; } = new();

    // Extra loot positions discovered on spawned furniture pieces
    readonly List<DungeonSpawnPoint> discoveredLootPoints = new();
    public IReadOnlyList<DungeonSpawnPoint> DiscoveredLootPoints => discoveredLootPoints;

    protected override void OnCollected()
    {
        SpawnedFurniture.Clear();
        FurnitureNetIds.Clear();
        discoveredLootPoints.Clear();
    }

    protected override void SpawnOne(DungeonSpawnPoint point, DungeonGenerator generator)
    {
        if (!IsServer) return;

        FurnitureDataSO data = generator.Theme.GetWeigthedFurniture(point.transform.position, generator.RNG);
        if (data == null) return;

        Vector3 pos = ResolvePosition(point);
        Quaternion rot = ResolveRotation(point);

        GameObject go = NetworkSpawn(data.Prefab, pos, rot, furnitureParent);
        if (go == null) return;

        if (!go.TryGetComponent(out FurnitureEntity furnEnt)) return;
        if (!go.TryGetComponent<NetworkIdentity>(out var ni)) return;

        if (furnEnt.lootPositions != null)
        {
            foreach (var lp in furnEnt.lootPositions)
                if (lp != null) discoveredLootPoints.Add(lp);
        }

        SpawnedFurniture.Add(furnEnt);
        FurnitureNetIds.Add(ni.netId);
    }

    protected override void OnSpawnComplete(int count) =>
        Debug.Log($"[FurnitureSpawner] Spawned {count} furniture piece(s).");

    protected override void OnClear()
    {
        SpawnedFurniture.Clear();
        FurnitureNetIds.Clear();
        discoveredLootPoints.Clear();
    }
}
