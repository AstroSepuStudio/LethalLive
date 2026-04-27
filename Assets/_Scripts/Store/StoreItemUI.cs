using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StoreItemUI : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI itemNameTxt;
    [SerializeField] TextMeshProUGUI itemPriceTxt;
    [SerializeField] TextMeshProUGUI quantityTxt;
    [SerializeField] Button addButton;
    [SerializeField] Button removeButton;

    StoreItemSO storeItem;
    Store store;

    public void Initialize(StoreItemSO item, Store owningStore)
    {
        storeItem = item;
        store = owningStore;

        itemNameTxt.SetText(item.item.itemName);
        itemPriceTxt.SetText("...");
        RefreshQuantity();
    }

    public void RefreshPrice()
    {
        if (store == null || storeItem == null) return;

        int price = store.GetDailyPrice(storeItem);

        itemPriceTxt.SetText($"${price}");
    }

    public void AddOne()
    {
        store.AddToCart(storeItem, 1);
    }

    public void RemoveOne()
    {
        store.RemoveFromCart(storeItem, 1);
    }

    void OnCartChanged()
    {
        RefreshQuantity();
    }

    public void RefreshQuantity()
    {
        int qty = 0;
        foreach (var entry in store.SyncedCart)
        {
            if (store.GetCatalogueItem(entry.catalogueIndex) == storeItem)
            {
                qty = entry.quantity;
                break;
            }
        }

        if (quantityTxt != null)
            quantityTxt.SetText(qty > 0 ? $"x{qty}" : "");

        if (removeButton != null)
            removeButton.interactable = qty > 0;

        if (addButton != null)
            addButton.interactable = store.CanAffordOne(storeItem);
    }
}
