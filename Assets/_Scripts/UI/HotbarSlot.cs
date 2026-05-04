using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HotbarSlot : MonoBehaviour
{
    [SerializeField] GameObject handsFullGO;
    [SerializeField] Image hotbarSlotImg;
    [SerializeField] Sprite selectedSlot;
    [SerializeField] Sprite deselectedSlot;
    [SerializeField] Color selectedSlotColor = Color.white;
    [SerializeField] Color deselectedSlotColor = Color.orange;
    [SerializeField] Color lockedSlotColor = Color.gray;
    [SerializeField] Image iconImg;
    [SerializeField] TextMeshProUGUI valueTxt;

    ItemBase slotItem;
    bool selected = false;

    public void SetItem(ItemBase item)
    {
        if (item == null || item.ItemData == null)
        {
            RemoveItem();
            return;
        }

        slotItem = item;
        iconImg.sprite = slotItem.ItemData.icon;
        iconImg.enabled = iconImg.sprite != null;
        valueTxt.enabled = true;
        valueTxt.SetText($"${item.ItemValue}");
    }

    public void RemoveItem()
    {
        slotItem = null;
        iconImg.enabled = false;
        iconImg.sprite = null;
        valueTxt.enabled = false;
    }

    public void SelectSlot()
    {
        selected = true;
        hotbarSlotImg.sprite = selectedSlot;
        hotbarSlotImg.color = selectedSlotColor;
    }

    public void DeselectSlot()
    {
        selected = false;
        hotbarSlotImg.sprite = deselectedSlot;
        hotbarSlotImg.color = deselectedSlotColor;
    }

    public void LockSlot()
    {
        hotbarSlotImg.color = lockedSlotColor;
        handsFullGO.SetActive(true);
    }

    public void UnlockSlot()
    {
        hotbarSlotImg.color = selected ? selectedSlotColor : deselectedSlotColor;
        handsFullGO.SetActive(false);
    }
}
