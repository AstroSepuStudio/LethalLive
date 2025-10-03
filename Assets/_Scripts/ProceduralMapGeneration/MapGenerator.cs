using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.InputSystem;

[Serializable]
public struct LootPosition
{
    public Transform position;
    public Vector3 maxOffset;
    public float chance;
}

[Serializable]
public struct FurniturePosition
{
    public Transform position;
    public Vector3 maxOffset;
    public float maxRotation;
    public float chance;
}

public class MapGenerator : MonoBehaviour
{
    public static MapGenerator Instance;

    [Header("Content")]
    [SerializeField] ThemeDataSO theme;
    [SerializeField] GameObject entitySpawnerPref;
    [SerializeField] Transform roomsParent;
    [SerializeField] Transform furnitureParent;
    [SerializeField] Transform itemsParent;

    [SerializeField] NavMeshSurface surface;

    [Header("Seed")]
    [SerializeField] string gameSeed = "Default";
    [SerializeField] int seed;

    [Header("Grid")]
    [SerializeField] Vector3Int gridSize = new(24, 6, 24);
    [SerializeField] int cellSize = 6;

    [Header("Limits")]
    [SerializeField] int maxRooms = 30;
    [SerializeField] int maxDepth = 30;

    [Header("Debug")]
    [SerializeField] PlayerInput pInput;
    [SerializeField] bool generateOnStart = false;
    [SerializeField] bool stepGenerationMode = false;
    [SerializeField] GameObject seedDisplayCanvas;
    [SerializeField] TextMeshProUGUI seedDisplayTxt;

    System.Random rng;
    Cell[,,] grid;
    readonly List<PlacedRoom> placed = new();
    readonly Dictionary<int, RoomData> spawned = new();
    int nextRoomId = 1;
    bool _generated;

    readonly List<LootPosition> lootPositions = new();
    readonly List<FurniturePosition> furniturePositions = new();
    readonly List<Transform> entitySpawnerPositions = new();

    private struct OpenPort
    {
        public int roomId;
        public Vector3Int worldCell;
        public Direction face;
        public Vector3Int localCell;
        public int depth;
        public RoomDataSO.PortType type;
    }

    private class PlacedRoom
    {
        public int id;
        public RoomDataSO data;
        public Vector3Int anchor;
        public int depth;
        public Biome biome;
    }

