using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class RoomData : MonoBehaviour
{
    [System.Serializable]
    public struct WallPortKey
    {
        public Vector3Int localCell;
        public Direction face;
        public GameObject wall;
        public GameObject door;
    }

    [SerializeField] private WallPortKey[] ports = System.Array.Empty<WallPortKey>();
    public RoomDataSO Data;
    public DungeonGenerator.PlacedRoom PlacedRoom;

    public List<ItemSpawnPosition> ItemSpawnPositions;
    public List<FurnitureSpawnPosition> FurnitureSpawnPositions;

    public List<Transform> entitySpawnerPositions;
    public Renderer[] roomRenderers;
    public LED_Light[] roomLights;

    [SerializeField] bool beingRendered = true;

    [Header("Gizmo Settings")]
    [SerializeField] bool showFootprint = false;
    [SerializeField] bool showPorts = false;
    [SerializeField] bool showFootprintCorners = false;
    [SerializeField] bool showFootprintCoords = false;
    [SerializeField] bool showFootprintSprites = false;

    [SerializeField] Color fontColor = Color.darkRed;
    [SerializeField] int fontSize = 25;
    [SerializeField] int spriteSize = 150;

    [SerializeField] bool useLayers = false;
    [SerializeField] int currentLayer = 0;

    public readonly List<WallPortKey> closedPorts = new();

    private void Start()
    {
        roomRenderers = roomRenderers.Where(r => r != null && r.enabled).ToArray();
    }

    public void SetPort(Vector3Int localCell, Direction face, bool open)
    {
        if (!open)
        {
            WallPortKey key = new() { localCell = localCell, face = face };

            closedPorts.Add(key);
        }

        for (int i = 0; i < ports.Length; i++)
        {
            if (ports[i].localCell == localCell && ports[i].face == face)
            {
                if (ports[i].wall) ports[i].wall.SetActive(!open);
                if (ports[i].door) ports[i].door.SetActive(open);
                return;
            }
        }
    }

    public void SetRender(bool shouldRender)
    {
        if (beingRendered == shouldRender) return;

        beingRendered = shouldRender;

        foreach (Renderer renderer in roomRenderers)
        {
            renderer.enabled = shouldRender;
        }

        foreach (LED_Light light in roomLights)
        {
            light.RenderLight(shouldRender);
        }
    }

    public void SetLightRender(bool shouldRender)
    {
        if (beingRendered == shouldRender) return;

        foreach (LED_Light light in roomLights)
        {
            light.RenderLight(shouldRender);
        }
    }

    public Vector3 GetRandomPositionInRoom(float yOffset = 0f)
    {
        var footprint = PlacedRoom.data.RoomFootprint;
        var entry = footprint[Random.Range(0, footprint.Length)];
        float cellSize = DungeonGenerator.Instance.CellSize;

        Vector3 cellOrigin = transform.position + new Vector3(
            entry.Footprint.x * cellSize,
            entry.Footprint.y * cellSize + yOffset,
            entry.Footprint.z * cellSize);

        cellSize *= 0.8f;
        float x = Random.Range(-cellSize, cellSize);
        float z = Random.Range(-cellSize, cellSize);
        Vector3 candidate = cellOrigin + new Vector3(x, 0f, z);

        if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, cellSize, NavMesh.AllAreas))
            return hit.position;

        if (NavMesh.SamplePosition(cellOrigin, out NavMeshHit centerHit, cellSize, NavMesh.AllAreas))
            return centerHit.position;

        return candidate;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (Data == null || Data.RoomFootprint == null) return;

        Vector3 origin = transform.position;
        const float cellSize = 5f;

        if (showPorts)
        {
            foreach (var port in Data.Ports)
            {
                if (useLayers && port.localCell.y != currentLayer) continue;

                Vector3 dir = (Vector3)DirectionUtils.DirectionVector(port.face);
                Vector3 cellCenter = origin + new Vector3(
                    port.localCell.x * cellSize,
                    port.localCell.y * cellSize + cellSize * 0.45f,
                    port.localCell.z * cellSize);

                Vector3 faceCentre = cellCenter + dir * (cellSize * 0.5f);

                Gizmos.color = DirectionColor(port.face);

                Vector3 right = Vector3.Cross(dir, Vector3.up);
                if (right == Vector3.zero) right = Vector3.right;
                Vector3 up = Vector3.Cross(right, dir);

                float half = cellSize * 0.45f;
                Vector3 tl = faceCentre + (-right + up) * half;
                Vector3 tr = faceCentre + (right + up) * half;
                Vector3 br = faceCentre + (right - up) * half;
                Vector3 bl = faceCentre + (-right - up) * half;

                Gizmos.DrawLine(tl, tr);
                Gizmos.DrawLine(tr, br);
                Gizmos.DrawLine(br, bl);
                Gizmos.DrawLine(bl, tl);

                UnityEditor.Handles.color = Gizmos.color;
                UnityEditor.Handles.ArrowHandleCap(
                    0,
                    faceCentre,
                    Quaternion.LookRotation(dir),
                    cellSize * 0.35f,
                    EventType.Repaint);

                UnityEditor.Handles.Label(
                    faceCentre + dir * (cellSize * 0.4f) + Vector3.up * 0.3f,
                    $"{port.face} {port.localCell}",
                    new GUIStyle
                    {
                        normal = { textColor = DirectionColor(port.face) },
                        fontSize = 11,
                        alignment = TextAnchor.MiddleCenter
                    });
            }
        }

        foreach (var entry in Data.RoomFootprint)
        {
            if (useLayers && entry.Footprint.y != currentLayer)
                continue;

            Vector3 worldCenter = origin + new Vector3(
                entry.Footprint.x * cellSize,
                entry.Footprint.y * cellSize + (cellSize / 2),
                entry.Footprint.z * cellSize);

            if (showFootprint)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(worldCenter, new Vector3(cellSize, cellSize, cellSize));
            }

            if (showFootprintSprites && entry.MapSprite != null)
            {
                Texture2D tex = UnityEditor.AssetPreview.GetAssetPreview(entry.MapSprite);
                if (tex == null) tex = entry.MapSprite.texture;
                
                if (tex != null)
                {
                    float worldSize = spriteSize / 100f;

                    Vector3 right = UnityEditor.SceneView.lastActiveSceneView?.camera.transform.right ?? Vector3.right;
                    Vector3 up = UnityEditor.SceneView.lastActiveSceneView?.camera.transform.up ?? Vector3.up;

                    Vector3 tl = worldCenter + (-right + up) * worldSize * 0.5f;
                    Vector3 tr = worldCenter + (right + up) * worldSize * 0.5f;
                    Vector3 br = worldCenter + (right - up) * worldSize * 0.5f;
                    Vector3 bl = worldCenter + (-right - up) * worldSize * 0.5f;

                    UnityEditor.Handles.BeginGUI();
                    Vector2 guiCenter = UnityEditor.HandleUtility.WorldToGUIPoint(worldCenter);
                    float guiSize = spriteSize;
                    Rect guiRect = new Rect(guiCenter.x - guiSize * 0.5f, guiCenter.y - guiSize * 0.5f, guiSize, guiSize);
                    GUI.DrawTexture(guiRect, tex, ScaleMode.ScaleToFit, true);
                    UnityEditor.Handles.EndGUI();
                }
            }

            if (showFootprintCoords)
            {
                UnityEditor.Handles.Label(
                    worldCenter + Vector3.up * 0.25f,
                    $"{entry.Footprint.x},{entry.Footprint.y},{entry.Footprint.z}",
                    new GUIStyle { normal = { textColor = fontColor }, fontSize = fontSize, alignment = TextAnchor.MiddleCenter }
                );
            }
        }

        if (showFootprintCorners)
        {
            if (Data.FootprintCorners != null)
            {
                foreach (var entry in Data.FootprintCorners)
                {
                    if (useLayers && entry.Footprint.y != currentLayer)
                        continue;

                    Vector3 worldCenter = origin + new Vector3(
                        entry.Footprint.x * cellSize,
                        entry.Footprint.y * cellSize + (cellSize / 2),
                        entry.Footprint.z * cellSize);

                    Gizmos.color = Color.cyan;
                    Gizmos.DrawWireCube(worldCenter, new Vector3(cellSize * 0.4f, cellSize * 0.4f, cellSize * 0.4f));
                }
            }
        }
    }

    private static Color DirectionColor(Direction d) => d switch
    {
        Direction.North => Color.blue,
        Direction.South => Color.red,
        Direction.East => Color.green,
        Direction.West => Color.yellow,
        Direction.Up => Color.cyan,
        Direction.Down => Color.magenta,
        _ => Color.white
    };
#endif
}
