using Mirror;
using System.Collections.Generic;
using UnityEngine;

public class FurnitureEntity : EntityStats
{
    [SerializeField] Rigidbody rb;
    [SerializeField] AudioSFX[] strongHitSFX;

    public List<LootPosition> lootPositions;

    [SerializeField] ItemDropThreshold[] dropThresholds;
    [SerializeField] ItemDrop[] lootTable;

    [SerializeField] float minDistance = 1f;
    [SerializeField] float maxDistance = 2f;

    [System.Serializable]
    struct ItemDrop
    {
        public ItemSO Item;
        public float dropChance;
        public int minQuantity;
        public int maxQuantity;
    }

    [System.Serializable]
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

    public override void ModifyHP(EntityStats source, AttackStat attack)
    {
        currentHP = Mathf.Clamp(currentHP - attack.AttackDamage, 0f, maxHP);

        for (int i = 0; i < dropThresholds.Length; i++)
        {
            if (dropThresholds[i].dropped) continue;

            float hpThreshold = maxHP * (dropThresholds[i].dropThreshold / 100);
            if (currentHP <= hpThreshold)
            {
                if (TryDropItem(dropThresholds[i].Item_Drop))
                    dropThresholds[i].dropped = true;
            }
        }

        if (currentHP <= 0)
        {
            OnDeath(source, attack);
        }
        else if (attack.AttackDamage >= 10f)
        {
            RequestPlaySFX(3);
        }
        else
        {
            RequestPlaySFX(0);
        }
    }

    protected override void OnDeath(EntityStats source, AttackStat attack)
    {
        RequestPlaySFX(2);

        foreach (var item in lootTable)
        {
            TryDropItem(item);
        }

        NetworkServer.Destroy(gameObject);
    }

    [Server]
    bool TryDropItem(ItemDrop ItemDrop)
    {
        float rand = Random.Range(0f, 100f);
        if (rand > ItemDrop.dropChance)
            return false;

        int quantity = Random.Range(ItemDrop.minQuantity, ItemDrop.maxQuantity);

        for (int q = 0; q < quantity; q++)
        {
            Vector2 randomCircle = Random.insideUnitCircle.normalized;
            float distance = Random.Range(minDistance, maxDistance);
            Vector3 offset = new Vector3(randomCircle.x, 1f, randomCircle.y) * distance;
            Vector3 pos = transform.position + offset;
            GameObject spawned = Instantiate(ItemDrop.Item.itemPrefab, pos, Quaternion.identity);
            NetworkServer.Spawn(spawned);

            Vector3 dir = (pos - transform.position).normalized;
            if (spawned.TryGetComponent(out Rigidbody rb))
                rb.AddForce(dir * Random.Range(0.2f, 1f), ForceMode.Impulse);
        }

        return true;
    }

    protected override void RequestPlaySFX(int index)
    {
        int sfxIndex = 0;
        if (index == 0 && takeDamageSFX.Length > 0)
            sfxIndex = Random.Range(0, takeDamageSFX.Length);
        else if (index == 1 && knockedSFX.Length > 0)
            sfxIndex = Random.Range(0, knockedSFX.Length);
        else if (index == 2 && diedSFX.Length > 0)
            sfxIndex = Random.Range(0, diedSFX.Length);
        else if (index == 3 && strongHitSFX.Length > 0)
            sfxIndex = Random.Range(0, strongHitSFX.Length);

        RpcPlaySFX(index, sfxIndex);
    }

    protected override void RpcPlaySFX(int index, int sfxIndex)
    {
        if (audioSource == null) return;

        if (index == 0 && takeDamageSFX.Length > 0)
            AudioManager.Instance.PlayOneShot(audioSource, takeDamageSFX[sfxIndex]);
        else if (index == 1 && knockedSFX.Length > 0)
            AudioManager.Instance.PlayOneShot(audioSource, knockedSFX[sfxIndex]);
        else if (index == 2 && diedSFX.Length > 0)
            AudioManager.Instance.PlayOneShotAndDestroy(transform.position, diedSFX[sfxIndex]);
        else if (index == 3 && strongHitSFX.Length > 0)
            AudioManager.Instance.PlayOneShot(audioSource, strongHitSFX[sfxIndex]);
    }
}