    private class Cell
    {
        public PlacedRoom placedRoom;
        public Vector3Int local;
    }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (generateOnStart)
            StartGeneration(StableHash(gameSeed));
    }

    public void StartGeneration(int setSeed = -1)
    {
        if (_generated) return;
        _generated = true;

        int size = LobbySettings.Instance.MapSize;
        
        grid = new Cell[gridSize.x * size, gridSize.y * size, gridSize.z * size];
        
        for (int x = 0; x < gridSize.x * size; x++)
            for (int y = 0; y < gridSize.y * size; y++)
                for (int z = 0; z < gridSize.z * size; z++)
                    grid[x, y, z] = new Cell();

        seed = setSeed;
        rng = new System.Random(seed);

        StartCoroutine(DisplaySeed(setSeed, 5f));

        if (stepGenerationMode)
        {
            pInput.enabled = true;
            //StartStepGeneration();
        }
        else
        {
            pInput.enabled = false;

            Generate();
            InstantiateRooms();
            ResolveDoors();
        }
    }

    private void Generate()
    {
        if (theme == null || theme.startingRoom == null) { Debug.LogError("Theme or startingRoom is missing."); return; }
        var center = new Vector3Int(gridSize.x / 2, Mathf.Clamp(gridSize.y / 2, 0, gridSize.y - 1), gridSize.z / 2); 
        var start = Place(theme.startingRoom, center, depth: 0);

        if (GameManager.Instance.isServer)
            GameManager.Instance.startRoomPos = center * cellSize;

        if (start == null) 
        { 
            Debug.LogError("Failed to place starting room at center."); 
            return; 
        }

        var frontier = BuildOpenPorts(start); 
        while (frontier.Count > 0 && placed.Count < maxRooms * LobbySettings.Instance.MapSize) 
        { 
            int idx = rng.Next(frontier.Count); 
            var open = frontier[idx]; frontier.RemoveAt(idx); 

            if (open.depth >= maxDepth * LobbySettings.Instance.MapSize) continue;

            var neighborRoom = placed.Find(r => r.id == open.roomId);
            var candidates = GetWeightedCandidates(theme.spawnableRooms, neighborRoom.biome);

            bool placedAny = false; 

            for (int c = 0; c < candidates.Length; c++) 
            { 
                var cand = candidates[c]; 
                if (cand == null) continue; 

                for (int p = 0; p < cand.Ports.Length; p++) 
                { 
                    var port = cand.Ports[p];

                    if (port.type != open.type) continue;
                    if (port.face != DirectionUtils.OppositeDirection(open.face)) continue; 

                    var anchor = open.worldCell + DirectionUtils.DirectionVector(open.face) - port.localCell; 
                    if (!FootprintFits(cand, anchor)) continue; 

                    var pr = Place(cand, anchor, open.depth + 1); 
                    if (pr == null) continue; 

                    var newPorts = BuildOpenPorts(pr); 
                    for (int i = newPorts.Count - 1; i >= 0; i--) 
                    { 
                        var np = newPorts[i]; 
                        if (np.worldCell == open.worldCell && np.face == DirectionUtils.OppositeDirection(open.face)) 
                        { 
                            newPorts.RemoveAt(i); 
                            break; 
                        } 
                    } 
                    
                    frontier.AddRange(newPorts); 
                    placedAny = true; 
                    break; 
                } 
                if (placedAny) break; 
            } 
        }
    }

    private PlacedRoom Place(RoomDataSO data, Vector3Int anchor, int depth)
    {
        if (!FootprintFits(data, anchor)) return null;

        var placedRoom = new PlacedRoom
        {
            id = nextRoomId++,
            data = data,
            anchor = anchor,
            depth = depth,
            biome = data.biome
        };
        placed.Add(placedRoom);

        foreach (var local in data.Footprint)
        {
            var w = anchor + local;
            var cell = grid[w.x, w.y, w.z];
            cell.placedRoom = placedRoom;
            cell.local = local;
        }

        return placedRoom;
    }

    private List<OpenPort> BuildOpenPorts(PlacedRoom pr)
    {
        var list = new List<OpenPort>(pr.data.Ports.Length);
        foreach (var port in pr.data.Ports)
        {
            if (!pr.data.ContainsLocalCell(port.localCell))
                continue;

            var worldCell = pr.anchor + port.localCell;
            list.Add(new OpenPort
            {
                roomId = pr.id,
                worldCell = worldCell,
                face = port.face,
                localCell = port.localCell,
                depth = pr.depth,
                type = port.type
            });
        }
        return list;
    }

    #region Fit / bounds / RNG
    private bool InBounds(Vector3Int p) =>
        p.x >= 0 && p.y >= 0 && p.z >= 0 &&
        p.x < gridSize.x && p.y < gridSize.y && p.z < gridSize.z;

    private bool FootprintFits(RoomDataSO data, Vector3Int anchor)
    {
        if (data == null) return false;
        for (int i = 0; i < data.Footprint.Length; i++)
        {
            var w = anchor + data.Footprint[i];
            if (!InBounds(w)) return false;
            if (grid[w.x, w.y, w.z].placedRoom != null) return false;
        }
        return true;
    }

    private static int StableHash(string s)
    {
        unchecked
        {
            const int prime = 16777619;
            int hash = (int)2166136261;
            for (int i = 0; i < s.Length; i++)
            {
                hash = (hash ^ s[i]) * prime;
            }
            return hash;
        }
    }

    private void Shuffle<T>(IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)    
        {
            int j = rng.Next(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private RoomDataSO[] GetWeightedCandidates(RoomDataSO[] candidates, Biome neighborBiome)
    {
        List<RoomDataSO> weighted = new();
        foreach (var cand in candidates)
        {
            if (cand == null) continue;

            weighted.Add(cand);

            if (cand.biome == neighborBiome)
            {
                weighted.Add(cand);
                weighted.Add(cand);
            }
        }

        Shuffle(weighted);
        return weighted.ToArray();
    }

    #endregion

    #region Instantiate
    private void InstantiateRooms()
    {
        foreach (var pr in placed)
        {
            var world = Vector3.Scale((Vector3)pr.anchor, new Vector3(cellSize, cellSize, cellSize));
            var go = Instantiate(pr.data.Prefab, world, Quaternion.identity, roomsParent);
            go.name = $"Room {pr.id} (depth {pr.depth})";
            if (!go.TryGetComponent<RoomData>(out var rd)) 
                Debug.LogWarning($"Prefab {pr.data.Prefab.name} lacks RoomData component.");

            spawned[pr.id] = rd;

            furniturePositions.AddRange(rd.furnitureSpawnPositions);
            lootPositions.AddRange(rd.itemSpawnPositions);
            entitySpawnerPositions.AddRange(rd.entitySpawnerPositions);
        }

        if (GameManager.Instance != null)
        {
            if (GameManager.Instance.LocalPlayer.isServer)
            {
                SpawnFurniture();
                SpawnLoot();
                SetEntitySpawners();
            }
        }

        StartCoroutine(GenerateNavMeshSurface());
    }

    [Server]
    public void SpawnFurniture()
    {
        if (theme.furniturePrefabs.Length <= 0) return;

        foreach (var pos in furniturePositions)
        {
            float rand = UnityEngine.Random.Range(0, 100f);
            if (rand > pos.chance) continue;

            int furnIndex = UnityEngine.Random.Range(0, theme.furniturePrefabs.Length);

            Quaternion rot = pos.position.rotation * Quaternion.Euler(0f, UnityEngine.Random.Range(-pos.maxRotation, pos.maxRotation), 0f);
            Vector3 offset = new (UnityEngine.Random.Range(-pos.maxOffset.x, pos.maxOffset.x), 
                UnityEngine.Random.Range(-pos.maxOffset.y, pos.maxOffset.y),
                UnityEngine.Random.Range(-pos.maxOffset.z, pos.maxOffset.z));

            GameObject furnObj = Instantiate(theme.furniturePrefabs[furnIndex], pos.position.position + offset, rot, furnitureParent);
            NetworkServer.Spawn(furnObj);

            furnObj.transform.Rotate(Vector3.up * UnityEngine.Random.Range(-pos.maxRotation, pos.maxRotation));

            FurnitureEntity furnEnt = furnObj.GetComponent<FurnitureEntity>();
            lootPositions.AddRange(furnEnt.lootPositions);
        }
    }

    [Server]
    public void SpawnLoot()
    {
        if (theme.spawnableItems.Length <= 0) return;

        foreach(var pos in lootPositions)
        {
            float rand = UnityEngine.Random.Range(0, 100f);
            if (rand > pos.chance) continue;

            Vector3 offset = new(UnityEngine.Random.Range(-pos.maxOffset.x, pos.maxOffset.x),
                UnityEngine.Random.Range(-pos.maxOffset.y, pos.maxOffset.y),
                UnityEngine.Random.Range(-pos.maxOffset.z, pos.maxOffset.z));

            int itemIndex = UnityEngine.Random.Range(0, theme.spawnableItems.Length);

            GameObject itemObj = Instantiate(theme.spawnableItems[itemIndex].itemPrefab, pos.position.position + offset, pos.position.rotation, itemsParent);
            NetworkServer.Spawn(itemObj);
        }
    }

    [Server]
    public void SetEntitySpawners()
    {
        if (entitySpawnerPositions == null || entitySpawnerPositions.Count == 0) return;

        int quantity = UnityEngine.Random.Range(4, entitySpawnerPositions.Count);
        List<Transform> positions = new(entitySpawnerPositions);
        List<Transform> spawnedPos = new();

        for (int i = 0; i < quantity; i++)
        {
            int pos = UnityEngine.Random.Range(0, positions.Count);

            GameObject spawner = Instantiate(entitySpawnerPref, positions[pos].position, positions[pos].rotation,transform);
            NetworkServer.Spawn(spawner);

            spawnedPos.Add(positions[pos]);
            positions.RemoveAt(pos);
        }

        EntitySpawnerManager.Instance.SetSpawnerPositions(spawnedPos);
    }

    private void ResolveDoors()
    {
        foreach (var pr in placed)
        {
            if (!spawned.TryGetValue(pr.id, out var rd) || rd == null) continue;

            foreach (var port in pr.data.Ports)
            {
                var worldCell = pr.anchor + port.localCell;
                var nb = worldCell + DirectionUtils.DirectionVector(port.face);

                bool open = false;
                if (InBounds(nb))
                {
                    var nOcc = grid[nb.x, nb.y, nb.z].placedRoom;
                    if (nOcc != null)
                    {
                        // Verify neighbor exposes matching port at its corresponding local cell/face
                        var neighborLocal = nb - nOcc.anchor;
                        foreach (var np in nOcc.data.Ports)
                        {
                            if (np.type == port.type &&
                                np.localCell == neighborLocal &&
                                np.face == DirectionUtils.OppositeDirection(port.face))
                            {
                                open = true;
                                break;
                            }
                        }
                    }
                }

                rd.SetPort(port.localCell, port.face, open);
            }
        }
    }

    IEnumerator GenerateNavMeshSurface()
    {
        yield return null;

        surface.BuildNavMesh();
    }
    #endregion

    public bool GeneratedDungeon 
    { 
        get
        {
            if (spawned == null || placed == null) return false;
            if (spawned.Count <= 0 || placed.Count <= 0) return false;

            return true;
        }
    }

    IEnumerator DisplaySeed(int seed, float duration)
    {
        seedDisplayCanvas.SetActive(true);
        seedDisplayTxt.SetText($"Seed: {seed}");

        yield return new WaitForSeconds(duration);

        seedDisplayCanvas.SetActive(false);
    }

    public Vector3 GetRandomPosition(float maxOffset = 4f)
    {
        int index = UnityEngine.Random.Range(0, spawned.Count);
        var world = Vector3.Scale((Vector3)placed[index].anchor, new Vector3(cellSize, cellSize, cellSize));

        world.x += UnityEngine.Random.Range(-maxOffset, maxOffset);
        world.z += UnityEngine.Random.Range(-maxOffset, maxOffset);

        return world;
    }
}
