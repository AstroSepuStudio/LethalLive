using UnityEngine;
using UnityEngine.Events;

public class AIModule_ItemCarrier : AIModule
{
    [SerializeField] Transform pickUpPos;
    [SerializeField] float dropCooldownDuration = 3f;

    public ItemBase CarriedItem { get; private set; }
    public bool HasItem => CarriedItem != null;
    public float PostDropCooldown => postDropCooldown;
    public bool IsOnDropCooldown => postDropCooldown > 0f;

    public UnityEvent OnItemCarried;
    public UnityEvent OnItemDropped;

    float postDropCooldown;

    public override void OnModuleTick(AIBrain brain)
    {
        if (postDropCooldown > 0f)
            postDropCooldown -= Time.deltaTime;
    }

    public void CarryItem(ItemBase item, AIBrain brain)
    {
        brain.PlaySFX(AIBrain.SFXEvent.Happy, 1f);
        CarriedItem = item;
        item.OnPickUp();
        item.transform.SetParent(pickUpPos);
        item.transform.localPosition = Vector3.zero;
        OnItemCarried?.Invoke();
    }

    public void DropCarriedItem()
    {
        if (CarriedItem == null) return;
        CarriedItem.OnDrop(null);
        CarriedItem.transform.SetParent(null);
        CarriedItem = null;
        postDropCooldown = dropCooldownDuration;
        OnItemDropped?.Invoke();
    }
}
