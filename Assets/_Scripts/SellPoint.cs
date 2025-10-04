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

        GameManager.Instance.totalBalance += totalValue;

        totalValue = 0;

        for (int i = itemsInside.Count - 1; i >= 0; i--)
        {
            if (itemsInside[i].pData != null)
            {
                if (itemsInside[i].pData.Team == PlayerTeam.Hololive)
                    GameManager.Instance.teamHololiveBalance += itemsInside[i].ItemValue;
                else if (itemsInside[i].pData.Team == PlayerTeam.Gamers)
                    GameManager.Instance.teamHololiveGamers += itemsInside[i].ItemValue;
                else if (itemsInside[i].pData.Team == PlayerTeam.HoloX)
                    GameManager.Instance.teamHoloXBalance += itemsInside[i].ItemValue;
                else if (itemsInside[i].pData.Team == PlayerTeam.English)
                    GameManager.Instance.teamEnglishBalance += itemsInside[i].ItemValue;
            }

            NetworkServer.Destroy(itemsInside[i].gameObject);
        }
        
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
