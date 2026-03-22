using LethalLive;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static DungeonGenerator;

public class DynamicOcclusionCulling : MonoBehaviour
{
    enum DistanceMethod { Manhattan, Chebyshev }

    [SerializeField] DistanceMethod distanceMethod;
    [SerializeField] bool docActive = false;
    [SerializeField] int triesToUpdate = 10;

    [Header("Fog")]
    [SerializeField] bool fogEnabled = true;
    [SerializeField] float fogStartRatio = 0.35f;
    [SerializeField] float fogEndRatio = 0.5f;

    int updateTries = 0;
    Vector3Int lastCellPos = Vector3Int.zero;
    bool disabledAll = false;

    Dictionary<int, HashSet<int>> validAdjacency = new();
    Dictionary<int, Vector3Int> roomAnchors = new();

    void OnEnable()
    {
        GameTick.OnTick -= UpdateCulling;
        GameTick.OnTick += UpdateCulling;
    }

    void OnDisable()
    {
        GameTick.OnTick -= UpdateCulling;

        if (Instance == null || !Instance.GeneratedDungeon) return;
        foreach (var room in Instance.SpawnedRooms)
            room.Value.SetRender(true);
    }

    public void RebuildValidAdjacency()
    {
        validAdjacency.Clear();
        roomAnchors.Clear();

        foreach (var pr in Instance.PlacedRooms)
        {
            roomAnchors[pr.id] = pr.anchor;

            if (!validAdjacency.ContainsKey(pr.id))
                validAdjacency[pr.id] = new HashSet<int>();

            foreach (var port in pr.data.Ports)
            {
                Vector3Int worldCell = pr.anchor + port.localCell;
                Vector3Int neighborPos = worldCell + DirectionUtils.DirectionVector(port.face);

                if (!Instance.InBounds(neighborPos)) continue;

                var neighborCell = Instance.Grid[neighborPos.x, neighborPos.y, neighborPos.z];
                if (neighborCell?.placedRoom == null) continue;

                var neighborRoom = neighborCell.placedRoom;

                bool hasMatchingPort = neighborRoom.data.Ports.Any(np =>
                    np.type == port.type &&
                    np.localCell == (neighborPos - neighborRoom.anchor) &&
                    np.face == DirectionUtils.OppositeDirection(port.face));

                if (!hasMatchingPort) continue;

                validAdjacency[pr.id].Add(neighborRoom.id);

                if (!validAdjacency.ContainsKey(neighborRoom.id))
                    validAdjacency[neighborRoom.id] = new HashSet<int>();
                validAdjacency[neighborRoom.id].Add(pr.id);
            }
        }
    }

    private void UpdateCulling()
    {
        if (!Instance.GeneratedDungeon || !docActive) return;

        if (validAdjacency.Count == 0) RebuildValidAdjacency();

        PlayerData pData = GameManager.Instance.playMod.LocalPlayer
                                       .Spectator_Movement.GetPlayerData();

        Vector3 playerPos = pData.transform.position;
        Vector3Int cellPosition = new(
            Mathf.RoundToInt(playerPos.x / Instance.CellSize),
            Mathf.RoundToInt(playerPos.y / Instance.CellSize),
            Mathf.RoundToInt(playerPos.z / Instance.CellSize));

        if (!Instance.InBounds(cellPosition) ||
            GameManager.Instance.playMod.LocalPlayer._PlayerInOffice)
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

        var startCell = Instance.Grid[cellPosition.x, cellPosition.y, cellPosition.z];
        if (startCell?.placedRoom == null) return;

        int startId = startCell.placedRoom.id;
        int renderDist = SettingsManager.Instance.UserSettings.GetRenderDistance();

        HashSet<int> coreVisible = new();
        Queue<int> queue = new();

        coreVisible.Add(startId);
        queue.Enqueue(startId);

        while (queue.Count > 0)
        {
            int id = queue.Dequeue();

            if (!validAdjacency.TryGetValue(id, out var neighbors)) continue;

            foreach (int neighborId in neighbors)
            {
                if (coreVisible.Contains(neighborId)) continue;
                if (!roomAnchors.TryGetValue(neighborId, out Vector3Int anchor)) continue;

                distanceMethod = (DistanceMethod)SettingsManager.Instance.UserSettings.GetRenderTypeIndex();
                int cellDistance = distanceMethod switch
                {
                    DistanceMethod.Chebyshev => CellDistance_Chebyshev(cellPosition, anchor),
                    DistanceMethod.Manhattan => CellDistance_Manhattan(cellPosition, anchor),
                    _ => CellDistance_Chebyshev(cellPosition, anchor)
                };

                if (cellDistance <= renderDist)
                {
                    coreVisible.Add(neighborId);
                    queue.Enqueue(neighborId);
                }
            }
        }

        HashSet<int> expandedVisible = new(coreVisible);

        if (SettingsManager.Instance.UserSettings.GetUseRenderSecondPass())
        {
            foreach (int coreId in coreVisible)
            {
                if (!validAdjacency.TryGetValue(coreId, out var neighbors)) continue;
                foreach (int neighborId in neighbors)
                    expandedVisible.Add(neighborId);
            }
        }

        float maxWorldDist = 0f;

        foreach (var r in Instance.SpawnedRooms)
        {
            bool shouldRender = expandedVisible.Contains(r.Key);
            r.Value.SetRender(shouldRender);

            if (shouldRender && fogEnabled &&
                roomAnchors.TryGetValue(r.Key, out Vector3Int rAnchor))
            {
                Vector3 roomWorldPos = (Vector3)rAnchor * Instance.CellSize;
                float dist = Vector3.Distance(playerPos, roomWorldPos);
                float roomRadius = Instance.SpawnedRooms[r.Key]
                                               .PlacedRoom.data.RoomFootprint.Length
                                               * Instance.CellSize;
                maxWorldDist = Mathf.Max(maxWorldDist, dist + roomRadius);
            }
        }

        foreach (var kvp in Instance.RoomFurniture)
        {
            foreach (var furniture in kvp.Value)
            {
                if (furniture == null) continue;
                int roomId = Instance.GetRoomIdAtPosition(furniture.transform.position);
                furniture.SetRender(roomId != -1 && expandedVisible.Contains(roomId));
            }
        }

        foreach (var kvp in Instance.RoomItems)
        {
            foreach (var item in kvp.Value)
            {
                if (item == null) continue;
                int roomId = Instance.GetRoomIdAtPosition(item.transform.position);
                item.SetRender(roomId != -1 && expandedVisible.Contains(roomId));
            }
        }

        if (fogEnabled) UpdateFog(maxWorldDist);
    }

    static int CellDistance_Chebyshev(Vector3Int a, Vector3Int b) =>
        Mathf.Max(Mathf.Abs(a.x - b.x),
                  Mathf.Abs(a.y - b.y),
                  Mathf.Abs(a.z - b.z));

    static int CellDistance_Manhattan(Vector3Int a, Vector3Int b) =>
         Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) + Mathf.Abs(a.z - b.z);

    private void UpdateFog(float maxVisibleDist)
    {
        RenderSettings.fog = true;
        RenderSettings.fogEndDistance = maxVisibleDist * fogEndRatio;
        RenderSettings.fogStartDistance = maxVisibleDist * fogStartRatio;
    }
}
