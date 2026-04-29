using Mirror;
using System.Collections.Generic;
using UnityEngine;

public class ItemHurtbox : NetworkBehaviour
{
    [SerializeField] bool startEnabled = false;
    [SerializeField] Collider hitbox;

    IA_HurtboxAttack itemAction;

    readonly List<GameObject> hitEntities = new();

    private void Start()
    {
        if (!startEnabled) enabled = false;
    }

    public void Initialize(IA_HurtboxAttack itemAction)
    {
        this.itemAction = itemAction;
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

        if (itemAction.Item == null || itemAction.Item.PData == null) return;
        if (other.gameObject == itemAction.Item.PData.gameObject) return;
        if (hitEntities.Contains(other.gameObject)) return;

        if (!other.TryGetComponent(out EntityStats target))
        {
            itemAction.HurtBoxHit();
            return;
        }

        itemAction.HurtBoxHurt(target);

        target.ReceiveAttack(AttackEvent.From(itemAction.Item.PData, target, itemAction.AttackStat));
        hitEntities.Add(other.gameObject);
    }
}
