using Mirror;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.Events;

public class DungeonGenerator : NetworkBehaviour
{
    public static DungeonGenerator Instance;

    [Header("Content")]
    [SerializeField] DungeonSettingsSO settings;
    [SerializeField] ThemeDataSO theme;
    [SerializeField] Transform roomsParent;
    [SerializeField] Transform voidDestroyerParent;
    [SerializeField] NavMeshSurface surface;
    [SerializeField] AudioReverbZone reverbZone;

    [Header("Debug")]
    [SerializeField] int simMapSize = 10;
    [SerializeField] GameObject seedDisplayCanvas;
    [SerializeField] RectTransform seedDisplayTransform;
    [SerializeField] CanvasGroup seedDisplayGroup;
    [SerializeField] TextMeshProUGUI seedDisplayTxt;

    public System.Random RNG { get; private set; }
    public readonly SyncList<uint> EntityNetIds = new();

    Cell[,,] grid;
    int nextRoomId = 1;
    float maxDistance = 0;
    bool _generated = false;
    Vector3Int effectiveGridSize;
    readonly List<PlacedRoom> placed = new();
    readonly List<IDungeonSpawner> spawners = new();
    readonly Dictionary<int, RoomData> spawned = new();

    public UnityEvent OnDungeonGenerated;
    public UnityEvent OnDungeonClear;


    public Vector3Int GridSize => effectiveGridSize;
    public Dictionary<int, HashSet<int>> RoomAdjacency = new();
    public Vector3Int StartRoomPos;

    public Cell[,,] Grid => grid;
    public ThemeDataSO Theme => theme;
    public float MaxDistance => maxDistance;
    public int CellSize => settings.cellSize;
    public DungeonSettingsSO Settings => settings;
    public IReadOnlyList<PlacedRoom> PlacedRooms => placed;
    public IReadOnlyDictionary<int, RoomData> SpawnedRooms => spawned;

    struct OpenPort
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

    float RandomRange(float min, float max) => (float)(RNG.NextDouble() * (max - min)) + min;
    int RandomRange(int min, int max) => (int)(RNG.NextDouble() * (max - min)) + min;

    void Awake()
    {
        Instance = this;

        GetComponentsInChildren(true, spawners);
        spawners.Clear();
        foreach (var s in GetComponentsInChildren<MonoBehaviour>(true))
            if (s is IDungeonSpawner ds) spawners.Add(ds);

        Debug.Log($"[Generator] spawners count: {spawners.Count}");
    }

    public void StartGeneration(int setSeed, int themeIndex, int? mapSizeOverride = null)
    {
        if (_generated) return;
        _generated = true;

        theme = GameManager.Instance.dngMod.ThemeDatas[themeIndex];

        int size = Mathf.Max(1, mapSizeOverride ?? LobbySettings.Instance.MapSize);
        effectiveGridSize = CalculateEffectiveGrid(size);

        voidDestroyerParent.localScale = new Vector3(size + 2, 1, size + 2);

        grid = new Cell[effectiveGridSize.x, effectiveGridSize.y, effectiveGridSize.z];
        for (int x = 0; x < effectiveGridSize.x; x++)
            for (int y = 0; y < effectiveGridSize.y; y++)
                for (int z = 0; z < effectiveGridSize.z; z++)
                    grid[x, y, z] = new Cell();

        RNG = new System.Random(setSeed);

        StartCoroutine(GenerationCoroutine(setSeed, 5f));
    }

    private Vector3Int CalculateEffectiveGrid(int size)
    {
        Vector3Int res = settings.baseGridSize;
        int horizontalSteps = size - 1;
        res.x *= 1 + ((horizontalSteps + 1) / 2);
        res.y *= 1 + (size / 2);
        res.z *= 1 + (horizontalSteps / 2);
        return res;
    }

