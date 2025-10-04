using Mirror;
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
    public bool IsItemInUse => equippedItem != null && equippedItem.InUse;

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
    public void PrimaryInput(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            CmdUsePrimary();
        }
        else if (context.canceled)
        {
            CmdCancelPrimary();
        }
    }

    public void SecondaryInput(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            CmdUseSecondary();
        }
        else if (context.canceled)
        {
            CmdCancelSecondary();
        }
    }

    public void OnClientDrop(InputAction.CallbackContext context)
    {
        if (!pData.isLocalPlayer) return;
        if (!context.started) return;

        CmdDropItem();
    }

    public void SelectNextSlot(InputAction.CallbackContext context)
    {
        if (!context.started) return;

        CmdSelectNextSlot();
    }

    public void SelectSlot(InputAction.CallbackContext context, int index)
    {
        if (!context.started) return;

        CmdSelectSlot(index);
    }

    public void SelectSlot_00(InputAction.CallbackContext context) => SelectSlot(context, 0);
    public void SelectSlot_01(InputAction.CallbackContext context) => SelectSlot(context, 1);
    public void SelectSlot_02(InputAction.CallbackContext context) => SelectSlot(context, 2);
    public void SelectSlot_03(InputAction.CallbackContext context) => SelectSlot(context, 3);
    public void SelectSlot_04(InputAction.CallbackContext context) => SelectSlot(context, 4);
    public void SelectSlot_05(InputAction.CallbackContext context) => SelectSlot(context, 5);
    public void SelectSlot_06(InputAction.CallbackContext context) => SelectSlot(context, 6);
    public void SelectSlot_07(InputAction.CallbackContext context) => SelectSlot(context, 7);
    public void SelectSlot_08(InputAction.CallbackContext context) => SelectSlot(context, 8);
    public void SelectSlot_09(InputAction.CallbackContext context) => SelectSlot(context, 9);
    #endregion

    [Server]
    public bool AddItem(ItemBase item)
    {
        if (pData._LockPlayer) return false;
        if (item == null) return false;
        if (IsFull()) return false;

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

        //inventorySlots[index] = item;
        //item.OnPickUp();
        //hotbarSlots[index].SetItem(item);

        if (equippedItem != null && equippedItem.InUse)
        {
            RpcAddItem(item.ID, index, false);
            return true;
        }

        //LocalSelectSlot(index);
        RpcAddItem(item.ID, index, true);
        return true;
    }

    [ClientRpc]
    void RpcAddItem(uint itemID, int slotIndex, bool select)
    {
        NetworkClient.spawned.TryGetValue(itemID, out NetworkIdentity identity);

        if (identity == null) return;
        inventorySlots[slotIndex] = identity.gameObject.GetComponent<ItemBase>();
        inventorySlots[slotIndex].OnPickUp();
        hotbarSlots[slotIndex].SetItem(inventorySlots[slotIndex]);

        if (select)
            LocalSelectSlot(slotIndex);
    }

    [Command]
    void CmdSelectNextSlot()
    {
        if (pData._LockPlayer) return;
        if (HasTwoHandedEquipped || IsItemInUse) return;

        int startIndex = selectedSlotIndex;
        int nextIndex = (selectedSlotIndex + 1) % inventorySlots.Length;

        while (nextIndex != startIndex)
        {
            if (inventorySlots[nextIndex] != null)
            {
                //selectedSlotIndex = nextIndex;
                RpcSelectSlot(nextIndex);
                return;
            }

            nextIndex = (nextIndex + 1) % inventorySlots.Length;
        }

        nextIndex = (selectedSlotIndex + 1) % inventorySlots.Length;

        //LocalSelectSlot(selectedSlotIndex);
        RpcSelectSlot(nextIndex);
    }

    [Command]
    void CmdSelectSlot(int index)
    {
        if (pData._LockPlayer) return;
        if (HasTwoHandedEquipped || IsItemInUse) return;
        if (index >= inventorySlots.Length) return;

        //LocalSelectSlot(index);
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
            pData.Skin_Data.CharacterAnimator.SetBool("G_OneH", false);
            pData.Skin_Data.CharacterAnimator.SetBool("G_Crowbar", false);
            pData.Skin_Data.CharacterAnimator.SetBool("G_TwoH", false);

            pData.Skin_Data.CharacterAnimator.SetLayerWeight(2, 0);
            return;
        }

        equippedItem = item;
        item.OnEquip(this);

        item.transform.SetParent(pData.Skin_Data.GrabPoint);
        item.transform.SetLocalPositionAndRotation(item.ItemData.gOffset, Quaternion.Euler(item.ItemData.gRotation));
        StartCoroutine(DisplayItemName(item.ItemData.itemName));

        if (item.ItemData.isTwoHanded)
        {
            pData.Skin_Data.CharacterAnimator.SetBool("G_OneH", false);
            pData.Skin_Data.CharacterAnimator.SetBool("G_Crowbar", false);
            pData.Skin_Data.CharacterAnimator.SetBool("G_TwoH", true);
        }
        else
        {
            switch (item.ItemData.itemName.ToLower())
            {
                case "crowbar":
                    pData.Skin_Data.CharacterAnimator.SetBool("G_TwoH", false);
                    pData.Skin_Data.CharacterAnimator.SetBool("G_OneH", false);
                    pData.Skin_Data.CharacterAnimator.SetBool("G_Crowbar", true); break;

                default:
                    pData.Skin_Data.CharacterAnimator.SetBool("G_TwoH", false);
                    pData.Skin_Data.CharacterAnimator.SetBool("G_Crowbar", false);
                    pData.Skin_Data.CharacterAnimator.SetBool("G_OneH", true); break;
            }
        }

        pData.Skin_Data.CharacterAnimator.SetLayerWeight(2, 1);
    }

    public void RemoveCurrentItem()
    {
        if (equippedItem == null) return;

        LocalDropItem(false);
    }

    [Command]
    void CmdDropItem()
    {
        if (pData._LockPlayer) return;
        if (equippedItem == null || IsItemInUse) return;

        //LocalDropItem();
        RpcDropItem();
    }

    [ClientRpc]
    void RpcDropItem() => LocalDropItem();

    void LocalDropItem(bool drop = true)
    {
        for (int i = 0; i < inventorySlots.Length; i++)
        {
            if (inventorySlots[i] != equippedItem) continue;

            inventorySlots[i] = null;
            hotbarSlots[i].RemoveItem();
            break;
        }

        if (!equippedItem.ItemData.droppable) return;

        if (equippedItem.ItemData.isTwoHanded)
        {
            pData.Skin_Data.CharacterAnimator.SetBool("G_TwoH", false);
        }
        else
        {
            switch (equippedItem.ItemData.itemName.ToLower())
            {
                case "crowbar": pData.Skin_Data.CharacterAnimator.SetBool("G_Crowbar", false); break;

                default: pData.Skin_Data.CharacterAnimator.SetBool("G_OneH", false); break;
            }
        }

        pData.Skin_Data.CharacterAnimator.SetLayerWeight(2, 0);

        if (drop) equippedItem.OnDrop(this);
        equippedItem = null;
    }

    [Command]
    void CmdUsePrimary()
    {
        if (equippedItem == null || IsItemInUse || pData._LockPlayer) return;
        if (equippedItem.primaryAtkStats.OnCooldown) return;

        if (equippedItem.primaryAtkStats.AttackCooldown > 0)
            StartCoroutine(equippedItem.primaryAtkStats.CountdownCooldown());

        equippedItem.PrimaryAction();
        RpcUsePrimary();
    }

    [ClientRpc]
    void RpcUsePrimary() => equippedItem.PrimaryAction();

    [Command]
    void CmdCancelPrimary()
    {
        if (equippedItem == null) return;

        equippedItem.CancelPrimaryAction();
        RpcCancelPrimary();
    }

    [ClientRpc]
    void RpcCancelPrimary() => equippedItem.CancelPrimaryAction();

    [Command]
    void CmdUseSecondary()
    {
        if (equippedItem == null || IsItemInUse || pData._LockPlayer) return;
        if (equippedItem.secondaryAtkStats.OnCooldown) return;

        if (equippedItem.secondaryAtkStats.AttackCooldown > 0)
            StartCoroutine(equippedItem.secondaryAtkStats.CountdownCooldown());

        equippedItem.SecondaryAction();
        RpcUseSecondary();
    }

    [ClientRpc]
    void RpcUseSecondary() => equippedItem.SecondaryAction();

    [Command]
    void CmdCancelSecondary()
    {
        if (equippedItem == null) return;

        equippedItem.CancelPrimaryAction();
        RpcCancelDecondary();
    }

    [ClientRpc]
    void RpcCancelDecondary() => equippedItem.CancelPrimaryAction();

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

    [Server]
    public void PrimaryItemAnimationTrigger() => RpcPrimaryItemAnimationTrigger();

    [ClientRpc]
    void RpcPrimaryItemAnimationTrigger() => equippedItem.PrimaryAnimationTrigger();

    [Server]
    public void PrimaryItemAnimationFinishes() => RpcPrimaryItemAnimationFinishes();

    [ClientRpc]
    public void RpcPrimaryItemAnimationFinishes() => equippedItem.PrimaryAnimationFinish();

    [Server]
    public void SecondaryItemAnimationTrigger() => RpcSecondaryItemAnimationTrigger();

    [ClientRpc]
    public void RpcSecondaryItemAnimationTrigger() => equippedItem.SecondaryAnimationTrigger();

    [Server]
    public void SecondaryItemAnimationFinishes() => RpcSecondaryItemAnimationFinishes();

    [ClientRpc]
    public void RpcSecondaryItemAnimationFinishes() => equippedItem.SecondaryAnimationFinish();
}
