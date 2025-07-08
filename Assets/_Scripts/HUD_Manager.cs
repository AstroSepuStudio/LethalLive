using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUD_Manager : MonoBehaviour
{
    [SerializeField] PlayerData pData;

    [SerializeField] TextMeshProUGUI healthValueTxt;
    [SerializeField] Image healthMeter;

    [SerializeField] TextMeshProUGUI staminaValueTxt;
    [SerializeField] Image staminaMeter;

    [SerializeField] TextMeshProUGUI knockValueTxt;
    [SerializeField] Image knockMeter;

    public void UpdateHUD()
    {
        healthValueTxt.SetText($"{Mathf.Round(pData.Player_Stats.currentHP)}/{Mathf.Round(pData.Player_Stats.maxHP)}");
        staminaValueTxt.SetText($"{Mathf.Round(pData.Player_Stats.currentStamina)}/{Mathf.Round(pData.Player_Stats.maxStamina)}");
        knockValueTxt.SetText($"{Mathf.Round(pData.Player_Stats.currentKnock)}/{Mathf.Round(pData.Player_Stats.maxKnock)}");
    }
}
