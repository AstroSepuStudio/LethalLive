using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUD_Manager : MonoBehaviour
{
    [SerializeField] List<GameObject> OpenWindows = new();

    [SerializeField] PlayerData pData;

    [SerializeField] TextMeshProUGUI healthValueTxt;
    [SerializeField] Image healthMeter;

    [SerializeField] TextMeshProUGUI staminaValueTxt;
    [SerializeField] Image staminaMeter;

    [SerializeField] TextMeshProUGUI knockValueTxt;
    [SerializeField] Image knockMeter;

    private void Awake()
    {
        OpenWindows.Clear();
    }

    public int OpenedWindows => OpenWindows.Count;
    public bool OpenedWindow => OpenedWindows > 0;

    public void OpenWindow(GameObject window)
    {
        OpenWindows.Add(window);
        SettingsManager.Instance.UnlockMouse();
    }

    public void CloseWindow(GameObject window)
    {
        if (OpenWindows.Contains(window))
            OpenWindows.Remove(window);

        if (OpenWindows.Count <= 0)
            SettingsManager.Instance.LockMouse();
    }

    public void UpdateHUD()
    {
        healthValueTxt.SetText($"{Mathf.Round(pData.Player_Stats.currentHP)}/{Mathf.Round(pData.Player_Stats.maxHP)}");
        staminaValueTxt.SetText($"{Mathf.Round(pData.Player_Stats.currentStamina)}/{Mathf.Round(pData.Player_Stats.maxStamina)}");
        knockValueTxt.SetText($"{Mathf.Round(pData.Player_Stats.currentKnock)}/{Mathf.Round(pData.Player_Stats.maxKnock)}");
    }
}