    void Generate()
    {
        if (theme == null || theme.startingRoom == null) { Debug.LogError("Theme or startingRoom is missing."); return; }

        var center = new Vector3Int(
            effectiveGridSize.x / 2,
            Mathf.Clamp(effectiveGridSize.y / 2, 0, effectiveGridSize.y - 1),
            effectiveGridSize.z / 2);

        var start = Place(theme.startingRoom, center, depth: 0);
        if (start == null)
        {
            center = new Vector3Int(1, 0, 0);
            start = Place(theme.startingRoom, center, depth: 0);
        }
        if (start == null) { Debug.LogError("Failed to place starting room."); return; }

        StartRoomPos = center * CellSize;
        if (GameManager.Instance.isServer)
            GameManager.Instance.dngMod.startRoomPos = StartRoomPos;

        var frontier = BuildOpenPorts(start);
        while (frontier.Count > 0 && placed.Count < settings.maxRoomsBase * LobbySettings.Instance.MapSize)
        {
            int idx = RNG.Next(frontier.Count);
            var open = frontier[idx]; frontier.RemoveAt(idx);

            if (open.depth >= settings.maxDepthBase * LobbySettings.Instance.MapSize) continue;

            var neighborRoom = placed.Find(r => r.id == open.roomId);
            var candidates = GetWeightedCandidates(theme.spawnableRooms, neighborRoom.biome);

            bool placedAny = false;
            int biomeSearchTries = candidates.Length / 10;
            for (int c = 0; c < candidates.Length; c++)
            {
                var cand = candidates[c];
                if (cand == null) continue;
                if (cand.biome != neighborRoom.biome && biomeSearchTries > 0) { biomeSearchTries--; continue; }

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
                        { newPorts.RemoveAt(i); break; }
                    }

                    frontier.AddRange(newPorts);
                    placedAny = true;
                    break;
                }
                if (placedAny) break;
            }
        }
    }

    PlacedRoom Place(RoomDataSO data, Vector3Int anchor, int depth)
    {
        if (!FootprintFits(data, anchor)) return null;

        var pr = new PlacedRoom { id = nextRoomId++, data = data, anchor = anchor, depth = depth, biome = data.biome };
        placed.Add(pr);

        foreach (var local in data.RoomFootprint)
        {
            var w = anchor + local.Footprint;
            grid[w.x, w.y, w.z].placedRoom = pr;
            grid[w.x, w.y, w.z].local = local.Footprint;
        }

        return pr;
    }

    List<OpenPort> BuildOpenPorts(PlacedRoom pr)
    {
        var list = new List<OpenPort>(pr.data.Ports.Length);
        foreach (var port in pr.data.Ports)
        {
            if (!pr.data.ContainsLocalCell(port.localCell)) continue;
            list.Add(new OpenPort
            {
                roomId = pr.id,
                worldCell = pr.anchor + port.localCell,
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

    bool FootprintFits(RoomDataSO data, Vector3Int anchor)
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

    static int StableHash(string s)
    {
        unchecked
        {
            const int prime = 16777619;
            int hash = (int)2166136261;
            foreach (char c in s) hash = (hash ^ c) * prime;
            return hash;
        }
    }

    void Shuffle<T>(IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = RNG.Next(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    RoomDataSO[] GetWeightedCandidates(RoomDataSO[] candidates, Biome neighborBiome)
    {
        LL_Tier.Tier rolledTier = RollTier(LL_Tier.BaseTierWeights);
        List<RoomDataSO> weighted = new();

        foreach (var c in candidates)
        {
            if (c == null || c.RoomTier != rolledTier) continue;
            weighted.Add(c);
            if (c.biome == neighborBiome) { weighted.Add(c); weighted.Add(c); }
        }

        if (weighted.Count == 0)
            foreach (var c in candidates)
            {
                if (c == null) continue;
                weighted.Add(c);
                if (c.biome == neighborBiome) { weighted.Add(c); weighted.Add(c); }
            }

        Shuffle(weighted);
        return weighted.ToArray();
    }

    LL_Tier.Tier RollTier(Dictionary<LL_Tier.Tier, float> weights)
    {
        float total = 0f;
        foreach (var kvp in weights) total += kvp.Value;
        float roll = (float)(RNG.NextDouble() * total);
        foreach (var kvp in weights) { roll -= kvp.Value; if (roll <= 0f) return kvp.Key; }
        return LL_Tier.Tier.Common;
    }

    void BuildRoomAdjacency()
    {
        foreach (var pr in placed)
        {
            if (!RoomAdjacency.ContainsKey(pr.id)) RoomAdjacency[pr.id] = new();

            foreach (var port in pr.data.Ports)
            {
                Vector3Int worldCell = pr.anchor + port.localCell;
                Vector3Int neighborPos = worldCell + DirectionUtils.DirectionVector(port.face);
                if (!InBounds(neighborPos)) continue;

                var neighborCell = grid[neighborPos.x, neighborPos.y, neighborPos.z];
                if (neighborCell?.placedRoom == null) continue;

                var neighborRoom = neighborCell.placedRoom;
                bool hasMatchingPort = false;
                foreach (var np in neighborRoom.data.Ports)
                {
                    if (np.type == port.type &&
                        np.localCell == neighborPos - neighborRoom.anchor &&
                        np.face == DirectionUtils.OppositeDirection(port.face))
                    { hasMatchingPort = true; break; }
                }

                if (hasMatchingPort) RoomAdjacency[pr.id].Add(neighborRoom.id);
            }
        }
    }

    public int GetRoomIdAtPosition(Vector3 worldPos)
    {
        Vector3Int cellPos = new(
            Mathf.FloorToInt(worldPos.x / CellSize),
            Mathf.FloorToInt(worldPos.y / CellSize),
            Mathf.FloorToInt(worldPos.z / CellSize));

        if (!InBounds(cellPos)) return -1;
        var cell = grid[cellPos.x, cellPos.y, cellPos.z];
        if (cell?.placedRoom != null) return cell.placedRoom.id;

        for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
                for (int dz = -1; dz <= 1; dz++)
                {
                    if (dx == 0 && dy == 0 && dz == 0) continue;
                    var nb = cellPos + new Vector3Int(dx, dy, dz);
                    if (!InBounds(nb)) continue;
                    var nbCell = grid[nb.x, nb.y, nb.z];
                    if (nbCell?.placedRoom != null) return nbCell.placedRoom.id;
                }

        return -1;
    }

    #endregion

    #region Instantiate

    void InstantiateRooms()
    {
        var pointsByChannel = new Dictionary<SpawnChannel, List<DungeonSpawnPoint>>();

        foreach (var pr in placed)
        {
            var world = Vector3.Scale((Vector3)pr.anchor, new Vector3(CellSize, CellSize, CellSize));
            var go = Instantiate(pr.data.Prefab, world, Quaternion.identity, roomsParent);
            go.name = $"Room {pr.id} (depth {pr.depth})";

            if (!go.TryGetComponent<RoomData>(out var rd))
                Debug.LogWarning($"Prefab {pr.data.Prefab.name} lacks RoomData component.");

            rd.PlacedRoom = pr;
            spawned[pr.id] = rd;

            float distance = Vector3.Distance(spawned[placed[0].id].transform.position, go.transform.position);
            if (distance > maxDistance) maxDistance = distance;

            foreach (var sp in rd.SpawnPoints)
            {
                if (sp == null || sp.Channel == null) continue;
                if (!pointsByChannel.TryGetValue(sp.Channel, out var list))
                    pointsByChannel[sp.Channel] = list = new();
                list.Add(sp);
            }
        }

        BuildRoomAdjacency();

        if (reverbZone != null)
        {
            reverbZone.transform.position = spawned[placed[0].id].transform.position;
            reverbZone.minDistance = maxDistance * settings.reverbMinDistanceMultiplier;
            reverbZone.maxDistance = maxDistance * settings.reverbMaxDistanceMultiplier;
            reverbZone.reverbPreset = theme.reverbPreset;
        }

        if (isServer)
            RunSpawners(pointsByChannel);

        StartCoroutine(GenerateNavMeshSurface());
    }

    [Server]
    void RunSpawners(Dictionary<SpawnChannel, List<DungeonSpawnPoint>> pointsByChannel)
    {
        foreach (var spawner in spawners)
        {
            if (spawner.Channel == null) continue;
            pointsByChannel.TryGetValue(spawner.Channel, out var pts);
            spawner.Collect(pts ?? new List<DungeonSpawnPoint>());
        }

        FurnitureSpawner furniture = null;
        LootSpawner loot = null;
        EntitySpawner entity = null;

        foreach (var spawner in spawners)
        {
            if (spawner is FurnitureSpawner f) { furniture = f; continue; }
            if (spawner is LootSpawner l) { loot = l; continue; }
            if (spawner is EntitySpawner e) { entity = e; continue; }
            spawner.Spawn(this);
        }

        furniture?.Spawn(this);

        if (loot != null)
        {
            if (furniture != null) loot.AddExtraPoints(furniture.DiscoveredLootPoints);
            loot.Spawn(this);
        }

        entity?.Spawn(this);
    }

    void ResolveDoors()
    {
        foreach (var pr in placed)
        {
            if (!spawned.TryGetValue(pr.id, out var rd) || rd == null) continue;

            for (int i = 0; i < pr.data.Ports.Length; i++)
            {
                var port = pr.data.Ports[i];
                var worldCell = pr.anchor + port.localCell;
                var nb = worldCell + DirectionUtils.DirectionVector(port.face);

                bool open = false;
                if (InBounds(nb))
                {
                    var nOcc = grid[nb.x, nb.y, nb.z].placedRoom;
                    if (nOcc != null)
                    {
                        foreach (var np in nOcc.data.Ports)
                        {
                            if (np.type == port.type &&
                                np.localCell == nb - nOcc.anchor &&
                                np.face == DirectionUtils.OppositeDirection(port.face))
                            { open = true; break; }
                        }
                    }
                }

                rd.SetPort(i, open);
            }
        }
    }

    IEnumerator GenerateNavMeshSurface()
    {
        yield return null;
        surface.BuildNavMesh();
    }

    #endregion

    public bool GeneratedDungeon =>
        spawned != null && placed != null && spawned.Count > 0 && placed.Count > 0;

    IEnumerator GenerationCoroutine(int seed, float duration)
    {
        yield return null;

        seedDisplayTransform.anchoredPosition = new Vector2(0, -225f);
        LeanTween.moveY(seedDisplayTransform, 0f, 0.5f).setEaseOutSine();

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
            if (dotTimer >= dotInterval) { dotTimer = 0f; dotCount = (dotCount + 1) % 4; }
            seedDisplayTxt.SetText($"Connecting to new location{new string('.', dotCount)}\n({seed})");
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
        while (fadeTimer > 0) { seedDisplayGroup.alpha = fadeTimer * 2; fadeTimer -= Time.deltaTime; yield return null; }
        seedDisplayGroup.alpha = 0;
        seedDisplayCanvas.SetActive(false);
    }

    public Vector3 GetRandomPosition(float maxOffset = 4f)
    {
        int index = UnityEngine.Random.Range(0, spawned.Count);
        var world = Vector3.Scale((Vector3)placed[index].anchor, new Vector3(CellSize, CellSize, CellSize));
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

        foreach (var spawner in spawners)
            spawner.Clear();

        DestroyChildren(roomsParent, false, true);

        placed.Clear();
        spawned.Clear();
        RoomAdjacency.Clear();

        if (isServer) EntityNetIds.Clear();

        grid = null;
        nextRoomId = 1;
        _generated = false;
        maxDistance = 0;
        RNG = null;

        if (surface != null) surface.RemoveData();
    }

    void DestroyChildren(Transform parent, bool hasNetID, bool ignoreFirst = false)
    {
        if (parent == null) return;
        if (hasNetID && !isServer) return;

        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            if (i == 0 && ignoreFirst) continue;
            var child = parent.GetChild(i).gameObject;
            if (hasNetID) NetworkServer.Destroy(child);
            else Destroy(child);
        }
    }

    public float GetDificultyMultiplier(Vector3 targetPos)
    {
        Vector3 initialRoomPos = spawned[placed[0].id].transform.position;
        float gridDistance = Vector3.Distance(initialRoomPos, targetPos) / 5f;
        return settings.difficultyCurve.Evaluate(gridDistance);
    }

    bool EvaluateSpawn(float chance, Vector3 position)
    {
        float rand = RandomRange(0f, 100f);
        return rand <= chance * GetDificultyMultiplier(position);
    }

    void OnDrawGizmosSelected()
    {
        int size = LobbySettings.Instance != null
            ? LobbySettings.Instance.MapSize
            : simMapSize;

        size = Mathf.Max(1, size);

        Vector3Int effective = CalculateEffectiveGrid(size);

        int horizontalSteps = size - 1;
        int xSteps = (horizontalSteps + 1) / 2;
        int ySteps = size / 2;
        int zSteps = horizontalSteps / 2;

        effective.x *= 1 + xSteps;
        effective.y *= 1 + ySteps;
        effective.z *= 1 + zSteps;

        Vector3 totalSize = new(
            effective.x * CellSize,
            effective.y * CellSize,
            effective.z * CellSize);

        Gizmos.color = Color.red;

        Vector3 center = transform.position + totalSize * 0.5f;

        Gizmos.DrawWireCube(center, totalSize);
    }
}
