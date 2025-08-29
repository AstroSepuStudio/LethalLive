using System;
using UnityEngine;

[CreateAssetMenu(menuName = "LethalLive/RoomData")]
public class RoomDataSO : ScriptableObject
{
    [Serializable]
    public struct RoomPort
    {
        public Vector3Int localCell;
        public Direction face;
        public PortType type;
    }

    public enum PortType
    {
        Doorway,
        Continuous
    }

    public GameObject Prefab;
    public Biome biome = Biome.Default;

    [Tooltip("Local footprint cells occupied by this room, relative to anchor (0,0,0).\nExamples: 1x1 = {(0,0,0)}; 2x1 east = {(0,0,0),(1,0,0)}; 2x2 on XZ = {(0,0,0),(1,0,0),(0,0,1),(1,0,1)}.")]
    public Vector3Int[] Footprint = new Vector3Int[] { Vector3Int.zero };

    [Tooltip("Doorway definitions: cell + face pairs. Each defines a potential connection.")]
    public RoomPort[] Ports = Array.Empty<RoomPort>();

    public bool ContainsLocalCell(in Vector3Int local)
    {
        for (int i = 0; i < Footprint.Length; i++) if (Footprint[i] == local) return true;
        return false;
    }
}
