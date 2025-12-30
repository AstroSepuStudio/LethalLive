using TMPro;
using UnityEngine;

public class TotalBalanceDisplayer : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI totalBalanceDisplayerTxt;

    private void Start()
    {
        GameTick.OnTick += OnTick;
    }

    void OnTick()
    {
        if (Mathf.Approximately(GameManager.Instance.economyModule.TotalBalance, 0))
        {
            totalBalanceDisplayerTxt.enabled = false;
            return;
        }

        totalBalanceDisplayerTxt.enabled = true;
        totalBalanceDisplayerTxt.SetText($"Total Balance\n${GameManager.Instance.economyModule.TotalBalance}");
    }
}
