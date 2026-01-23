using Mirror;
using Steamworks;
using System.Collections;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    public struct LobbyMemberData
    {
        public CSteamID SteamID;
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
        SteamMatchmaking.SetLobbyType(LobbyManager.Instace.CurrentLobbyID, ELobbyType.k_ELobbyTypePrivate);
        gameStarted = true;

        dayMod.currentDayTime = -1;
        lobbyManagerScreen.RpcSwitchScreenState();
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

    IEnumerator QuotaCompletionSequence()
    {
        Debug.Log("Quota completed");

        yield return null;

        playMod.ReviveAllPlayers();

        dayMod.currentDay++;
    }

    IEnumerator QuotaNotMetSequence()
    {
        Debug.Log("Quota not completed");
        yield return null;

        playMod.ExecuteAllPlayers();
        dayMod.ResetDays();
        ecoMod.ResetEconomy();

        yield return new WaitForSeconds(3f);

        playMod.ReviveAllPlayers();
    }

    public void SetUpNewDay()
    {
        dngMod.mapSeed = Random.Range(-1000000, 1000000);
        ecoMod.SetNewQuota();
        //OpenDungeon();
    }
}
