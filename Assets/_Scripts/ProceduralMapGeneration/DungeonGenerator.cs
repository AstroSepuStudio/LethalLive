using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public struct LootPosition
{
    public Transform position;
    public Vector3 maxOffset;
    public float chance;
}

public class DungeonGenerator : NetworkBehaviour
{
    public static DungeonGenerator Instance;

    [Header("Content")]
    [SerializeField] ThemeDataSO theme;
    [SerializeField] GameObject entitySpawnerPref;
    [SerializeField] Transform roomsParent;
    [SerializeField] Transform furnitureParent;
    [SerializeField] Transform itemsParent;
    [SerializeField] Transform entSpawnParent;
    [SerializeField] Transform voidDestroyerParent;
    [SerializeField] NavMeshSurface surface;
    [SerializeField]
    private AnimationCurve difficultyCurve = AnimationCurve.EaseInOut(0, 1, 20, 3);

    [Header("Audio")]
    [SerializeField] AudioReverbZone reverbZone;
    [SerializeField] float reverbMinDistanceMultiplier = 0.5f;
    [SerializeField] float reverbMaxDistanceMultiplier = 1.5f;

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
    [SerializeField] bool generateOnStart = false;
    [SerializeField] int simMapSize = 10;
    [SerializeField] GameObject seedDisplayCanvas;
    [SerializeField] RectTransform seedDisplayTransform;
    [SerializeField] CanvasGroup seedDisplayGroup;
    [SerializeField] TextMeshProUGUI seedDisplayTxt;

    public System.Random RNG { get; private set; }

    Cell[,,] grid;
    readonly List<PlacedRoom> placed = new();
    readonly Dictionary<int, RoomData> spawned = new();
    int nextRoomId = 1;
    bool _generated;

    readonly List<LootPosition> lootPositions = new();
    readonly List<FurnitureDataSO.FurniturePosition> furniturePositions = new();
    readonly List<Transform> entitySpawnerPositions = new();

    public IReadOnlyDictionary<int, RoomData> SpawnedRooms => spawned;
    public IReadOnlyList<PlacedRoom> PlacedRooms => placed;

    public Dictionary<int, List<FurnitureEntity>> RoomFurniture = new();
    public Dictionary<int, List<ItemBase>> RoomItems = new();

    public ThemeDataSO Theme => theme;
    public Cell[,,] Grid => grid;
    private Vector3Int effectiveGridSize;

    public int CellSize => cellSize;
    public Vector3Int GridSize => effectiveGridSize;
    public UnityEvent OnDungeonGenerated;
    public UnityEvent OnDungeonClear;
    public Dictionary<int, HashSet<int>> RoomAdjacency = new();
    public Vector3Int StartRoomPos;

    [SerializeField] float maxDistance = 0;
    public float MaxDistance => maxDistance;

    private struct OpenPort
    {
        public int roomId;
        public Vector3Int worldCell;
        public Direction face;
        public Vector3Int localCell;
        public int depth;
        public RoomDataSO.PortType type;
    }

    public class PlacedRoom
    {
        public int id;
        public RoomDataSO data;
        public Vector3Int anchor;
        public int depth;
        public Biome biome;
    }

    public class Cell
    {
        public PlacedRoom placedRoom;
        public Vector3Int local;
    }

