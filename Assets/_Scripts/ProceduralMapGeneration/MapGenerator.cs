using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class MapGenerator : MonoBehaviour
{
    [Header("Content")]
    [SerializeField] private ThemeDataSO theme;

    [Header("Seed")]
    [SerializeField] private string gameSeed = "Default";
    [SerializeField] private int seed;

    [Header("Grid")]
    [SerializeField] private Vector3Int gridSize = new (24, 6, 24);
    [SerializeField] private int cellSize = 6;

    [Header("Limits")]
    [SerializeField] private int maxRooms = 30;
    [SerializeField] private int maxDepth = 30;

    [Header("Debug")]
    [SerializeField] PlayerInput pInput;
    [SerializeField] bool stepGenerationMode = false;

    private System.Random rng;
    private Cell[,,] grid;
    private readonly List<PlacedRoom> placed = new();
    private readonly Dictionary<int, RoomData> spawned = new();
    private int nextRoomId = 1;

    private struct OpenPort
    {
        public int roomId;
        public Vector3Int worldCell;
        public Direction face;
        public Vector3Int localCell;
        public int depth;
    }

    private class PlacedRoom
    {
        public int id;
        public RoomDataSO data;
        public Vector3Int anchor;
        public int depth;
    }

    private class Cell
    {
        public PlacedRoom placedRoom;
        public Vector3Int local;
    }

    private void Awake()
    {
        seed = StableHash(gameSeed);
        rng = new System.Random(seed);
        grid = new Cell[gridSize.x, gridSize.y, gridSize.z];
        for (int x = 0; x < gridSize.x; x++)
            for (int y = 0; y < gridSize.y; y++)
                for (int z = 0; z < gridSize.z; z++)
                    grid[x, y, z] = new Cell();
    }

    private void Start()
    {
        if (stepGenerationMode)
        {
            pInput.enabled = true;
            StartStepGeneration();
        }
        else
        {
            pInput.enabled = false;

            Generate();
            InstantiateRooms();
            ResolveDoors();
        }
    }

    #region Step Generation
    private List<OpenPort> stepFrontier;
    private bool generationStarted = false;

    public void StartStepGeneration()
    {
        placed.Clear();
        spawned.Clear();
        nextRoomId = 1;

        grid = new Cell[gridSize.x, gridSize.y, gridSize.z];
        for (int x = 0; x < gridSize.x; x++)
            for (int y = 0; y < gridSize.y; y++)
                for (int z = 0; z < gridSize.z; z++)
                    grid[x, y, z] = new Cell();

        var center = new Vector3Int(
            gridSize.x / 2,
            Mathf.Clamp(gridSize.y / 2, 0, gridSize.y - 1),
            gridSize.z / 2
        );

        var start = Place(theme.startingRoom, center, 0);
        stepFrontier = BuildOpenPorts(start);

        generationStarted = true;
    }

    public bool StepGeneration()
    {
        if (!generationStarted || stepFrontier.Count == 0 || placed.Count >= maxRooms)
            return false;

        int idx = rng.Next(stepFrontier.Count);
        var open = stepFrontier[idx];
        stepFrontier.RemoveAt(idx);

        if (open.depth >= maxDepth)
            return true;

        var candidates = theme.spawnableRooms;
        Shuffle(candidates);

        foreach (var cand in candidates)
        {
            if (cand == null) continue;

            for (int p = 0; p < cand.Ports.Length; p++)
            {
                var port = cand.Ports[p];
                if (port.face != DirectionUtils.OppositeDirection(open.face)) continue;

                var anchor = open.worldCell - port.localCell;
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

                stepFrontier.AddRange(newPorts);
                return true;
            }
        }

        return true;
    }

    public void DoStepGeneration(InputAction.CallbackContext context)
    {
        if (!context.started) return;

        if (StepGeneration())
            InstantiateRooms();
        else
            ResolveDoors();
    }
    #endregion

    private void Generate()
    {
        if (theme == null || theme.startingRoom == null) { Debug.LogError("Theme or startingRoom is missing."); return; }
        var center = new Vector3Int(gridSize.x / 2, Mathf.Clamp(gridSize.y / 2, 0, gridSize.y - 1), gridSize.z / 2); 
        var start = Place(theme.startingRoom, center, depth: 0); 

        if (start == null) 
        { 
            Debug.LogError("Failed to place starting room at center."); 
            return; 
        }

        var frontier = BuildOpenPorts(start); 
        while (frontier.Count > 0 && placed.Count < maxRooms) 
        { 
            int idx = rng.Next(frontier.Count); 
            var open = frontier[idx]; frontier.RemoveAt(idx); 

            if (open.depth >= maxDepth) continue; 

            var candidates = theme.spawnableRooms; 
            Shuffle(candidates); 
            
            bool placedAny = false; 

            for (int c = 0; c < candidates.Length; c++) 
            { 
                var cand = candidates[c]; 
                if (cand == null) continue; 

                for (int p = 0; p < cand.Ports.Length; p++) 
                { 
                    var port = cand.Ports[p];
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
            depth = depth
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
                depth = pr.depth
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
    #endregion

    #region Instantiate + door pass
    private void InstantiateRooms()
    {
        foreach (var pr in placed)
        {
            var world = Vector3.Scale((Vector3)pr.anchor, new Vector3(cellSize, cellSize, cellSize));
            var go = Instantiate(pr.data.Prefab, world, Quaternion.identity, transform);
            go.name = $"Room {pr.id} (depth {pr.depth})";
            var rd = go.GetComponent<RoomData>();
            if (rd == null) Debug.LogWarning($"Prefab {pr.data.Prefab.name} lacks RoomData component.");
            spawned[pr.id] = rd;
        }
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
                            if (np.localCell == neighborLocal && np.face == DirectionUtils.OppositeDirection(port.face))
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
    #endregion
}
