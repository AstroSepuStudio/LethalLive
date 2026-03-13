using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FurnitureEntity : EntityStats
{
    [Header("Furniture")]
    [SerializeField] Renderer furRenderer;
    [SerializeField] FurnitureDataSO dataSO;
    [SerializeField] Rigidbody rb;

    public List<LootPosition> lootPositions;

    bool _dying;
    public bool IsDying => _dying;

    public void SetRender(bool active)
        => furRenderer.enabled = active;

    protected override void Awake()
    {
        base.Awake();
        
        dataSO = Instantiate(dataSO);
    }

    [Server]
    public override void ApplyDamage(AttackEvent source)
    {
        currentHP = Mathf.Clamp(currentHP - source.AttackStat_.AttackDamage, 0f, maxHP);

        if (currentHP <= 0f)
        {
            CheckDropThresholds(false);
            HandleDeath(source);
            return;
        }

        CheckDropThresholds(true);

        if (source.AttackStat_.AttackDamage >= 10f) 
            PlaySFX(SFXEvent.StrongHit);
        else 
            PlaySFX(SFXEvent.TakeDamage);
    }

    [Server]
    public override void AddKnock(float amount, Vector3 momentum)
    {
        base.AddKnock(amount, momentum);
        rb.AddForce(momentum, ForceMode.Impulse);
    }

    [Server]
    protected override void HandleDeath(AttackEvent source)
    {
        if (_dying) return;
        _dying = true;

        if (!sfxMap.TryGetValue(SFXEvent.Died, out var group) || group.Clips.Length == 0) return;
        RpcFurnitureBreaks(Random.Range(0, group.Clips.Length));
        foreach (var item in dataSO.lootTable) TryDropItem(item);
        StartCoroutine(DelayedDestroy());
    }

    [ClientRpc]
    void RpcFurnitureBreaks(int clipIndex)
    {
        SetRender(false);
        if (!sfxMap.TryGetValue(SFXEvent.Died, out var group) || clipIndex >= group.Clips.Length) return;
        AudioManager.Instance.PlayOneShotAndDestroy(transform.position, group.Clips[clipIndex], gameObject, SoundLoudness.Average);
    }

    IEnumerator DelayedDestroy()
    {
        yield return new WaitForSeconds(0.2f);
        NetworkServer.Destroy(gameObject);
    }

    void CheckDropThresholds(bool playSFX)
    {
        bool anyDropped = false;

        for (int i = 0; i < dataSO.dropThresholds.Length; i++)
        {
            if (dataSO.dropThresholds[i].triggered) continue;

            float hpThreshold = maxHP * (dataSO.dropThresholds[i].dropThreshold / 100f);
            if (currentHP > hpThreshold) continue;

            if (TryDropItem(dataSO.dropThresholds[i].Item_Drop))
                anyDropped = true;

            dataSO.dropThresholds[i].triggered = true;
        }

        if (anyDropped && playSFX) PlaySFX(SFXEvent.PartialBreak);
    }

    [Server]
    bool TryDropItem(FurnitureDataSO.ItemDrop drop)
    {
        float rand = Random.Range(0f, 100f);
        if (rand > drop.dropChance) return false;

        int qty = Random.Range(drop.minQuantity, drop.maxQuantity);
        for (int i = 0; i < qty; i++)
        {
            Vector2 circle = Random.insideUnitCircle.normalized;
            float dist = Random.Range(dataSO.horizontalMinDistance, dataSO.horizontalMaxDistance);
            Vector3 offset = new Vector3(circle.x, 0, circle.y) * dist;
            offset.y = Random.Range(dataSO.verticalMinDistance, dataSO.verticalMaxDistance);
            Vector3 pos = transform.position + offset;

            GameObject spawned = Instantiate(drop.Item.itemPrefab, pos, Quaternion.identity);
            NetworkServer.Spawn(spawned);

            if (spawned.TryGetComponent(out ItemBase item))
                item.AddForce((pos - transform.position).normalized * Random.Range(1f, 2f), ForceMode.Impulse);
        }
        return true;
    }

    private void OnDrawGizmos()
    {
        if (GameManager.Instance == null || !GameManager.Instance.debug) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 1.5f);
    }
}