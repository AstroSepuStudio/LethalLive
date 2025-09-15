using Mirror;
using System.Collections.Generic;
using UnityEngine;

public class ItemHurtbox : NetworkBehaviour
{
    [SerializeField] bool startEnabled = false;
    [SerializeField] Collider hitbox;
    [SerializeField] ItemBase item;

    List<GameObject> hitEntities = new();

    private void Start()
    {
        if (!startEnabled)
            enabled = false;
    }

    public void EnableHitbox()
    {
        hitEntities.Clear();
        hitbox.enabled = true;
    }

    public void DisableHitbox()
    {
        hitbox.enabled = false;
        hitEntities.Clear();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isServer) return;

        if (other.gameObject == item.pData.gameObject) return;

        if (hitEntities.Contains(other.gameObject)) return;

        if (other.TryGetComponent(out EntityStats stats))
        {
            if (stats is PlayerStats pStats)
            {
                pStats.ReceiveAttack(item.pData, item.primaryAtkStats);
            }
            else
            {
                stats.ReceiveAttack(item.pData.Player_Stats, item.primaryAtkStats);
            }

            hitEntities.Add(other.gameObject);
        }
    }
}
