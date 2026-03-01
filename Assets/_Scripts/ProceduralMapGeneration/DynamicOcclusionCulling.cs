using System.Collections.Generic;
using UnityEngine;
using static DungeonGenerator;

public class DynamicOcclusionCulling : MonoBehaviour
{
    [SerializeField] int renderDistance = 3;
    [SerializeField] int triesToUpdate = 10;

    int updateTries = 0;
    Vector3Int lastCellPos = Vector3Int.zero;
    bool disabledAll = false;

    void OnEnable() { GameTick.OnTick += UpdateCulling; }
    void OnDisable() { GameTick.OnTick -= UpdateCulling; }

    private void UpdateCulling()
    {
        if (!Instance.GeneratedDungeon) return;

        PlayerData pData = GameManager.Instance.playMod.LocalPlayer.Spectator_Movement.GetPlayerData();

        Vector3 playerPos = pData.transform.position;
        Vector3Int cellPosition = new(
            Mathf.RoundToInt(playerPos.x) / Instance.CellSize,
            Mathf.RoundToInt(playerPos.y) / Instance.CellSize,
            Mathf.RoundToInt(playerPos.z) / Instance.CellSize
        );

        if (!Instance.InBounds(cellPosition))
        {
            if (!disabledAll)
            {
                foreach (var room in Instance.SpawnedRooms)
                    room.Value.SetRender(false);
                disabledAll = true;
            }
            return;
        }

        disabledAll = false;

        if (lastCellPos == cellPosition && updateTries < triesToUpdate)
        {
            updateTries++;
            return;
        }
        lastCellPos = cellPosition;
        updateTries = 0;

        var grid = Instance.Grid;
        var startCell = grid[cellPosition.x, cellPosition.y, cellPosition.z];
        if (startCell?.placedRoom == null) return;

        int startId = startCell.placedRoom.id;
        var adjacency = Instance.RoomAdjacency;

        HashSet<int> coreVisible = new();
        Queue<(int id, int depth)> queue = new();

        coreVisible.Add(startId);
        queue.Enqueue((startId, 0));

        while (queue.Count > 0)
        {
            var (id, depth) = queue.Dequeue();

            if (depth >= renderDistance) continue;

            if (!adjacency.TryGetValue(id, out var neighbors)) continue;

            foreach (var neighborId in neighbors)
            {
                if (coreVisible.Contains(neighborId)) continue;
                coreVisible.Add(neighborId);
                queue.Enqueue((neighborId, depth + 1));
            }
        }

        // Bleeding pass
        HashSet<int> expanded = new(coreVisible);
        if (adjacency.TryGetValue(startId, out var playerRoomNeighbors))
        {
            foreach (var neighborId in playerRoomNeighbors)
                expanded.Add(neighborId);
        }

        foreach (var r in Instance.SpawnedRooms)
            r.Value.SetRender(expanded.Contains(r.Key));
    }
}
