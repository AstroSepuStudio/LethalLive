using System;
using System.Collections.Generic;
using UnityEngine;

public class RoomData : MonoBehaviour
{
    [Serializable]
    public struct WallPortKey
    {
        public Vector3Int localCell;
        public Direction face;
        public GameObject wall;
        public GameObject door;
    }

    [SerializeField] private WallPortKey[] ports = Array.Empty<WallPortKey>();
    public RoomDataSO Data;
    public List<LootPosition> itemSpawnPositions;
    public List<FurniturePosition> furnitureSpawnPositions;
    public List<Transform> entitySpawnerPositions;
    public Renderer[] roomRenderers;
    public Light[] roomLights;
    public Bounds roomBounds;

    [SerializeField] bool beingRendered = true;

    private void Awake()
    {
        RecalculateBounds();   
    }

    public void RecalculateBounds()
    {
        if (roomRenderers.Length == 0)
            return;

        roomBounds = roomRenderers[0].bounds;
        for (int i = 1; i < roomRenderers.Length; i++)
            roomBounds.Encapsulate(roomRenderers[i].bounds);
    }

    public void SetPort(Vector3Int localCell, Direction face, bool open)
    {
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

        foreach (Light light in roomLights)
        {
            light.enabled = shouldRender;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;

        foreach (var pos in itemSpawnPositions)
        {
            if (pos.position == null) continue;

            Vector3 center = pos.position.position;
            Vector3 size = pos.maxOffset * 2f;

            Gizmos.DrawWireCube(center, size);
        }
    }
}
