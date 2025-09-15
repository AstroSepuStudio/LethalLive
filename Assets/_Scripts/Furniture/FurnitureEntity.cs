using System;
using System.Collections.Generic;
using UnityEngine;

public class FurnitureEntity : EntityStats
{
    [SerializeField] Rigidbody rb;

    public List<LootPosition> lootPositions;

    [SerializeField] ItemDropThreshold[] dropThresholds;
    [SerializeField] ItemDrop[] lootTable;

    [Serializable]
    struct ItemDrop
    {
        public ItemSO Item;
        public float dropChance;
        public int minQuantity;
        public int maxQuantity;
    }

    [Serializable]
    struct ItemDropThreshold
    {
        public ItemDrop Item_Drop;
        public float dropThreshold; // Ex: 50% (currentHP)
        public bool dropped;
    }

    public override void ModifyKnock(float amount, Vector3 momentum)
    {
        base.ModifyKnock(amount, momentum);

        rb.AddForce(momentum, ForceMode.Impulse);
    }

    public override void ModifyHP(float amount)
    {
        currentHP = Mathf.Clamp(currentHP + amount, 0f, maxHP);

        for (int i = 0; i < dropThresholds.Length; i++)
        {
            if (dropThresholds[i].dropped) continue;

            float rand = UnityEngine.Random.Range(0f, 100f);
            if (rand > dropThresholds[i].Item_Drop.dropChance)
                continue;

            float hpThreshold = maxHP * (dropThresholds[i].dropThreshold / 100);
            if (currentHP <= hpThreshold)
            {
                int quantity = UnityEngine.Random.Range(dropThresholds[i].Item_Drop.minQuantity, dropThresholds[i].Item_Drop.maxQuantity);

                // TODO: Drop Item
                Debug.Log($"Dropping {quantity} '{dropThresholds[i].Item_Drop.Item.itemName}'");
                dropThresholds[i].dropped = true;
            }
        }

        if (currentHP <= 0)
        {
            OnDeath();
        }
    }

    protected override void OnDeath()
    {
        foreach (var item in lootTable)
        {
            float rand = UnityEngine.Random.Range(0f, 100f);
            if (rand > item.dropChance)
                continue;

            int quantity = UnityEngine.Random.Range(item.minQuantity, item.maxQuantity);

            // TODO: Drop Item
            Debug.Log($"Dropping {quantity} '{item.Item.itemName}'");
        }
    }
}
