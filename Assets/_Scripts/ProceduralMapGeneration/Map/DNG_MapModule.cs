using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class DNG_MapModule : MonoBehaviour
{
    [System.Serializable] 
    struct DirectionalSprite 
    { public Sprite sprite; public Direction direction; }

    [Header("References")]
    [SerializeField] GameObject mapCellPrefab;
    [SerializeField] GameObject deadEndPrefab;
    [SerializeField] GameObject mapFeaturePrefab;
    [SerializeField] Image followPlayerImg;

    [SerializeField] RectTransform playerDot;
    [SerializeField] RectTransform mapAnchor;
    [SerializeField] Transform cellParent;
    [SerializeField] Transform itemParent;
    [SerializeField] Transform furnParent;
    [SerializeField] Transform extraParent;

    [Header("Visual")]
    [SerializeField] DirectionalSprite[] deadEndSprites;
    [SerializeField] Sprite itemSprite;
    [SerializeField] Color itemColor = Color.softYellow;
    [SerializeField] Sprite furnSprite;
    [SerializeField] Color furnColor = Color.rosyBrown;
    [SerializeField] Sprite beaconSprite;
    [SerializeField] Color beaconColor = Color.cyan;

    [Header("Config")]
    [SerializeField] float cellSize = 200;
    [SerializeField] Color mapColor = new Color(0, 150, 0);

    public bool IsFollowingPlayer => followPlayer;
    bool followPlayer = true;
    bool displayFeatures = true;

    readonly Dictionary<int, List<GameObject>> mapLayers = new();
    readonly Dictionary<Vector3Int, GameObject> cellObjects = new();
    readonly Dictionary<Transform, RectTransform> trackedIcons = new();
    readonly Dictionary<Transform, int> trackedIconLayers = new();
    readonly HashSet<GameObject> trackedIconObjects = new();

    int currentLayer = 0;
    Vector3Int _mapMin;

    private Sprite GetDeadEnd(Direction direction)
    {
        foreach (var des in deadEndSprites)
            if (des.direction == direction)
                return des.sprite;
        return null;
    }

    public void OnDungeonOpens()
    {
        var input = GameManager.Instance.playMod.LocalPlayer.Player_Input;
        input.actions["mapnext"].started += SetNextLayer;
        input.actions["mapprevious"].started += SetPreviousLayer;
        input.actions["map"].started += ToggleFollowPlayer;
        GenerateMap();
    }

    public void OnDungeonCloses()
    {
        var input = GameManager.Instance.playMod.LocalPlayer.Player_Input;
        input.actions["mapnext"].started -= SetNextLayer;
        input.actions["mapprevious"].started -= SetPreviousLayer;
        input.actions["map"].started -= ToggleFollowPlayer;

        ClearMap();
    }

    private void SetNextLayer(InputAction.CallbackContext ctx) => SetNextLayer();
    public void SetNextLayer() { if (followPlayer) ToggleFollowPlayer(); SetRenderLayer(currentLayer + 1); }

    private void SetPreviousLayer(InputAction.CallbackContext ctx) => SetPreviousLayer();
    public void SetPreviousLayer() { if (followPlayer) ToggleFollowPlayer();  SetRenderLayer(currentLayer - 1); }

    private void ToggleFollowPlayer(InputAction.CallbackContext ctx) => ToggleFollowPlayer();
    public void ToggleFollowPlayer()
    {
        followPlayer = !followPlayer;
        if (followPlayerImg == null) return;

        Color target;
        if (followPlayer)
            ColorUtility.TryParseHtmlString("#DC9632", out target);
        else
            ColorUtility.TryParseHtmlString("#4D4D4D", out target);

        followPlayerImg.color = target;
    }

    public void ToggleFeatures()
    {
        displayFeatures = !displayFeatures;
        foreach (var kv in trackedIcons)
        {
            if (!trackedIconLayers.ContainsKey(kv.Key)) continue;
            if (trackedIconLayers[kv.Key] != currentLayer) continue;

            kv.Value.gameObject.SetActive(displayFeatures);
        }
    }

    private void SetRenderLayer(int layer)
    {
        if (!mapLayers.ContainsKey(layer)) return;

        if (mapLayers.TryGetValue(currentLayer, out var prev))
            foreach (var cell in prev)
                cell.SetActive(false);

        foreach (var cell in mapLayers[layer])
        {
            if (!displayFeatures && trackedIconObjects.Contains(cell))
                continue;

            cell.SetActive(true);
        }

        currentLayer = layer;
    }

    private void Update()
    {
        UpdateMap();
    }

    private void UpdateMap()
    {
        var pData = GameManager.Instance.playMod.LocalPlayer;
        if (pData == null) return;
        var gen = DungeonGenerator.Instance;

        Vector3 worldPos;
        if (pData._PlayerInOffice)
        {
            worldPos = GameManager.Instance.dngMod.HomewardBeacon.transform.position;
            playerDot.gameObject.SetActive(false);
        }
        else
        {
            worldPos = pData.transform.position;
            playerDot.gameObject.SetActive(true);
        }

        int playerLayer = Mathf.RoundToInt(worldPos.y / gen.CellSize);
        if (playerLayer != currentLayer && followPlayer)
            SetRenderLayer(playerLayer);

        playerDot.gameObject.SetActive(!pData._PlayerInOffice && playerLayer == currentLayer);

        float uiX = (worldPos.x / gen.CellSize - _mapMin.x) * cellSize;
        float uiY = (worldPos.z / gen.CellSize - _mapMin.z) * cellSize;
        Vector2 playerMapPos = new(uiX, uiY);

        if (followPlayer)
        {
            mapAnchor.anchoredPosition = -playerMapPos;
            playerDot.anchoredPosition = Vector2.zero;
        }
        else
        {
            playerDot.anchoredPosition = playerMapPos + mapAnchor.anchoredPosition;
        }

        if (!displayFeatures) return;

        foreach (var kvp in trackedIcons)
        {
            if (kvp.Key == null)
            {
                Destroy(kvp.Value.gameObject);
                continue;
            }

            if (kvp.Value == null) continue;

            worldPos = kvp.Key.position;
            kvp.Value.anchoredPosition = new Vector2(
                (worldPos.x / gen.CellSize - _mapMin.x) * cellSize,
                (worldPos.z / gen.CellSize - _mapMin.z) * cellSize);

            int newLayer = Mathf.RoundToInt(worldPos.y / gen.CellSize);
            int oldLayer = trackedIconLayers[kvp.Key];
            if (oldLayer == newLayer) continue;

            mapLayers[oldLayer].Remove(kvp.Value.gameObject);

            if (!mapLayers.ContainsKey(newLayer))
                mapLayers[newLayer] = new();

            mapLayers[newLayer].Add(kvp.Value.gameObject);
            kvp.Value.gameObject.SetActive(newLayer == currentLayer);
            trackedIconLayers[kvp.Key] = newLayer;
        }
    }

    private void GenerateMap()
    {
        var gen = DungeonGenerator.Instance;
        _mapMin = ComputeMapMin(gen.PlacedRooms);

        foreach (var sr in gen.SpawnedRooms)
        {
            DungeonGenerator.PlacedRoom pr = sr.Value.PlacedRoom;
            foreach (var fp in sr.Value.PlacedRoom.data.RoomFootprint)
            {
                Vector3Int worldCell = pr.anchor + fp.Footprint;
                var go = SpawnCell(worldCell.x, worldCell.y, worldCell.z, fp.MapSprite, pr.data.roomColor);

                if (go != null) cellObjects[worldCell] = go;
            }

            foreach (var port in sr.Value.closedPorts)
            {
                Sprite deadEndSprite = GetDeadEnd(port.face);
                if (deadEndSprite == null) continue;

                Vector3Int worldCell = pr.anchor + port.localCell;
                if (!cellObjects.TryGetValue(worldCell, out var parent)) continue;

                SpawnDeadEnd(parent, deadEndSprite, pr.data.roomColor);
            }

            if (gen.RoomItems.TryGetValue(sr.Key, out var items))
                foreach (var item in items)
                    if (item != null) SpawnDynamicIcon(item.transform, itemSprite, itemColor, itemParent, sr.Value.PlacedRoom.anchor.y);

            if (gen.RoomFurniture.TryGetValue(sr.Key, out var furniture))
                foreach (var furn in furniture)
                    if (furn != null) SpawnDynamicIcon(furn.transform, furnSprite, furnColor, furnParent, sr.Value.PlacedRoom.anchor.y);
        }

        var beacon = GameManager.Instance.dngMod.HomewardBeacon;
        if (beacon != null)
        {
            int beaconLayer = Mathf.RoundToInt(beacon.transform.position.y / gen.CellSize);
            SpawnDynamicIcon(beacon.transform, beaconSprite, beaconColor, extraParent, beaconLayer, false);
        }

        int startLayer = gen.StartRoomPos.y / gen.CellSize;
        SetRenderLayer(startLayer);
    }

    private GameObject SpawnCell(int x, int y, int z, Sprite sprite, Color color)
    {
        if (sprite == null) return null;

        GameObject go = Instantiate(mapCellPrefab, cellParent);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = new Vector2((x - _mapMin.x) * cellSize, (z - _mapMin.z) * cellSize);
        rt.localRotation = Quaternion.identity;

        if (go.TryGetComponent(out Image img))
        {
            img.sprite = sprite;
            img.color = color;

            if (Mathf.Approximately(color.r, Color.white.r) &&
                Mathf.Approximately(color.g, Color.white.g) &&
                Mathf.Approximately(color.b, Color.white.b))
            { img.color = mapColor; }

            if (!mapLayers.ContainsKey(y))
                mapLayers[y] = new();

            mapLayers[y].Add(go);
            go.SetActive(false);
            return go;
        }
        else
        {
            Destroy(go);
        }
        return null;
    }

    private void SpawnDeadEnd(GameObject parent, Sprite sprite, Color color)
    {
        GameObject go = Instantiate(deadEndPrefab, parent.transform);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = Vector2.zero;
        rt.localRotation = Quaternion.identity;

        if (go.TryGetComponent(out Image img))
        {
            img.sprite = sprite;
            img.color = color;

            if (Mathf.Approximately(color.r, Color.white.r) &&
                Mathf.Approximately(color.g, Color.white.g) &&
                Mathf.Approximately(color.b, Color.white.b))
            { img.color = mapColor; }

            img.maskable = true;
        }
        else
        {
            Destroy(go);
        }

        go.SetActive(true);
    }

    private void SpawnDynamicIcon(Transform worldTransform, Sprite sprite, Color color, Transform parent, int layer, bool track = true)
    {
        if (sprite == null) return;

        var gen = DungeonGenerator.Instance;
        Vector3 worldPos = worldTransform.position;

        GameObject go = Instantiate(mapFeaturePrefab, parent);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = new Vector2(
            (worldPos.x / gen.CellSize - _mapMin.x) * cellSize,
            (worldPos.z / gen.CellSize - _mapMin.z) * cellSize);
        rt.localRotation = Quaternion.identity;

        if (go.TryGetComponent(out Image img))
        {
            img.sprite = sprite;
            img.color = color;

            if (!mapLayers.ContainsKey(layer))
                mapLayers[layer] = new();
            mapLayers[layer].Add(go);

            if (track)
            {
                trackedIcons[worldTransform] = rt;
                trackedIconLayers[worldTransform] = layer;
                trackedIconObjects.Add(go);
            }
            
            go.SetActive(false);
        }
        else Destroy(go);
    }

    private void ClearMap()
    {
        foreach (var layer in mapLayers.Values)
            foreach (var cell in layer)
                if (cell) Destroy(cell);

        mapLayers.Clear();
        cellObjects.Clear();
        trackedIcons.Clear();
        trackedIconLayers.Clear();
        trackedIconObjects.Clear();

        currentLayer = 0;
    }

    private Vector3Int ComputeMapMin(IReadOnlyList<DungeonGenerator.PlacedRoom> rooms)
    {
        int minX = int.MaxValue, minY = int.MaxValue, minZ = int.MaxValue;
        foreach (var pr in rooms)
            foreach (var fp in pr.data.RoomFootprint)
            {
                var w = pr.anchor + fp.Footprint;
                if (w.x < minX) minX = w.x;
                if (w.y < minY) minY = w.y;
                if (w.z < minZ) minZ = w.z;
            }
        return new Vector3Int(minX, minY, minZ);
    }
}
