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

    [SyncVar(hook = nameof(OnQuotaChanged))]
    public int targetQuota;

    public bool IsQuotaMet => TotalBalance >= targetQuota;

    public UnityEvent<PlayerTeam, float> OnTeamBalanceChangedEv;
    public UnityEvent<int> OnQuotaChangedEv;

    int stagedNextQuota = 0;

    private void Start()
    {
        teamsBalance.OnChange += OnTeamBalanceChanged;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        ResetEconomy();
    }

    [Server]
    public int ComputeAndStageNextQuota(int day)
    {
        int linear = (day - 1) * Random.Range(linearIncreaseMin, linearIncreaseMax);
        float exponential = startingQuota * Mathf.Pow(exponentialRate, day - 1) * exponentialFactor;
        int value = Mathf.RoundToInt(startingQuota + linear + exponential);
        stagedNextQuota = value;
        return value;
    }

    [Server]
    public void ApplyStagedQuota()
    {
        if (stagedNextQuota > 0)
            targetQuota = stagedNextQuota;
        stagedNextQuota = 0;
    }

    [Server]
    public void SetNewQuota()
    {
        if (startingQuota == 0)
        {
            startingQuota = Random.Range(startingQuotaMin, startingQuotaMax);
            targetQuota = startingQuota;
            Debug.Log("Aplying starting quota");
            return;
        }

        Debug.Log("Aplying staged quota");
        ApplyStagedQuota();
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

        while (remainingQuota > 0 && validTeams > 0)
        {
            float evenTake = remainingQuota / validTeams;

            var keys = validBalances.Keys.ToList();
            List<PlayerTeam> toRemove = new();

            foreach (var key in keys)
            {
                float value = validBalances[key];

                if (value >= evenTake)
                {
                    validBalances[key] -= evenTake;
                    teamsBalance[key] -= evenTake;
                    remainingQuota -= evenTake;
                }
                else
                {
                    remainingQuota -= value;
                    validBalances[key] = 0;
                    teamsBalance[key] = 0;

                    toRemove.Add(key);
                }
            }

            foreach (var key in toRemove)
            {
                validBalances.Remove(key);
            }

            validTeams = validBalances.Count;
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
        stagedNextQuota = 0;
        SetNewQuota();
    }

    private void OnTeamBalanceChanged(SyncDictionary<PlayerTeam, float>.Operation op,
                                  PlayerTeam key,
                                  float item)
    {
        OnTeamBalanceChangedEv?.Invoke(key, item);
    }

    private void OnQuotaChanged(int newValue, int oldValue)
    {
        OnQuotaChangedEv?.Invoke(newValue);
    }
}
