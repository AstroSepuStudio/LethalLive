using UnityEngine;
using UnityEngine.UI;

public class HotbarSlot : MonoBehaviour
{
    [SerializeField] Image hotbarSlotImg;
    [SerializeField] Sprite selectedSlot;
    [SerializeField] Sprite deselectedSlot;
    [SerializeField] Image iconImg;

    ItemBase slotItem;

    public void SetItem(ItemBase item)
    {
        if (item == null || item.ItemData == null)
        {
            Debug.LogWarning("[HotbarSlot] SetItem called with null item or missing ItemData.");
            RemoveItem();
            return;
        }

        slotItem = item;
        iconImg.sprite = slotItem.ItemData.icon;
        iconImg.enabled = iconImg.sprite != null;
    }

    public void RemoveItem()
    {
        slotItem = null;
        iconImg.enabled = false;
        iconImg.sprite = null;
        Debug.Log("[HotbarSlot] Removed item from slot.", gameObject);
    }

    public void SelectSlot()
    {
        hotbarSlotImg.sprite = selectedSlot;
    }

    public void DeselectSlot()
    {
        hotbarSlotImg.sprite = deselectedSlot;
    }
}
