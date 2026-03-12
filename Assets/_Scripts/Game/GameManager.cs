using Mirror;
using Steamworks;
using System;
using System.Collections;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    public struct LobbyMemberData
    {
        public CSteamID SteamID;
        public uint netID;
        public string Name;
        public byte[] AvatarData;
        public PlayerTeam Team;
        public int Ping;
    }

    public static GameManager Instance { get; private set; }

    [Header("References")]
    public LobbyManagerScreen lobbyManagerScreen;
    public GM_DayCycleModule dayMod;
    public GM_EconomyModule ecoMod;
    public GM_PlayerModule playMod;
    public GM_DungeonModule dngMod;

    [SyncVar]
    public bool gameStarted = false;

    [SyncVar]
    public int Seed = 67;

    [Server] public void SetSeed(int seed) => Seed = seed;

    public bool debug = false;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    [Server]
    public void StartGame()
    {
        SteamMatchmaking.SetLobbyType(LobbyManager.Instance.CurrentLobbyID, ELobbyType.k_ELobbyTypePrivate);
        gameStarted = true;

        dayMod.currentDayTime = -1;
        //lobbyManagerScreen.RpcSwitchScreenState();
    }

    [Server]
    public void CheckQuotaCompletion()
    {
        if (ecoMod.IsQuotaMet)
        {
            Debug.Log("Quota Met");
            ecoMod.TakeQuotaValue();
            StartCoroutine(QuotaCompletionSequence());
        }
        else
        {
            StartCoroutine(QuotaNotMetSequence());
        }
    }

    [Server]
    public void ResetGame()
    {
        StartCoroutine(ResetGameSequence());
    }

    IEnumerator QuotaCompletionSequence()
    {
        Debug.Log("Quota completed");

        yield return null;

        dngMod.CloseDungeon();
        dngMod.ResetCooldown();
        playMod.ReviveAllPlayers();
        StopMusic();

        dayMod.currentDay++;
    }

    IEnumerator QuotaNotMetSequence()
    {
        Debug.Log("Quota not completed");
        yield return null;

        playMod.ExecuteAllPlayers();
        dayMod.ResetDays();
        ecoMod.ResetEconomy();
        dngMod.CloseDungeon();
        dngMod.ResetCooldown();

        yield return new WaitForSeconds(6f);

        StopMusic();
        playMod.ReviveAllPlayers();
    }

    IEnumerator ResetGameSequence()
    {
        yield return new WaitForSeconds(5f);

        dayMod.ResetDays();
        ecoMod.ResetEconomy();
        dngMod.CloseDungeon();
        dngMod.ResetCooldown();

        yield return new WaitForSeconds(5f);

        StopMusic();
        playMod.ReviveAllPlayers();
    }

    [ClientRpc]
    private void StopMusic() => AudioManager.Instance.StopMusic();

    public void SetUpNewDay()
    {
        ecoMod.SetNewQuota();
        //OpenDungeon();
    }    
}
