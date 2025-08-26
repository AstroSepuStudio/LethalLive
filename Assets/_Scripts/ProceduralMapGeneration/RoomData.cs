using System;
using UnityEngine;

public class RoomData : MonoBehaviour
{
    [Serializable]
    public struct WallPortKey
    {
        public Vector3Int localCell; // must match RoomDataSO.RoomPort.localCell
        public Direction face; // must match RoomDataSO.RoomPort.face
        public GameObject wall; // enable when closed
        public GameObject door; // enable when open (optional)
    }


    [Header("Port bindings (author in prefab variant)")]
    [SerializeField] private WallPortKey[] ports = Array.Empty<WallPortKey>();

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
}
