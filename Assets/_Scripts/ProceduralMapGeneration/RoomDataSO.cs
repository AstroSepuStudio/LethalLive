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
    public Vector3Int[] Footprint = new Vector3Int[] { Vector3Int.zero };
    public RoomPort[] Ports = Array.Empty<RoomPort>();

    public bool ContainsLocalCell(in Vector3Int local)
    {
        for (int i = 0; i < Footprint.Length; i++) if (Footprint[i] == local) return true;
        return false;
    }
}
