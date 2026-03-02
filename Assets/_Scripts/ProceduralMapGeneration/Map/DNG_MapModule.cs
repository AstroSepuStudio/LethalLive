using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class DNG_MapModule : MonoBehaviour
{
    [SerializeField] GameObject imgPrefab;
    [SerializeField] Transform targetParent;
    [SerializeField] RectTransform playerDot;
    [SerializeField] float cellSize = 200;

    readonly Dictionary<int, List<GameObject>> mapLayers = new();
    int currentLayer = 0;
    Vector3Int _mapMin;

    public void OnDungeonOpens()
    {
        var input = GameManager.Instance.playMod.LocalPlayer.Player_Input;
        input.actions["mapnext"].started += SetNextLayer;
        input.actions["mapprevious"].started += SetPreviousLayer;
        GenerateMap();
    }

    public void OnDungeonCloses()
    {
        var input = GameManager.Instance.playMod.LocalPlayer.Player_Input;
        input.actions["mapnext"].started -= SetNextLayer;
        input.actions["mapprevious"].started -= SetPreviousLayer;

        ClearMap();
    }

    private void SetNextLayer(InputAction.CallbackContext ctx)
    {
        SetRenderLayer(currentLayer + 1);
    }

    private void SetPreviousLayer(InputAction.CallbackContext ctx)
    {
        SetRenderLayer(currentLayer - 1);
    }

    private void Update()
    {
        if (GameManager.Instance.playMod.LocalPlayer == null) return;

        var gen = DungeonGenerator.Instance;
        Vector3 worldPos = GameManager.Instance.playMod.LocalPlayer.transform.position;

        int playerLayer = Mathf.RoundToInt(worldPos.y / gen.CellSize);
        playerDot.gameObject.SetActive(playerLayer == currentLayer);

        float uiX = (worldPos.x / gen.CellSize - _mapMin.x) * cellSize;
        float uiY = (worldPos.z / gen.CellSize - _mapMin.z) * cellSize;

        playerDot.anchoredPosition = new Vector2(uiX, uiY);
        playerDot.SetAsLastSibling();
    }

    private void GenerateMap()
    {
        var gen = DungeonGenerator.Instance;
        _mapMin = ComputeMapMin(gen.PlacedRooms);

        foreach (var pr in gen.PlacedRooms)
        {
            foreach (var fp in pr.data.RoomFootprint)
            {
                Vector3Int worldCell = pr.anchor + fp.Footprint;
                SpawnCell(worldCell.x, worldCell.y, worldCell.z, fp.MapSprite, pr.data.roomColor);
            }
        }

        int startLayer = gen.StartRoomPos.y / gen.CellSize;
        SetRenderLayer(startLayer);
    }

    private void SpawnCell(int x, int y, int z, Sprite sprite, Color color)
    {
        if (sprite == null) return;

        GameObject go = Instantiate(imgPrefab, targetParent);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = new Vector2((x - _mapMin.x) * cellSize, (z - _mapMin.z) * cellSize);
        rt.localRotation = Quaternion.identity;

        if (go.TryGetComponent(out Image img))
        {
            img.sprite = sprite;
            img.color = color;

            if (!mapLayers.ContainsKey(y))
                mapLayers[y] = new();

            mapLayers[y].Add(go);
            go.SetActive(false);
        }
        else
        {
            Destroy(go);
        }
    }

    private void SetRenderLayer(int layer)
    {
        if (!mapLayers.ContainsKey(layer)) return;

        // Hide current
        if (mapLayers.TryGetValue(currentLayer, out var prev))
            foreach (var cell in prev)
                cell.SetActive(false);

        // Show new
        foreach (var cell in mapLayers[layer])
            cell.SetActive(true);

        currentLayer = layer;
    }

    private void ClearMap()
    {
        foreach (var layer in mapLayers.Values)
            foreach (var cell in layer)
                if (cell) Destroy(cell);

        mapLayers.Clear();
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
