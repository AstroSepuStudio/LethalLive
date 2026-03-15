using System;
using System.Collections.Generic;
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

    public FootprintStr[] FootprintCorners = Array.Empty<FootprintStr>();

    public bool ContainsLocalCell(in Vector3Int local)
    {
        for (int i = 0; i < RoomFootprint.Length; i++) if (RoomFootprint[i].Footprint == local) return true;
        return false;
    }

#if UNITY_EDITOR
    [ContextMenu("Populate Footprint From Corners")]
    public void PopulateFootprintFromCorners()
    {
        if (FootprintCorners.Length % 2 != 0)
        {
            Debug.LogWarning("footprintCorners must have an even number of entries (pairs of corners).");
            return;
        }

        var result = new List<FootprintStr>();

        for (int i = 0; i < FootprintCorners.Length; i += 2)
        {
            FootprintStr cornerA = FootprintCorners[i];
            FootprintStr cornerB = FootprintCorners[i + 1];

            Vector3Int min = Vector3Int.Min(cornerA.Footprint, cornerB.Footprint);
            Vector3Int max = Vector3Int.Max(cornerA.Footprint, cornerB.Footprint);

            for (int x = min.x; x <= max.x; x++)
                for (int y = min.y; y <= max.y; y++)
                    for (int z = min.z; z <= max.z; z++)
                    {
                        var cell = new Vector3Int(x, y, z);

                        result.Add(new FootprintStr
                        {
                            Footprint = cell
                        });
                    }
        }

        RoomFootprint = result.ToArray();
        UnityEditor.EditorUtility.SetDirty(this);
        Debug.Log($"Populated {RoomFootprint.Length} footprint cells from {FootprintCorners.Length / 2} corner pair(s).");
    }
#endif
}
