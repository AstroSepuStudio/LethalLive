using System;
using UnityEngine;
using static LL_Tier;

[CreateAssetMenu(menuName = "LethalLive/Furniture")]
public class FurnitureDataSO : ScriptableObject, IHaveTier
{
    public GameObject Prefab;

    public Tier Tier;
    public Tier GetTier() => Tier;

    public ItemDropThreshold[] dropThresholds;
    public ItemDrop[] lootTable;

    public float horizontalMinDistance = 1f;
    public float horizontalMaxDistance = 2f;
    public float verticalMinDistance = 0.2f;
    public float verticalMaxDistance = 0.6f;

    [Serializable]
    public struct ItemDrop
    {
        public ItemSO Item;
        public float dropChance;
        public int minQuantity;
        public int maxQuantity;
    }

    [Serializable]
    public struct ItemDropThreshold
    {
        public ItemDrop Item_Drop;
        public float dropThreshold; // Ex: 50% (currentHP)
        public bool dropped;
    }

    [Serializable]
    public struct FurniturePosition
    {
        public Transform position;
        public Vector3 maxOffset;
        public float maxRotation;
        public float chance;
    }
}
