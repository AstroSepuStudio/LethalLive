using Mirror;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using static GameManager;

public class DayCycleModule : NetworkBehaviour
{
    [SerializeField] float dayDuration = 900;

    [SyncVar]
    public int currentDay = 0;
    [SyncVar]
    public float currentDayTime;
    [SyncVar]
    public bool dayStarted = false;

    public UnityEvent OnDayStarted = new();
    public UnityEvent OnDayEnded = new();

    [Server]
    public void StartDay()
    {
        if (!Instance.gameStarted || !isServer) return;

        dayStarted = true;
        Instance.SetUpNewDay();
        OnDayStarted?.Invoke();

        StartCoroutine(DayTimer());
    }

    [Server]
    IEnumerator DayTimer()
    {
        currentDayTime = 0;
        while (currentDayTime < dayDuration)
        {
            if (!dayStarted) yield break;

            currentDayTime += Time.deltaTime;
            yield return null;
        }

        FinishDay();
    }

    [Server]
    public void FinishDay()
    {
        OnDayEnded?.Invoke();

        foreach (var player in Instance.playersOnDungeon)
        {
            player.Player_Stats.ForceDeath();
        }

        Instance.playersOnDungeon.Clear();

        dayStarted = false;
        Instance.CloseDungeon();
    }
}
