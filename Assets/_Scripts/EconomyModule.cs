using Mirror;
using System.Collections.Generic;
using UnityEngine;
using static GameManager;

public class EconomyModule : NetworkBehaviour
{
    [Header("Quota Settings")]
    [SerializeField] int startingQuotaMin = 900;
    [SerializeField] int startingQuotaMax = 1100;
    [SerializeField] int linearIncreaseMin = 400;
    [SerializeField] int linearIncreaseMax = 700;
    [SerializeField] float exponentialRate = 1.15f;
    [SerializeField] float exponentialFactor = 0.3f;

    [SyncVar]
    public int targetQuota;

    public float TotalBalance => teamWhiteBalance + teamRedBalance + teamBlueBalance + teamYellowBalance + teamGreenBalance + teamPinkBalance;

    [SyncVar]
    public float teamWhiteBalance = 0;
    [SyncVar]
    public float teamRedBalance = 0;
    [SyncVar]
    public float teamBlueBalance = 0;
    [SyncVar]
    public float teamYellowBalance = 0;
    [SyncVar]
    public float teamGreenBalance = 0;
    [SyncVar]
    public float teamPinkBalance = 0;

    [SyncVar]
    int startingQuota = 0;

    public bool IsQuotaMet => TotalBalance >= targetQuota;

    [Server]
    public void SetNewQuota()
    {
        if (startingQuota == 0)
        {
            startingQuota = Random.Range(startingQuotaMin, startingQuotaMax);
            targetQuota = startingQuota;
            return;
        }

        targetQuota = GetQuota(Instance.dayCycleModule.currentDay);
    }

    public int GetQuota(int round)
    {
        if (round < 1) round = 1;

        int linearPart = (round - 1) * Random.Range(linearIncreaseMin, linearIncreaseMax);
        float exponentialPart = startingQuota * Mathf.Pow(exponentialRate, round - 1) * exponentialFactor;

        return Mathf.RoundToInt(startingQuota + linearPart + exponentialPart);
    }

    public bool TakeQuotaValue()
    {
        float remainingQuota = targetQuota;
        if (remainingQuota <= 0f)
            return false;

        List<float> balances = new()
        {
            teamWhiteBalance,
            teamRedBalance,
            teamBlueBalance,
            teamYellowBalance,
            teamGreenBalance,
            teamPinkBalance
        };

        int activeTeams = balances.Count;

        float totalBalance = 0f;
        foreach (var b in balances)
            totalBalance += b;

        if (totalBalance <= 0f)
            return false;

        remainingQuota = Mathf.Min(remainingQuota, totalBalance);

        while (remainingQuota > 0f && activeTeams > 0)
        {
            float equalShare = remainingQuota / activeTeams;
            bool anyTeamDepletedThisPass = false;

            for (int i = 0; i < balances.Count; i++)
            {
                if (balances[i] <= 0f)
                    continue;

                if (balances[i] <= equalShare)
                {
                    remainingQuota -= balances[i];
                    balances[i] = 0f;
                    activeTeams--;
                    anyTeamDepletedThisPass = true;
                }
                else
                {
                    balances[i] -= equalShare;
                    remainingQuota -= equalShare;
                }
            }

            if (!anyTeamDepletedThisPass)
                break;
        }

        teamWhiteBalance = balances[0];
        teamRedBalance = balances[1];
        teamBlueBalance = balances[2];
        teamYellowBalance = balances[3];
        teamGreenBalance = balances[4];
        teamPinkBalance = balances[5];

        return remainingQuota <= 0;
    }
}
