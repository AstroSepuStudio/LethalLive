using Mirror;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public class GM_EconomyModule : NetworkBehaviour
{
    [Header("Quota Settings")]
    [SerializeField] int startingQuotaMin = 900;
    [SerializeField] int startingQuotaMax = 1100;
    [SerializeField] int linearIncreaseMin = 400;
    [SerializeField] int linearIncreaseMax = 700;
    [SerializeField] float exponentialRate = 1.15f;
    [SerializeField] float exponentialFactor = 0.3f;

    public readonly SyncDictionary<PlayerTeam, float> teamsBalance = new()
    {
        { PlayerTeam.White, 0 }, { PlayerTeam.Red, 0 }, { PlayerTeam.Blue, 0 },
        { PlayerTeam.Yellow, 0 }, { PlayerTeam.Green, 0 }, { PlayerTeam.Pink, 0 }
    };

    public float TotalBalance => 
        teamsBalance[PlayerTeam.White] + teamsBalance[PlayerTeam.Red] + teamsBalance[PlayerTeam.Blue] +
        teamsBalance[PlayerTeam.Yellow] + teamsBalance[PlayerTeam.Green] + teamsBalance[PlayerTeam.Pink];

    [SyncVar]
    int startingQuota = 0;

    [SyncVar]
    public int targetQuota;

    public bool IsQuotaMet => TotalBalance >= targetQuota;

    public UnityEvent<PlayerTeam, float> OnTeamBalanceChangedEv;

    private void Start()
    {
        teamsBalance.OnChange += OnTeamBalanceChanged;
    }

    [Server]
    public void SetNewQuota()
    {
        if (startingQuota == 0)
        {
            startingQuota = Random.Range(startingQuotaMin, startingQuotaMax);
            targetQuota = startingQuota;
            return;
        }

        targetQuota = GetQuota(GameManager.Instance.dayMod.currentDay);
    }

    public int GetQuota(int round)
    {
        if (round < 1) round = 1;

        int linearPart = (round - 1) * Random.Range(linearIncreaseMin, linearIncreaseMax);
        float exponentialPart = startingQuota * Mathf.Pow(exponentialRate, round - 1) * exponentialFactor;

        return Mathf.RoundToInt(startingQuota + linearPart + exponentialPart);
    }

    [Server]
    public void TakeQuotaValue()
    {
        if (!IsQuotaMet) return;

        Dictionary<PlayerTeam, float> validBalances = new();
        int validTeams = 0;

        foreach (var pair in teamsBalance)
            if (pair.Value > 0)
            {
                validBalances.Add(pair.Key, pair.Value);
                validTeams++;
            }

        float remainingQuota = targetQuota;

        while (remainingQuota > 0)
        {
            float evenTake = remainingQuota / validTeams;
            Dictionary<PlayerTeam, float> teamsToRemove = new();

            foreach (var pair in validBalances)
            {
                if (pair.Value >= evenTake)
                {
                    validBalances[pair.Key] -= evenTake;
                    teamsBalance[pair.Key] -= evenTake;
                    remainingQuota -= evenTake;
                }
                else
                {
                    remainingQuota -= validBalances[pair.Key];
                    validBalances[pair.Key] = 0;
                    teamsBalance[pair.Key] = 0;

                    validTeams--;
                    teamsToRemove.Add(pair.Key, pair.Value);
                }
            }

            foreach (var pair in teamsToRemove)
            {
                validBalances.Remove(pair.Key);
            }
        }
    }

    [Server]
    public void ResetEconomy()
    {
        var keys = teamsBalance.Keys.ToList();
        foreach (var key in keys)
        {
            teamsBalance[key] = 0;
        }

        startingQuota = 0;
        SetNewQuota();
    }

    private void OnTeamBalanceChanged(SyncDictionary<PlayerTeam, float>.Operation op,
                                  PlayerTeam key,
                                  float item)
    {
        OnTeamBalanceChangedEv?.Invoke(key, item);
    }
}
