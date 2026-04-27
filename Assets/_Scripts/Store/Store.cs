using System.Collections;
using System.Collections.Generic;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class Store : NetworkBehaviour
{
    [System.Serializable]
    public class CartEntry
    {
        public StoreItemSO storeItem;
        public int quantity;
        public int unitPrice;
        public int TotalPrice => unitPrice * quantity;
    }

    [System.Serializable]
    public struct SyncCartEntry : System.IEquatable<SyncCartEntry>
    {
        public int catalogueIndex;
        public int quantity;
        public int unitPrice;
        public int TotalPrice => unitPrice * quantity;

        public bool Equals(SyncCartEntry other) =>
            catalogueIndex == other.catalogueIndex &&
            quantity == other.quantity &&
            unitPrice == other.unitPrice;
    }

    [Header("References")]
    [SerializeField] StoreItemSO[] catalogue;
    [SerializeField] Transform storeContentParent;
    [SerializeField] GameObject storeItemUIPrefab;
    [SerializeField] TextMeshProUGUI balanceTxt;
    [SerializeField] TextMeshProUGUI costTxt;

    [SerializeField] GameObject checkoutPanel;
    [SerializeField] GameObject notEnough4quota;
    [SerializeField] Button checkoutConfirmBtn;
    [SerializeField] TextMeshProUGUI checkoutCost;
    [SerializeField] TextMeshProUGUI checkoutConfirmBtnTxt;

    [Header("Spawn")]
    [SerializeField] Transform spawnOrigin;
    [SerializeField] float spawnMaxOffset = 1.5f;

    [Header("Events")]
    public UnityEvent OnCartChanged;

    public readonly SyncList<SyncCartEntry> SyncedCart = new();
    public readonly SyncList<int> SyncedPrices = new();

    PlayerData currentPlayer;

    public IReadOnlyList<SyncCartEntry> Cart => SyncedCart;
    public StoreItemSO[] Catalogue => catalogue;

    public int CartTotal
    {
        get
        {
            int total = 0;
            foreach (var entry in SyncedCart) total += entry.TotalPrice;
            return total;
        }
    }

    public bool CanAffordOne(StoreItemSO storeItem)
    {
        if (storeItem == null) return false;
        int price = GetDailyPrice(storeItem);
        return CartTotal + price <= GetCurrentBalance();
    }

    int GetCatalogueIndex(StoreItemSO item)
    {
        for (int i = 0; i < catalogue.Length; i++)
            if (catalogue[i] == item) return i;
        return -1;
    }

    public StoreItemSO GetCatalogueItem(int index) =>
        index >= 0 && index < catalogue.Length ? catalogue[index] : null;

    public int GetDailyPrice(StoreItemSO item)
    {
        if (catalogue == null) return item.minPrice;
        for (int i = 0; i < catalogue.Length; i++)
            if (catalogue[i] == item)
                return i < SyncedPrices.Count ? SyncedPrices[i] : item.minPrice;
        return item.minPrice;
    }

    void Start()
    {
        SyncedPrices.Callback += OnSyncedPricesChanged;
        SyncedCart.Callback += OnSyncedCartChanged;
        PopulateCatalogue();

        if (!isServer) return;

        GameManager.Instance.ecoMod.OnTeamBalanceChangedEv.AddListener(OnBalanceUpdate);
        GameManager.Instance.dayMod.OnDayEnded.AddListener(OnDayEnded);
        RollDailyPrices();
    }

    private void OnBalanceUpdate(PlayerTeam arg0, float arg1) => RpcUpdateUI();

    void OnDestroy()
    {
        SyncedPrices.Callback -= OnSyncedPricesChanged;
        SyncedCart.Callback -= OnSyncedCartChanged;

        if (isServer)
            GameManager.Instance.dayMod.OnDayEnded.RemoveListener(OnDayEnded);
    }

    void OnSyncedPricesChanged(SyncList<int>.Operation op, int index, int oldValue, int newValue)
    {
        RefreshAllPrices();
        RefreshBalanceDisplay();
    }

    void OnSyncedCartChanged(SyncList<SyncCartEntry>.Operation op, int index,
    SyncCartEntry oldItem, SyncCartEntry newItem)
    {
        OnCartChanged?.Invoke();
        RefreshAllCartUI();
        RefreshBalanceDisplay();
    }

    void RollDailyPrices()
    {
        if (catalogue == null) return;

        SyncedPrices.Clear();
        foreach (var item in catalogue)
            SyncedPrices.Add(item != null ? item.RolledPrice : 0);
    }

    void OnDayEnded(int day)
    {
        RollDailyPrices();
        RpcClearCart();
    }

    public void SetCurrentPlayer(PlayerData player)
    {
        currentPlayer = player;
        RefreshBalanceDisplay();
    }

    public void ClearCurrentPlayer()
    {
        currentPlayer = null;
    }

    void RefreshAllPrices()
    {
        foreach (Transform child in storeContentParent)
            if (child.TryGetComponent(out StoreItemUI ui))
                ui.RefreshPrice();
    }

    void PopulateCatalogue()
    {
        if (storeContentParent == null || storeItemUIPrefab == null || catalogue == null) return;

        foreach (Transform child in storeContentParent)
            Destroy(child.gameObject);

        foreach (var item in catalogue)
        {
            if (item == null) continue;
            GameObject go = Instantiate(storeItemUIPrefab, storeContentParent);
            if (go.TryGetComponent(out StoreItemUI ui))
                ui.Initialize(item, this);
        }

        if (SyncedPrices.Count > 0)
            RefreshAllPrices();
    }

    public void AddToCart(StoreItemSO storeItem, int quantity = 1)
    {
        if (storeItem == null || quantity <= 0) return;
        CmdAddToCart(GetCatalogueIndex(storeItem), quantity);
    }

    public void RemoveFromCart(StoreItemSO storeItem, int quantity = 1)
    {
        if (storeItem == null) return;
        CmdRemoveFromCart(GetCatalogueIndex(storeItem), quantity);
    }

    public void RemoveAllFromCart(StoreItemSO storeItem)
    {
        if (storeItem == null) return;
        CmdRemoveAllFromCart(GetCatalogueIndex(storeItem));
    }

    public void ClearCart()
    {
        CmdClearCart();
    }

    [Command(requiresAuthority = false)]
    void CmdAddToCart(int catalogueIndex, int quantity)
    {
        if (catalogueIndex < 0 || catalogueIndex >= catalogue.Length) return;

        int unitPrice = SyncedPrices[catalogueIndex];
        int addedCost = unitPrice * quantity;

        if (CartTotal + addedCost > GetCurrentBalance()) return;

        for (int i = 0; i < SyncedCart.Count; i++)
        {
            if (SyncedCart[i].catalogueIndex != catalogueIndex) continue;
            var updated = SyncedCart[i];
            updated.quantity += quantity;
            SyncedCart[i] = updated;
            return;
        }

        SyncedCart.Add(new SyncCartEntry
        {
            catalogueIndex = catalogueIndex,
            quantity = quantity,
            unitPrice = unitPrice
        });
    }

    [Command(requiresAuthority = false)]
    void CmdRemoveFromCart(int catalogueIndex, int quantity)
    {
        for (int i = 0; i < SyncedCart.Count; i++)
        {
            if (SyncedCart[i].catalogueIndex != catalogueIndex) continue;

            var updated = SyncedCart[i];
            updated.quantity -= quantity;

            if (updated.quantity <= 0)
                SyncedCart.RemoveAt(i);
            else
                SyncedCart[i] = updated;

            return;
        }
    }

    [Command(requiresAuthority = false)]
    void CmdRemoveAllFromCart(int catalogueIndex)
    {
        for (int i = SyncedCart.Count - 1; i >= 0; i--)
            if (SyncedCart[i].catalogueIndex == catalogueIndex)
                SyncedCart.RemoveAt(i);
    }

    [Command(requiresAuthority = false)]
    void CmdClearCart()
    {
        SyncedCart.Clear();
        RpcUpdateUI();
    }

    public void RequestCheckout()
    {
        if (Cart.Count == 0) return;

        float balance = GetCurrentBalance();
        float finalBalance = balance - CartTotal;

        checkoutPanel.SetActive(true);
        checkoutCost.SetText(
            $"${CartTotal}\n" +
            $"${balance} -\n" +
            $"--------\n" +
            $"${finalBalance}");

        if (finalBalance < GameManager.Instance.ecoMod.targetQuota)
        {
            StopAllCoroutines();
            StartCoroutine(CountdownBelowQuota());
        }
        else
        {
            notEnough4quota.SetActive(false);
            checkoutConfirmBtn.interactable = true;
            checkoutConfirmBtnTxt.SetText("CONFIRM");
        }
    }

    public void CloseCheckout()
    {
        checkoutPanel.SetActive(false);
        StopAllCoroutines();
    }

    IEnumerator CountdownBelowQuota()
    {
        notEnough4quota.SetActive(true);
        checkoutConfirmBtn.interactable = false;
        float timer = 3f;
        while (timer > 0f)
        {
            checkoutConfirmBtnTxt.SetText($"CONFIRM ({Mathf.RoundToInt(timer)})");
            timer -= Time.deltaTime;
            yield return null;
        }
        checkoutConfirmBtnTxt.SetText("CONFIRM");
        checkoutConfirmBtn.interactable = true;
    }

    public void ConfirmPurchase()
    {
        if (currentPlayer == null) return;
        CloseCheckout();
        CmdRequestPurchase();
    }

    [Command(requiresAuthority = false)]
    void CmdRequestPurchase() => ServerTryPurchase();

    [Server]
    void ServerTryPurchase()
    {
        if (SyncedCart.Count == 0) return;

        var eco = GameManager.Instance.ecoMod;

        if (LobbySettings.Instance.TeamsShareBalance)
        {
            if (eco.TotalBalance < CartTotal) return;
            eco.TakeBalance(CartTotal);
        }
        else
        {
            if (!eco.CanAfford(currentPlayer.Team, CartTotal)) return;
            eco.Deduct(currentPlayer.Team, CartTotal);
        }

        RpcUpdateUI();
        SpawnCartItems();
        SyncedCart.Clear();
    }

    [ClientRpc]
    void RpcClearCart()
    {
        ClearCart();
    }

    [Server]
    void SpawnCartItems()
    {
        if (spawnOrigin == null) { Debug.LogWarning("[Store] No spawnOrigin assigned."); return; }

        foreach (var entry in SyncedCart)
        {
            var storeItem = GetCatalogueItem(entry.catalogueIndex);
            if (storeItem.item == null) continue;
            for (int i = 0; i < entry.quantity; i++)
                SpawnOne(storeItem.item);
        }
    }

    [Server]
    void SpawnOne(ItemSO itemData)
    {
        Vector2 circle = Random.insideUnitCircle * spawnMaxOffset;
        Vector3 pos = spawnOrigin.position + new Vector3(circle.x, 0f, circle.y);

        GameObject go = Instantiate(itemData.itemPrefab, pos, Quaternion.identity);
        NetworkServer.Spawn(go);

        if (go.TryGetComponent(out ItemBase item))
        {
            item.ItemValue = 0;

            if (itemData.stackable && item is IStackable stackable)
                stackable.SetQuantity(1);
        }
    }

    [ClientRpc]
    void RpcUpdateUI()
    {
        RefreshAllCartUI();
        RefreshBalanceDisplay();
    }

    float GetCurrentBalance()
    {
        var eco = GameManager.Instance.ecoMod;
        if (LobbySettings.Instance.TeamsShareBalance)
            return eco.TotalBalance;

        if (currentPlayer != null)
            return eco.GetTeamBalance(currentPlayer.Team);

        return 0f;
    }

    void RefreshBalanceDisplay()
    {
        if (balanceTxt != null)
            balanceTxt.SetText($"Balance:\n${GetCurrentBalance()}");

        if (costTxt != null)
            costTxt.SetText("Cost:\n" + (CartTotal > 0 ? $"-${CartTotal}" : "$0"));
    }

    void RefreshAllCartUI()
    {
        foreach (Transform child in storeContentParent)
            if (child.TryGetComponent(out StoreItemUI ui))
                ui.RefreshQuantity();
    }
}
