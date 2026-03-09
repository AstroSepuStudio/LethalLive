using Mirror;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SellPoint : NetworkBehaviour
{
    [SerializeField] TextMeshProUGUI totalValueTxt;

    readonly List<ItemBase> itemsInside = new();

    [SyncVar(hook = nameof(RefreshValueTxt))]
    float totalValue;

    [Server]
    public void SellItems()
    {
        if (totalValue <= 0) return;

        Debug.Log($"Selling {itemsInside.Count} items, totalValue: {totalValue}");

        for (int i = itemsInside.Count - 1; i >= 0; i--)
        {
            Debug.Log($"Item: {itemsInside[i].name}, pData: {itemsInside[i].lastPlayer?.PlayerName ?? "NULL"}, value: {itemsInside[i].ItemValue}");

            if (itemsInside[i].lastPlayer != null)
            {
                PlayerTeam team = itemsInside[i].lastPlayer.Team;
                float current = GameManager.Instance.ecoMod.teamsBalance[team];
                float newBalance = current + itemsInside[i].ItemValue;
                Debug.Log($"Team {team}: {current} -> {newBalance}");

                GameManager.Instance.ecoMod.teamsBalance[team] = newBalance;    
            }

            NetworkServer.Destroy(itemsInside[i].gameObject);
        }
        
        totalValue = 0;
        itemsInside.Clear();
    }

    void RefreshValueTxt(float oldValue, float newValue)
    {
        totalValueTxt.SetText($"${newValue}");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isServer) return;

        if (other.TryGetComponent(out ItemBase item))
        {
            if (!itemsInside.Contains(item))
            {
                itemsInside.Add(item);
                totalValue += item.ItemValue;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!isServer) return;

        if (other.TryGetComponent(out ItemBase item))
        {
            if (itemsInside.Contains(item))
            {
                itemsInside.Remove(item);
                totalValue -= item.ItemValue;
            }
        }
    }
}
