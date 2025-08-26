using Mirror;
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class ItemInventory : NetworkBehaviour
{
    [SerializeField] PlayerData pData;
    [SerializeField] CanvasGroup nameGroup;
    [SerializeField] TextMeshProUGUI itemName;
    [SerializeField] HotbarSlot[] hotbarSlots;

    [SyncVar]
    public int selectedSlotIndex = 0;

    ItemBase[] inventorySlots = new ItemBase[4];
    ItemBase equippedItem;
    WaitForSeconds sleepNameDisplay = new (1f);

    public bool ItemEquipped => equippedItem != null;
    public bool HasPrimaryAction => equippedItem != null && equippedItem.ItemData.hasPrimaryAction;
    public bool HasSecondaryAction => equippedItem != null && equippedItem.ItemData.hasSecondaryAction;
    public bool HasTwoHandedEquipped => equippedItem != null && equippedItem.ItemData.isTwoHanded;

    private void Start()
    {
        LocalSelectSlot(0);
    }

    public bool IsFull()
    {
        foreach (var slot in inventorySlots)
        {
            if (slot == null)
                return false;
        }

        return true;
    }

    #region Input
    public void PrimaryInput() => CmdUsePrimary();

    public void SecondaryInput() => CmdUseSecondary();

    public void OnClientDrop(InputAction.CallbackContext context)
    {
        if (!pData.isLocalPlayer) return;
        if (!context.started) return;

        CmdDropItem();
    }

    public void SelectSlot_00(InputAction.CallbackContext context)
    {
        if (!context.started) return;

        CmdSelectSlot(0);
    }

    public void SelectSlot_01(InputAction.CallbackContext context)
    {
        if (!context.started) return;

        CmdSelectSlot(1);
    }

    public void SelectSlot_02(InputAction.CallbackContext context)
    {
        if (!context.started) return;

        CmdSelectSlot(2);
    }
    public void SelectSlot_03(InputAction.CallbackContext context)
    {
        if (!context.started) return;

        CmdSelectSlot(3);
    }
    #endregion

    [Server]
    public void AddItem(ItemBase item)
    {
        if (item == null) return;
        if (IsFull()) return;

        int index = selectedSlotIndex;
        if (inventorySlots[index] != null)
        {
            for (int i = 0; i < inventorySlots.Length; i++)
            {
                if (inventorySlots[i] != null) continue;

                index = i;
                break;
            }
        }

        inventorySlots[index] = item;
        item.OnPickUp();
        hotbarSlots[index].SetItem(item);
        LocalSelectSlot(index);
        RpcAddItem(item.ID, index);
    }

    [ClientRpc]
    void RpcAddItem(uint itemID, int slotIndex)
    {
        NetworkClient.spawned.TryGetValue(itemID, out NetworkIdentity identity);

        if (identity == null) return;
        inventorySlots[slotIndex] = identity.gameObject.GetComponent<ItemBase>();
        inventorySlots[slotIndex].OnPickUp();
        hotbarSlots[slotIndex].SetItem(inventorySlots[slotIndex]);
        LocalSelectSlot(slotIndex);
    }

    [Command]
    void CmdSelectSlot(int index)
    {
        if (HasTwoHandedEquipped) return;

        LocalSelectSlot(index);
        RpcSelectSlot(index);
    }

    [ClientRpc]
    void RpcSelectSlot(int index)
    {
        LocalSelectSlot(index);
    }

    void LocalSelectSlot(int index)
    {
        hotbarSlots[selectedSlotIndex].DeselectSlot();
        selectedSlotIndex = index;
        hotbarSlots[index].SelectSlot();
        EquipItemFromSlot(index);
    }
    
    void EquipItemFromSlot(int index)
    {
        if (index < 0 || index >= inventorySlots.Length) return;

        if (equippedItem != null)
            equippedItem.OnUnequip(this);

        ItemBase item = inventorySlots[index];
        if (item == null)
        {
            equippedItem = null;
            return;
        }

        equippedItem = item;
        item.OnEquip(this);

        item.transform.SetParent(pData.Skin_Data.RightHand);
        item.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        StartCoroutine(DisplayItemName(item.ItemData.itemName));

        if (item.ItemData.isTwoHanded)
        {
            pData.Skin_Data.Rigging_Manager.EnableTwoHandedRig();
        }
        else
        {
            pData.Skin_Data.Rigging_Manager.DisableTwoHandedRig();
        }
    }

    [Command]
    void CmdDropItem()
    {
        if (equippedItem == null) return;

        LocalDropItem();
        RpcDropItem();
    }

    [ClientRpc]
    void RpcDropItem() => LocalDropItem();

    void LocalDropItem()
    {
        if (equippedItem == null) return;

        for (int i = 0; i < inventorySlots.Length; i++)
        {
            if (inventorySlots[i] != equippedItem) continue;

            inventorySlots[i] = null;
            hotbarSlots[i].RemoveItem();
            break;
        }

        if (equippedItem.ItemData.isTwoHanded)
        {
            pData.Skin_Data.Rigging_Manager.DisableTwoHandedRig();
        }

        equippedItem.OnDrop(this);
        equippedItem = null;

        // Unlock selection if was two-handed
    }

    [Command]
    void CmdUsePrimary()
    {
        if (equippedItem == null) return;
        equippedItem.PrimaryAction();
        RpcUsePrimary();
    }

    [ClientRpc]
    void RpcUsePrimary() => equippedItem.PrimaryAction();

    [Command]
    void CmdUseSecondary()
    {
        if (equippedItem == null) return;
        equippedItem?.SecondaryAction();
        RpcUseSecondary();
    }

    [ClientRpc]
    void RpcUseSecondary() => equippedItem.SecondaryAction();

    IEnumerator DisplayItemName(string name)
    {
        itemName.SetText(name);

        float fade = 0.5f;
        float timer = 0;

        while (timer < fade)
        {
            nameGroup.alpha = timer / fade;
            timer += Time.deltaTime;
            yield return null;
        }

        timer = fade;
        yield return sleepNameDisplay;

        while (timer > 0)
        {
            nameGroup.alpha = timer / fade;
            timer -= Time.deltaTime;
            yield return null;
        }
    }
}
