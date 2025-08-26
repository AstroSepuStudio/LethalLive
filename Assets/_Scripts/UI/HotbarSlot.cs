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
        slotItem = item;
        iconImg.enabled = true;
        iconImg.sprite = slotItem.ItemData.icon;
    }

    public void RemoveItem()
    {
        slotItem = null;
        iconImg.enabled = false;
        iconImg.sprite = null;
    }

    public void SelectItem()
    {

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
