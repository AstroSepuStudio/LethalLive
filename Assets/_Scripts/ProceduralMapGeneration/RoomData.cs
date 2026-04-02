using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class RoomData : MonoBehaviour
{
    [Serializable]
    public struct WallPortKey
    {
        public int portIndex;
        public GameObject wall;
        public GameObject door;
    }

    [SerializeField] private WallPortKey[] ports = Array.Empty<WallPortKey>();
    public RoomDataSO Data;
    public DungeonGenerator.PlacedRoom PlacedRoom;

    public List<ItemSpawnPosition> ItemSpawnPositions;
    public List<FurnitureSpawnPosition> FurnitureSpawnPositions;
    public List<DecorationSpawnPoint> DecorationPositions;

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

    public readonly List<int> closedPorts = new();

    private void Start()
    {
        roomRenderers = roomRenderers.Where(r => r != null && r.enabled).ToArray();
    }

    public void SetPort(int portIndex, bool open)
    {
        if (!open) closedPorts.Add(portIndex);

        if (portIndex < 0 || portIndex >= ports.Length) return;
        if (ports[portIndex].wall) ports[portIndex].wall.SetActive(!open);
        if (ports[portIndex].door) ports[portIndex].door.SetActive(open);
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
        var entry = footprint[UnityEngine.Random.Range(0, footprint.Length)];
        float cellSize = DungeonGenerator.Instance.CellSize;

        Vector3 cellOrigin = transform.position + new Vector3(
            entry.Footprint.x * cellSize,
            entry.Footprint.y * cellSize + yOffset,
            entry.Footprint.z * cellSize);

        cellSize *= 0.8f;
        float x = UnityEngine.Random.Range(-cellSize, cellSize);
        float z = UnityEngine.Random.Range(-cellSize, cellSize);
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
            for (int pi = 0; pi < Data.Ports.Length; pi++)
            {
                var port = Data.Ports[pi];

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
                    $"[{pi}] {port.face} {port.localCell}",
                    new GUIStyle
                    {
                        normal = { textColor = DirectionColor(port.face) },
                        fontSize = 25,
                        alignment = TextAnchor.MiddleCenter
                    });

                bool wired = false;
                foreach (var wpk in ports)
                {
                    if (wpk.portIndex != pi) continue;
                    wired = true;

                    string wallStr = wpk.wall != null ? $"W: {wpk.wall.name}" : "W: -";
                    string doorStr = wpk.door != null ? $"D: {wpk.door.name}" : "D: -";
                    bool incomplete = wpk.wall == null || wpk.door == null;

                    UnityEditor.Handles.Label(
                        faceCentre + dir * (cellSize * 0.4f) + Vector3.up * -0.5f,
                        $"{wallStr} | {doorStr}",
                        new GUIStyle
                        {
                            normal = { textColor = incomplete ? Color.yellow : Color.green },
                            fontSize = 20,
                            alignment = TextAnchor.MiddleCenter
                        });
                    break;
                }

                if (!wired)
                {
                    UnityEditor.Handles.Label(
                        faceCentre + dir * (cellSize * 0.4f) + Vector3.up * -0.5f,
                        "NOT WIRED",
                        new GUIStyle
                        {
                            normal = { textColor = Color.red },
                            fontSize = 20,
                            alignment = TextAnchor.MiddleCenter
                        });
                }

                if (port.deadEndOverride != null)
                {
                    Texture2D tex = UnityEditor.AssetPreview.GetAssetPreview(port.deadEndOverride);
                    if (tex == null) tex = port.deadEndOverride.texture;

                    if (tex != null)
                    {
                        UnityEditor.Handles.BeginGUI();
                        Vector2 guiCenter = UnityEditor.HandleUtility.WorldToGUIPoint(faceCentre);
                        float guiSize = spriteSize;
                        Rect guiRect = new Rect(guiCenter.x - guiSize * 0.5f, guiCenter.y - guiSize * 0.5f, guiSize, guiSize);
                        GUI.DrawTexture(guiRect, tex, ScaleMode.ScaleToFit, true);
                        UnityEditor.Handles.EndGUI();
                    }
                }

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