    private float RandomRange(float min, float max) =>
    (float)(RNG.NextDouble() * (max - min)) + min;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (generateOnStart)
            StartGeneration(StableHash(gameSeed), 0);
    }

    public void StartGeneration(int setSeed, int themeIndex)
    {
        if (_generated) return;
        _generated = true;

        theme = GameManager.Instance.dngMod.ThemeDatas[themeIndex];

        int size = Mathf.Max(1, LobbySettings.Instance.MapSize);
        effectiveGridSize = new Vector3Int(gridSize.x * size, gridSize.y * size, gridSize.z * size);
        voidDestroyerParent.localScale = new Vector3(size + 2, 1, size + 2);

        grid = new Cell[effectiveGridSize.x, effectiveGridSize.y, effectiveGridSize.z];

        for (int x = 0; x < gridSize.x * size; x++)
            for (int y = 0; y < gridSize.y * size; y++)
                for (int z = 0; z < gridSize.z * size; z++)
                    grid[x, y, z] = new Cell();

        seed = setSeed;
        RNG = new System.Random(seed);

        StartCoroutine(GenerationCoroutine(setSeed, 5f));
    }

    private void Generate()
    {
        if (theme == null || theme.startingRoom == null) { Debug.LogError("Theme or startingRoom is missing."); return; }

        var center = new Vector3Int(
            effectiveGridSize.x / 2,
            Mathf.Clamp(effectiveGridSize.y / 2, 0, effectiveGridSize.y - 1),
            effectiveGridSize.z / 2 );

        var start = Place(theme.startingRoom, center, depth: 0);
       
        if (start == null) 
        {
            center = new Vector3Int(1, 0, 0);
            start = Place(theme.startingRoom, center, depth: 0);
            Debug.Log("Tried to place start room in default position."); 
        }

        if (start == null)
        {
            Debug.LogError("Failed to place starting room at center.");
            return;
        }

        StartRoomPos = center * cellSize;
        if (GameManager.Instance.isServer)
            GameManager.Instance.dngMod.startRoomPos = StartRoomPos;

        var frontier = BuildOpenPorts(start); 
        while (frontier.Count > 0 && placed.Count < maxRooms * LobbySettings.Instance.MapSize) 
        { 
            int idx = RNG.Next(frontier.Count); 
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

        foreach (var local in data.RoomFootprint)
        {
            var w = anchor + local.Footprint;
            var cell = grid[w.x, w.y, w.z];
            cell.placedRoom = placedRoom;
            cell.local = local.Footprint;
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
    public bool InBounds(Vector3Int p) =>
        p.x >= 0 && p.y >= 0 && p.z >= 0 &&
        p.x < effectiveGridSize.x && p.y < effectiveGridSize.y && p.z < effectiveGridSize.z;

    private bool FootprintFits(RoomDataSO data, Vector3Int anchor)
    {
        if (data == null) return false;
        for (int i = 0; i < data.RoomFootprint.Length; i++)
        {
            var w = anchor + data.RoomFootprint[i].Footprint;
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
            int j = RNG.Next(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private RoomDataSO[] GetWeightedCandidates(RoomDataSO[] candidates, Biome neighborBiome)
    {
        LL_Tier.Tier rolledTier = RollTier(LL_Tier.BaseTierWeights);

        List<RoomDataSO> weighted = new();
        foreach (var cand in candidates)
        {
            if (cand == null) continue;
            if (cand.RoomTier != rolledTier) continue;

            weighted.Add(cand);
            if (cand.biome == neighborBiome)
            {
                weighted.Add(cand);
                weighted.Add(cand);
            }
        }

        if (weighted.Count == 0)
        {
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
        }

        Shuffle(weighted);
        return weighted.ToArray();
    }

    private LL_Tier.Tier RollTier(Dictionary<LL_Tier.Tier, float> weights)
    {
        float total = 0f;
        foreach (var kvp in weights) total += kvp.Value;

        float roll = (float)(RNG.NextDouble() * total);
        foreach (var kvp in weights)
        {
            roll -= kvp.Value;
            if (roll <= 0f) return kvp.Key;
        }

        return LL_Tier.Tier.Common;
    }

    void BuildRoomAdjacency()
    {
        foreach (var pr in placed)
        {
            if (!RoomAdjacency.ContainsKey(pr.id))
                RoomAdjacency[pr.id] = new();

            foreach (var port in pr.data.Ports)
            {
                Vector3Int worldCell = pr.anchor + port.localCell;
                Vector3Int neighborPos = worldCell + DirectionUtils.DirectionVector(port.face);

                if (!InBounds(neighborPos)) continue;

                var neighborCell = grid[neighborPos.x, neighborPos.y, neighborPos.z];
                if (neighborCell?.placedRoom == null) continue;

                var neighborRoom = neighborCell.placedRoom;

                // Verify the neighbor actually exposes a matching port back
                bool hasMatchingPort = false;
                foreach (var np in neighborRoom.data.Ports)
                {
                    if (np.type == port.type &&
                        np.localCell == neighborPos - neighborRoom.anchor &&
                        np.face == DirectionUtils.OppositeDirection(port.face))
                    {
                        hasMatchingPort = true;
                        break;
                    }
                }

                if (!hasMatchingPort) continue;

                RoomAdjacency[pr.id].Add(neighborRoom.id);
            }
        }
    }

    public int GetRoomIdAtPosition(Vector3 worldPos)
    {
        Vector3Int cellPos = new(
            Mathf.RoundToInt(worldPos.x) / cellSize,
            Mathf.RoundToInt(worldPos.y) / cellSize,
            Mathf.RoundToInt(worldPos.z) / cellSize
        );

        if (!InBounds(cellPos)) return -1;
        return grid[cellPos.x, cellPos.y, cellPos.z]?.placedRoom?.id ?? -1;
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

            rd.PlacedRoom = pr;
            spawned[pr.id] = rd;
            Vector3 initialRoomPos = spawned[placed[0].id].transform.position;
            float distance = Vector3.Distance(initialRoomPos, go.transform.position);
            maxDistance = distance > maxDistance ? distance : maxDistance;

            furniturePositions.AddRange(rd.furnitureSpawnPositions);
            lootPositions.AddRange(rd.itemSpawnPositions);
            entitySpawnerPositions.AddRange(rd.entitySpawnerPositions);
        }

        BuildRoomAdjacency();

        if (reverbZone != null)
        {
            reverbZone.transform.position = spawned[placed[0].id].transform.position;
            reverbZone.minDistance = maxDistance * reverbMinDistanceMultiplier;
            reverbZone.maxDistance = maxDistance * reverbMaxDistanceMultiplier;
            reverbZone.reverbPreset = theme.reverbPreset;
        }

        if (isServer)
        {
            SpawnFurniture();
            SpawnLoot();
            SetEntitySpawners();
        }

        StartCoroutine(GenerateNavMeshSurface());
    }

    [Server]
    public void SpawnFurniture()
    {
        if (theme.spawnableFurniture.Length <= 0) return;
        Vector3 initialRoomPos = spawned[placed[0].id].transform.position;
        int inner = 0;
        int mid = 0;
        int outer = 0;

        int q = 0;
        foreach (var pos in furniturePositions)
        {
            if (!EvaluateSpawn(pos.chance, pos.position.position)) continue;

            float distance = Vector3.Distance(initialRoomPos, pos.position.position);
            if (distance < maxDistance / 3) inner++;
            else if (distance < maxDistance / 3 * 2) mid++;
            else outer++;

            q++;

            Quaternion rot = pos.position.rotation * Quaternion.Euler(0f, RandomRange(-pos.maxRotation, pos.maxRotation), 0f);
            Vector3 offset = new(
                RandomRange(-pos.maxOffset.x, pos.maxOffset.x),
                RandomRange(-pos.maxOffset.y, pos.maxOffset.y),
                RandomRange(-pos.maxOffset.z, pos.maxOffset.z));

            FurnitureDataSO data = theme.GetWeigthedFurniture(pos.position.position, RNG);
            GameObject furnObj = Instantiate(data.Prefab, pos.position.position + offset, rot, furnitureParent);
            NetworkServer.Spawn(furnObj);

            if (!furnObj.TryGetComponent(out FurnitureEntity furnEnt)) continue;

            lootPositions.AddRange(furnEnt.lootPositions);

            int roomId = GetRoomIdAtPosition(pos.position.position);
            if (roomId != -1)
            {
                if (!RoomFurniture.ContainsKey(roomId))
                    RoomFurniture[roomId] = new();
                RoomFurniture[roomId].Add(furnEnt);
            }
        }

        Debug.Log($"Furniture spawned -> total: {q}, inner: {inner}, mid: {mid}, outer: {outer}");
    }

    [Server]
    public void SpawnLoot()
    {
        if (theme.spawnableItems.Length <= 0) return;
        Vector3 initialRoomPos = spawned[placed[0].id].transform.position;
        int inner = 0;
        int mid = 0;
        int outer = 0;

        int q = 0;
        foreach (var pos in lootPositions)
        {
            if (!EvaluateSpawn(pos.chance, pos.position.position)) continue;

            float distance = Vector3.Distance(initialRoomPos, pos.position.position);
            if (distance < maxDistance / 3) inner++;
            else if (distance < maxDistance / 3 * 2) mid++;
            else outer++;

            q++;

            Quaternion rot = pos.position.rotation * Quaternion.Euler(
                RandomRange(-180, 180),
                RandomRange(-180, 180),
                RandomRange(-180, 180));
            Vector3 offset = new(
                RandomRange(-pos.maxOffset.x, pos.maxOffset.x),
                RandomRange(-pos.maxOffset.y, pos.maxOffset.y),
                RandomRange(-pos.maxOffset.z, pos.maxOffset.z));

            ItemSO item = theme.GetWeightedItem(pos.position.position, RNG);
            GameObject itemObj = Instantiate(item.itemPrefab, pos.position.position + offset, rot, itemsParent);
            NetworkServer.Spawn(itemObj);

            if (!itemObj.TryGetComponent<ItemBase>(out var itemBase)) continue;
            
            int roomId = GetRoomIdAtPosition(pos.position.position);
            if (roomId != -1)
            {
                if (!RoomItems.ContainsKey(roomId))
                    RoomItems[roomId] = new();
                RoomItems[roomId].Add(itemBase);
            }
        }

        Debug.Log($"Loot spawned -> total: {q}, inner: {inner}, mid: {mid}, outer: {outer}");
    }

    [Server]
    public void SetEntitySpawners()
    {
        if (entitySpawnerPositions == null || entitySpawnerPositions.Count <= 0) return;
        Vector3 initialRoomPos = spawned[placed[0].id].transform.position;
        int inner = 0;
        int mid = 0;
        int outer = 0;

        int q = 0;
        float chance = 1;
        float acChance = chance;
        List<Transform> spawnedPos = new();
        foreach (var pos in entitySpawnerPositions)
        {
            if (!EvaluateSpawn(acChance, pos.position))
            {
                acChance += chance;
                continue;
            }

            float distance = Vector3.Distance(initialRoomPos, pos.position);
            if (distance < maxDistance / 3) inner++;
            else if (distance < maxDistance / 3 * 2) mid++;
            else outer++;

            acChance = chance;
            q++;

            GameObject spawner = Instantiate(entitySpawnerPref, pos.position, pos.rotation, entSpawnParent);
            NetworkServer.Spawn(spawner);

            spawnedPos.Add(spawner.transform);
        }

        if (spawnedPos.Count <= 3)
        {
            int quantity = 3 > entitySpawnerPositions.Count ? entitySpawnerPositions.Count : 3;
            for (int i = 0; i < quantity; i++)
            {
                float distance = Vector3.Distance(initialRoomPos, entitySpawnerPositions[i].position);
                if (distance < maxDistance / 3) inner++;
                else if (distance < maxDistance / 3 * 2) mid++;
                else outer++;
                q++;

                GameObject spawner = Instantiate(entitySpawnerPref, 
                    entitySpawnerPositions[i].position, 
                    entitySpawnerPositions[i].rotation, entSpawnParent);
                NetworkServer.Spawn(spawner);

                spawnedPos.Add(spawner.transform);
            }
        }

        EntitySpawnerManager.Instance.SetSpawnerPositions(spawnedPos);
        Debug.Log($"Entity Spawners -> total: {q}, inner: {inner}, mid: {mid}, outer: {outer}");
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

    IEnumerator GenerationCoroutine(int seed, float duration)
    {
        yield return null;

        seedDisplayTransform.anchoredPosition = new Vector2(0, -225f);
        LeanTween.moveY(seedDisplayTransform, 0f, 0.5f)
         .setEaseOutSine();

        seedDisplayGroup.alpha = 0;
        seedDisplayCanvas.SetActive(true);
        seedDisplayTxt.SetText($"Connecting to new location\n({seed})");

        float fadeTimer = 0f;
        float dotTimer = 0f;

        int dotCount = 0;
        const float dotInterval = 0.22f;

        while (fadeTimer < 0.75f)
        {
            seedDisplayGroup.alpha = fadeTimer * 2;
            fadeTimer += Time.deltaTime;

            dotTimer += Time.deltaTime;
            if (dotTimer >= dotInterval)
            {
                dotTimer = 0f;
                dotCount = (dotCount + 1) % 4;
            }

            string dots = new('.', dotCount);
            seedDisplayTxt.SetText($"Connecting to new location{dots}\n({seed})");
            yield return null;
        }

        seedDisplayGroup.alpha = 1;

        yield return null;

        Generate();
        InstantiateRooms();
        ResolveDoors();
        GenSeedSaver.SaveSeed(seed);

        seedDisplayTxt.SetText($"New location found!\n({seed})");
        OnDungeonGenerated?.Invoke();
        yield return new WaitForSeconds(duration);

        fadeTimer = 0.5f;
        while (fadeTimer > 0)
        {
            seedDisplayGroup.alpha = fadeTimer * 2;
            fadeTimer -= Time.deltaTime;
            yield return null;
        }

        seedDisplayGroup.alpha = 0;
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

    public RoomData GetRoomDataAtPosition(Vector3 worldPos)
    {
        int roomId = GetRoomIdAtPosition(worldPos);
        if (roomId == -1) return null;

        spawned.TryGetValue(roomId, out var rd);
        return rd;
    }

    public void ClearMap()
    {
        OnDungeonClear?.Invoke();

        DestroyChildren(roomsParent, false);
        DestroyChildren(furnitureParent, true);
        DestroyChildren(itemsParent, true);
        DestroyChildren(entSpawnParent, true);

        placed.Clear();
        spawned.Clear();
        lootPositions.Clear();
        furniturePositions.Clear();
        entitySpawnerPositions.Clear();
        RoomFurniture.Clear();
        RoomItems.Clear();

        grid = null;
        nextRoomId = 1;
        _generated = false;
        RNG = null;

        if (surface != null)
            surface.RemoveData();
    }

    private void DestroyChildren(Transform parent, bool hasNetID)
    {
        if (parent == null) return;
        if (hasNetID && !isServer) return;

        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            var child = parent.GetChild(i).gameObject;
            if (hasNetID)
                NetworkServer.Destroy(child);
            else
                Destroy(child);
        }
    }

    public float GetDificultyMultiplier(Vector3 targetPos)
    {
        Vector3 initialRoomPos = spawned[placed[0].id].transform.position;

        float gridDistance = Vector3.Distance(initialRoomPos, targetPos) / 5f;
        float multiplier = difficultyCurve.Evaluate(gridDistance);
        return multiplier;
    }

    private bool EvaluateSpawn(float chance, Vector3 position)
    {
        float rand = RandomRange(0, 100f);
        float balChance = chance * GetDificultyMultiplier(position);
        return rand <= balChance;
    }

    private void OnDrawGizmosSelected()
    {
        int size = LobbySettings.Instance != null ? LobbySettings.Instance.MapSize : simMapSize;
        Vector3Int effective = new(gridSize.x * size, gridSize.y * size, gridSize.z * size);

        Vector3 totalSize = new(effective.x * cellSize, effective.y * cellSize, effective.z * cellSize);
        Vector3 center = totalSize / 2f;

        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(center, totalSize);
    }
}
