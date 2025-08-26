using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemBase : InteractableObject
{
    public ItemSO ItemData;
    [SerializeField] NetworkIdentity identity;
    [SerializeField] CharacterController controller;
    [SerializeField] GravityController gravity;

    [SerializeField] TextMeshProUGUI itemNameTxt;
    [SerializeField] TextMeshProUGUI itemPriceTxt;

    public uint ID => identity.netId;

    [SyncVar]
    public int Index = 0;

    [SyncVar]
    public int ItemValue;

    protected override void Start()
    {
        base.Start();

        if (isServer)
        {
            ItemValue = Random.Range(ItemData.minValue, ItemData.maxValue);
        }
    }

    public virtual void PrimaryAction() { }
    public virtual void SecondaryAction() { }

    public override void OnInteract(PlayerData sourceData)
    {
        sourceData.PlayerInventory.AddItem(this);
    }

    public virtual void OnPickUp()
    {
        controller.enabled = false;
        gravity.enabled = false;
        DisableCanvas();
    }

    public virtual void OnEquip(ItemInventory handler)
    {
        controller.enabled = false; 
        gameObject.SetActive(true);
    }

    public virtual void OnUnequip(ItemInventory handler)
    {
        controller.enabled = false; 
        gameObject.SetActive(false);
    }

    public virtual void OnDrop(ItemInventory handler)
    {
        gameObject.SetActive(true);
        transform.SetParent(null);
        controller.enabled = true;
        gravity.enabled = true;
    }

    public override void EnableCanvas()
    {
        base.EnableCanvas();

        itemNameTxt.SetText(ItemData.itemName);
        itemPriceTxt.SetText(ItemValue.ToString());
    }
}
