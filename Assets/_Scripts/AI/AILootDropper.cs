using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(NetworkIdentity))]
public class AILootDropper : NetworkBehaviour
{
    [System.Serializable]
    public struct SpawnedLoot
    {
        public ItemBase item;
        public int quantity;
    }

    [Header("Loot Drop Settings")]
    [SerializeField] AIBrain brain;
    [SerializeField] float dropScatterRadius = 0.6f;
    [SerializeField] float dropUpwardForce = 3f;
    [SerializeField] float dropOutwardForce = 2f;
    [SerializeField] protected float defaultDropDelay = 1.2f;

    readonly List<SpawnedLoot> pendingLoot = new();

    public System.Action<ItemBase> OnLootSpawned;

    public override void OnStartServer()
    {
        base.OnStartServer();

        if (brain == null)
        {
            if (!TryGetComponent(out brain))
            {
                Debug.LogWarning($"[AILootDropper] No AIBrain found on {gameObject.name}. Loot will never drop.");
                return;
            }
        }
        
        PreSpawnLoot(brain.EntityStats_);
    }

    [Server]
    void PreSpawnLoot(EntityStats stats)
    {
        if (stats == null) return;

        AIBrain.LootDrop[] pool = brain.GetLootPool();
        if (pool == null || pool.Length == 0) return;

        foreach (var drop in pool)
        {
            if (drop.lootData == null || drop.lootData.itemPrefab == null) continue;
            if (Random.value * 100f > drop.dropChance) continue;

            int qty = Random.Range(drop.minQuantity, drop.maxQuantity + 1);
            if (qty <= 0) continue;

            GameObject lootObj = Instantiate(
                drop.lootData.itemPrefab,
                transform.position,
                Quaternion.identity);

            if (!lootObj.TryGetComponent<ItemBase>(out var itemBase)) continue;

            itemBase.SetKinematic(true);
            itemBase.DisplayItem(false);

            NetworkServer.Spawn(lootObj);
            RpcSetLootVisible(itemBase.netId, false);

            pendingLoot.Add(new SpawnedLoot { item = itemBase, quantity = qty });
            OnLootSpawned?.Invoke(itemBase);
        }
    }

    [Server]
    public void OnOwnerDeath(AttackEvent source)
    {
        StartCoroutine(DelayedReveal(source));
    }

    [Server]
    IEnumerator DelayedReveal(AttackEvent source)
    {
        float delay = GetDropDelay(source);
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        RevealLoot();
    }

    [Server]
    void RevealLoot()
    {
        foreach (var entry in pendingLoot)
        {
            if (entry.item == null) continue;

            Vector2 circle = Random.insideUnitCircle * dropScatterRadius;
            Vector3 spawnPos = transform.position + new Vector3(circle.x, 0.2f, circle.y);
            entry.item.transform.position = spawnPos;

            entry.item.SetKinematic(false);

            Vector3 outDir = spawnPos - transform.position;
            outDir.y = 0f;
            outDir = outDir == Vector3.zero
                ? new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)).normalized
                : outDir.normalized;

            entry.item.AddForce(outDir * dropOutwardForce + Vector3.up * dropUpwardForce, ForceMode.Impulse);

            RpcSetLootVisible(entry.item.netId, true);
        }

        pendingLoot.Clear();
    }

    protected virtual float GetDropDelay(AttackEvent source) => defaultDropDelay;

    [ClientRpc]
    void RpcSetLootVisible(uint itemNetId, bool visible)
    {
        StartCoroutine(WaitAndSetVisible(itemNetId, visible));
    }

    IEnumerator WaitAndSetVisible(uint itemNetId, bool visible)
    {
        NetworkIdentity netIdentity = null;
        while (netIdentity == null)
        {
            NetworkClient.spawned.TryGetValue(itemNetId, out netIdentity);
            yield return null;
        }

        if (netIdentity.TryGetComponent(out ItemBase item))
            item.DisplayItem(visible);
    }
}