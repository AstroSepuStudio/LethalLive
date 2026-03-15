using System.Collections;
using TMPro;
using UnityEngine;
using LethalLive;
using UnityEngine.Events;

public class TabletManager : MonoBehaviour
{
    [SerializeField] GameObject tabletObj;
    [SerializeField] GameObject rebindOverlay;
    [SerializeField] TextMeshProUGUI rebindText;
    [SerializeField] TextMeshProUGUI timeText;

    public static UnityEvent OnKeyRebindCompletedEvent = new();

    public GameObject RebindOverlay => rebindOverlay;
    public TextMeshProUGUI RebindText => rebindText;

    public bool CanSwitchState => currentActivities == 0;

    int currentActivities = 0;
    public bool IsActive { get; private set; } = false;

    public void AddActivity() => currentActivities++;
    public void RemoveActivity() => currentActivities--;

    bool esc;

    private void OnEnable()
    {
        GameTick.OnSecond += OnSecond;
    }

    private void OnDisable()
    {
        GameTick.OnSecond -= OnSecond;
    }

    private void OnDestroy()
    {
        OnKeyRebindCompletedEvent.RemoveAllListeners();
    }

    private void OnSecond() => timeText.SetText(GameManager.Instance.dayMod.GetFormatedTime());

    public bool TrySwitchState()
    {
        if (!CanSwitchState) return false;
        if (esc) return false;
        esc = true;
        StartCoroutine(WaitForNextFrame());

        IsActive = !IsActive;
        tabletObj.SetActive(IsActive);

        SettingsManager.Instance.SetMouseLockState(!IsActive);
        
        if (IsActive)
            GameManager.Instance.playMod.LocalPlayer.Player_Input.SwitchCurrentActionMap("Tablet");
        else
            GameManager.Instance.playMod.LocalPlayer.Player_Input.SwitchCurrentActionMap("Player");
        return true;
    }

    IEnumerator WaitForNextFrame()
    {
        yield return null;
        esc = false;
    }

    public void OnKeyRebindCompleted() => OnKeyRebindCompletedEvent?.Invoke();
}
