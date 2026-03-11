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

    [SyncVar(hook = nameof(OnSelectedSlotChanged))]
    public int selectedSlotIndex = 0;

    ItemBase[] inventorySlots = new ItemBase[4];
    ItemBase equippedItem;

    WaitForSeconds sleepNameDisplay = new(1f);
    Coroutine nameDisplayCoroutine;

    public bool ItemEquipped => equippedItem != null;
    public bool HasPrimaryAction => equippedItem != null && equippedItem.ItemData.hasPrimaryAction;
    public bool HasSecondaryAction => equippedItem != null && equippedItem.ItemData.hasSecondaryAction;
    public bool HasTwoHandedEquipped => equippedItem != null && equippedItem.ItemData.isTwoHanded;
    public bool IsItemInUse => equippedItem != null && equippedItem.InUse;

    public override void OnStartClient()
    {
        base.OnStartClient();
        hotbarSlots[selectedSlotIndex].SelectSlot();
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
        if (context.started) CmdUsePrimary();
        else if (context.canceled) CmdCancelPrimary();
    }

    public void SecondaryInput(InputAction.CallbackContext context)
    {
        if (context.started) CmdUseSecondary();
        else if (context.canceled) CmdCancelSecondary();
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

    #region Add / Remove Items

    [Server]
    public bool AddItem(ItemBase item)
    {
        if (pData._LockPlayer) return false;
        if (item == null) return false;
        if (IsFull()) return false;

        int index = selectedSlotIndex;
        if (inventorySlots[index] != null)
        {
            index = -1;
            for (int i = 0; i < inventorySlots.Length; i++)
            {
                if (inventorySlots[i] != null) continue;
                index = i;
                break;
            }
            if (index == -1) return false;
        }

        item.HasOwner = true;
        inventorySlots[index] = item;

        bool shouldSelect = equippedItem == null || !equippedItem.InUse;

        if (shouldSelect && index != selectedSlotIndex)
            selectedSlotIndex = index;

        RpcAddItem(item.ID, index, shouldSelect);
        return true;
    }

    [ClientRpc]
    void RpcAddItem(uint itemID, int slotIndex, bool shouldSelect)
    {
        NetworkClient.spawned.TryGetValue(itemID, out NetworkIdentity netIdentity);
        if (netIdentity == null) return;

        var item = netIdentity.gameObject.GetComponent<ItemBase>();
        inventorySlots[slotIndex] = item;
        item.OnPickUp();
        hotbarSlots[slotIndex].SetItem(item);

        if (shouldSelect)
            EquipItemFromSlot(slotIndex);
    }

    public void RemoveCurrentItem()
    {
        if (equippedItem == null) return;
        LocalDropItem(false);
    }

    [Server]
    public void DropEverything()
    {
        foreach (var slot in inventorySlots)
            if (slot != null) slot.HasOwner = false;

        RpcDropAllItems();
    }

    [Server]
    public void DestroyAllItems()
    {
        for (int i = 0; i < inventorySlots.Length; i++)
        {
            if (inventorySlots[i] == null) continue;

            ItemBase item = inventorySlots[i];
            inventorySlots[i] = null;
            NetworkServer.Destroy(item.gameObject);
        }

        if (equippedItem != null)
        {
            NetworkServer.Destroy(equippedItem.gameObject);
            equippedItem = null;
        }

        RpcOnAllItemsDestroyed();
    }

    #endregion

    #region Drop Commands

    [Command]
    void CmdDropItem()
    {
        if (pData._LockPlayer) return;
        if (equippedItem == null || IsItemInUse || !equippedItem.ItemData.droppable) return;

        equippedItem.HasOwner = false;
        inventorySlots[selectedSlotIndex] = null;

        RpcDropItem();
    }

    [ClientRpc]
    void RpcOnAllItemsDestroyed()
    {
        for (int i = 0; i < inventorySlots.Length; i++)
        {
            hotbarSlots[i].RemoveItem();
            inventorySlots[i] = null;
        }

        equippedItem = null;
        SetEquipAnimState(null);
    }

    [ClientRpc] void RpcDropItem() => LocalDropItem();
    [ClientRpc] void RpcDropAllItems() => LocalDropAllItems();

    void LocalDropItem(bool drop = true)
    {
        if (equippedItem == null) return;

        inventorySlots[selectedSlotIndex] = null;
        hotbarSlots[selectedSlotIndex].RemoveItem();

        ItemBase dropping = equippedItem;
        equippedItem = null;
        SetEquipAnimState(null);

        if (drop)
            dropping.OnDrop(this);
    }

    void LocalDropAllItems(bool drop = true)
    {
        for (int i = 0; i < inventorySlots.Length; i++)
        {
            if (inventorySlots[i] == null) continue;

            hotbarSlots[i].RemoveItem();
            if (drop)
                inventorySlots[i].OnDrop(this);
            inventorySlots[i] = null;
        }

        equippedItem = null;
        SetEquipAnimState(null);
    }

    #endregion

    #region Slot Selection

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
                selectedSlotIndex = nextIndex;
                return;
            }
            nextIndex = (nextIndex + 1) % inventorySlots.Length;
        }

        selectedSlotIndex = (selectedSlotIndex + 1) % inventorySlots.Length;
    }

    [Command]
    void CmdSelectSlot(int index)
    {
        if (pData._LockPlayer) return;
        if (HasTwoHandedEquipped || IsItemInUse) return;
        if (index < 0 || index >= inventorySlots.Length) return;

        selectedSlotIndex = index;
    }

    void OnSelectedSlotChanged(int oldIndex, int newIndex)
    {
        if (oldIndex != newIndex)
            hotbarSlots[oldIndex].DeselectSlot();

        hotbarSlots[newIndex].SelectSlot();
        EquipItemFromSlot(newIndex);
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
            SetEquipAnimState(null);
            return;
        }

        equippedItem = item;
        item.OnEquip(this);

        item.transform.SetParent(pData.Skin_Data.GrabPoint);
        item.transform.SetLocalPositionAndRotation(
            item.ItemData.gOffset,
            Quaternion.Euler(item.ItemData.gRotation));

        if (nameDisplayCoroutine != null)
            StopCoroutine(nameDisplayCoroutine);
        nameDisplayCoroutine = StartCoroutine(DisplayItemName(item.ItemData.itemName));

        SetEquipAnimState(item);
    }

    void SetEquipAnimState(ItemBase item)
    {
        bool hasItem = item != null;
        bool twoHanded = hasItem && item.ItemData.isTwoHanded;
        bool isCrowbar = hasItem && item.ItemData.animationType == ItemSO.ItemAnimationType.Crowbar;

        pData.Skin_Data.CharacterAnimator.SetBool("G_TwoH", twoHanded);
        pData.Skin_Data.CharacterAnimator.SetBool("G_Crowbar", !twoHanded && isCrowbar);
        pData.Skin_Data.CharacterAnimator.SetBool("G_OneH", hasItem && !twoHanded && !isCrowbar);
        pData.Skin_Data.CharacterAnimator.SetLayerWeight(2, hasItem ? 1f : 0f);
    }

    #endregion

    #region Item Actions

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
    void RpcUsePrimary()
    {
        if (isServer) return;
        equippedItem?.PrimaryAction();
    }

    [Command]
    void CmdCancelPrimary()
    {
        if (equippedItem == null) return;
        equippedItem.CancelPrimaryAction();
        RpcCancelPrimary();
    }

    [ClientRpc]
    void RpcCancelPrimary()
    {
        if (isServer) return;
        equippedItem?.CancelPrimaryAction();
    }

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
    void RpcUseSecondary()
    {
        if (isServer) return;
        equippedItem?.SecondaryAction();
    }

    [Command]
    void CmdCancelSecondary()
    {
        if (equippedItem == null) return;
        equippedItem.CancelSecondaryAction();
        RpcCancelSecondary();
    }

    [ClientRpc]
    void RpcCancelSecondary()
    {
        if (isServer) return;
        equippedItem?.CancelSecondaryAction();
    }

    #endregion

    #region Animation Callbacks

    [Server] public void PrimaryItemAnimationTrigger() => RpcPrimaryItemAnimationTrigger();
    [Server] public void PrimaryItemAnimationFinishes() => RpcPrimaryItemAnimationFinishes();
    [Server] public void SecondaryItemAnimationTrigger() => RpcSecondaryItemAnimationTrigger();
    [Server] public void SecondaryItemAnimationFinishes() => RpcSecondaryItemAnimationFinishes();

    [ClientRpc] void RpcPrimaryItemAnimationTrigger() => equippedItem?.PrimaryAnimationTrigger();
    [ClientRpc] public void RpcPrimaryItemAnimationFinishes() => equippedItem?.PrimaryAnimationFinish();
    [ClientRpc] public void RpcSecondaryItemAnimationTrigger() => equippedItem?.SecondaryAnimationTrigger();
    [ClientRpc] public void RpcSecondaryItemAnimationFinishes() => equippedItem?.SecondaryAnimationFinish();

    #endregion

    #region UI

    IEnumerator DisplayItemName(string displayName)
    {
        itemName.SetText(displayName);

        const float fade = 0.5f;
        float timer = 0f;

        while (timer < fade)
        {
            nameGroup.alpha = timer / fade;
            timer += Time.deltaTime;
            yield return null;
        }

        nameGroup.alpha = 1f;
        yield return sleepNameDisplay;

        timer = fade;
        while (timer > 0f)
        {
            nameGroup.alpha = timer / fade;
            timer -= Time.deltaTime;
            yield return null;
        }

        nameGroup.alpha = 0f;
        nameDisplayCoroutine = null;
    }

    #endregion
}
