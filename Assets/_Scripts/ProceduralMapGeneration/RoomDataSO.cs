using System;
using UnityEngine;

[CreateAssetMenu(menuName = "LethalLive/RoomData")]
public class RoomDataSO : ScriptableObject
{
    public enum PortType { Doorway, Continuous }

    [Serializable]
    public struct RoomPort
    {
        public Vector3Int localCell;
        public Direction face;
        public PortType type;
    }

    [Serializable]
    public struct FootprintStr
    {
        public Vector3Int Footprint;
        public Sprite MapSprite;
    }

    public GameObject Prefab;
    public Biome biome = Biome.Default;
    public LL_Tier.Tier RoomTier;
    public bool ValidVortexHome = true;

    public Color roomColor = Color.white;
    public FootprintStr[] RoomFootprint = Array.Empty<FootprintStr>();

    public RoomPort[] Ports = Array.Empty<RoomPort>();

    public bool ContainsLocalCell(in Vector3Int local)
    {
        for (int i = 0; i < RoomFootprint.Length; i++) if (RoomFootprint[i].Footprint == local) return true;
        return false;
    }
}
