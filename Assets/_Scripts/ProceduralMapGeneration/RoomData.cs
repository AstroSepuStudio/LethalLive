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
    public List<LootPosition> itemSpawnPositions;
    public List<FurniturePosition> furnitureSpawnPositions;

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
