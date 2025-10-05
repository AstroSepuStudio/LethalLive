using Mirror;
using TMPro;
using UnityEngine;

public class ItemBase : InteractableObject
{
    public ItemSO ItemData;
    [SerializeField] protected NetworkIdentity identity;
    [SerializeField] protected Rigidbody rb;
    [SerializeField] protected Collider coll;

    [SerializeField] TextMeshProUGUI itemNameTxt;
    [SerializeField] TextMeshProUGUI itemPriceTxt;

    public uint ID => identity.netId;

    [SyncVar]
    public int Index = 0;

    [SyncVar]
    public int ItemValue;

    public PlayerData pData { get; private set; }

    public AttackStat primaryAtkStats;
    public AttackStat secondaryAtkStats;

    [SyncVar]
    public bool InUse = false;

    protected virtual void Start()
    {
        if (isServer && ItemData.isSellable)
            ItemValue = Random.Range(ItemData.minValue, ItemData.maxValue);
    }

    [Server]
    public override void OnInteract(PlayerData sourceData)
    {
        if (!ItemData.pickable) return;

        if (sourceData.PlayerInventory.AddItem(this))
        {
            pData = sourceData;
            RpcGetPlayerData(sourceData.netId);
        }
    }

    [ClientRpc]
    protected void RpcGetPlayerData(uint playerID)
    {
        NetworkClient.spawned.TryGetValue(playerID, out NetworkIdentity identity);

        if (identity == null) return;
        pData = identity.GetComponent<PlayerData>();
    }

    public virtual void OnPickUp()
    {
        rb.isKinematic = true;
        coll.enabled = false;
        canvas.DisableCanvas();
    }

    public virtual void OnEquip(ItemInventory handler)
    {
        gameObject.SetActive(true);
    }

    public virtual void OnUnequip(ItemInventory handler)
    {
        gameObject.SetActive(false);
    }

    public virtual void OnDrop(ItemInventory handler)
    {
        gameObject.SetActive(true);
        coll.enabled = true;
        transform.SetParent(null);
        rb.isKinematic = false;
    }

    public override void EnableCanvas()
    {
        canvas.EnableCanvas();
    
        itemNameTxt.SetText(ItemData.itemName);

        if (ItemData.isSellable && itemPriceTxt != null)
            itemPriceTxt.SetText($"${ItemValue}");
    }

    public virtual void PrimaryAction() { }
    public virtual void SecondaryAction() { }
    public virtual void CancelPrimaryAction() { }
    public virtual void CancelSecondaryAction() { }
    public virtual void PrimaryAnimationTrigger() { }
    public virtual void PrimaryAnimationFinish() { }
    public virtual void SecondaryAnimationTrigger() { }
    public virtual void SecondaryAnimationFinish() { }
}
