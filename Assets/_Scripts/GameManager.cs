using Mirror;
using Steamworks;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }
    [SerializeField] ItemSO[] itemsData;
    [SerializeField] LobbyManagerScreen lobbyManagerScreen;
    [SerializeField] MapGenerator mapGenerator;
    [SerializeField] Int_Teleport teleporter;
    [SerializeField] List<PlayerData> players = new ();
    [SerializeField] Item_HomewardBeacon homewardBeacon;

    [Header("Day Settings")]
    [SerializeField] float dayDuration = 900;
    [SerializeField] int startingQuotaMin = 900;
    [SerializeField] int startingQuotaMax = 1100;
    [SerializeField] int linearIncreaseMin = 400;
    [SerializeField] int linearIncreaseMax = 700;
    [SerializeField] float exponentialRate = 1.15f;
    [SerializeField] float exponentialFactor = 0.3f;

    private readonly Dictionary<int, int> cachedQuotas = new ();

    public IReadOnlyList<PlayerData> Players => players;

    [HideInInspector] 
    public PlayerData LocalPlayer;

    [SyncVar]
    public int mapSeed;

    [SyncVar]
    public Vector3 startRoomPos;

    [SyncVar]
    public bool dayStarted = false;

    [SyncVar]
    public bool gameStarted = false;

    [SyncVar]
    public float totalBalance = 0;

    [SyncVar]
    public float teamHololiveBalance = 0;
    [SyncVar]
    public float teamHololiveGamers = 0;
    [SyncVar]
    public float teamHoloXBalance = 0;
    [SyncVar]
    public float teamEnglishBalance = 0;

    [SyncVar]
    public int currentDay = 1;

    [SyncVar]
    public float targetQuota;

    [SyncVar]
    public float currentDayTime;

    public struct LobbyMemberData
    {
        public CSteamID SteamID;
        public string Name;
        public byte[] AvatarData;
        public PlayerTeam Team;
        public int Ping;
    }

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
    public void RegisterPlayer(PlayerData player)
    {
        if (!players.Contains(player))
        {
            player.Index = players.Count;
            player.Team = PlayerTeam.Hololive;
            players.Add(player);
        }
    }

    [Server]
    public void UnregisterPlayer(PlayerData player)
    {
        if (players.Contains(player))
        {
            players.Remove(player);
        }
    }

    [Command(requiresAuthority = false)]
    public void CmdSetPlayerPing(int index, int ping) => players[index].Ping = ping;

    [Server]
    public void RequestScreenRefresh()
    {
        List<LobbyMemberData> members = new();

        foreach (var player in players)
        {
            members.Add(new LobbyMemberData
            {
                SteamID = player.SteamID,
                Name = player.PlayerName,
                AvatarData = player.AvatarData,
                Team = player.Team,
                Ping = player.Ping
            });
        }

        RpcRefreshScreen(members.ToArray());
    }

    [ClientRpc]
    void RpcRefreshScreen(LobbyMemberData[] members)
    {
        if (lobbyManagerScreen == null)
            return;

        lobbyManagerScreen.RefreshScreen(members);
    }

    [Server]
    public void CmdRequestOpenLMS(int index) => RpcOpenLobbyManagerScreen(index);

    [ClientRpc]
    void RpcOpenLobbyManagerScreen(int index)
    {
        if (lobbyManagerScreen == null)
            return;

        lobbyManagerScreen.OpenLobbyManagerScreen(index);
    }

    [Command(requiresAuthority = false)]
    public void CmdRequestTeamChange(int playerIndex, PlayerTeam team)
    {
        players[playerIndex].Team = team;
    }

    [Server]
    public void StartGame()
    {
        SteamMatchmaking.SetLobbyType(LobbyManager.Instace.CurrentLobbyID, ELobbyType.k_ELobbyTypePrivate);
        gameStarted = true;

        StartCoroutine(GameCoroutine());
        StartCoroutine(lobbyManagerScreen.SwitchScreenState());
    }

    public void StartDay()
    {
        if (!gameStarted || !isServer) return;

        if (dayStarted)
        {
            FinishDay();
        }
        else
        {
            mapSeed = Random.Range(-1000000, 1000000);
            dayStarted = true;

            targetQuota = GetQuota(currentDay);

            RpcGenerateMap(mapSeed);
        }
    }

    public int GetQuota(int round)
    {
        if (round < 1) round = 1;

        if (cachedQuotas.TryGetValue(round, out int quota))
            return quota;

        int linearPart = (round - 1) * Random.Range(linearIncreaseMin, linearIncreaseMax);
        float startingQuota = Random.Range(startingQuotaMin, startingQuotaMax);
        float exponentialPart = startingQuota * Mathf.Pow(exponentialRate, round - 1) * exponentialFactor;

        return Mathf.RoundToInt(startingQuota + linearPart + exponentialPart);
    }

    [Server]
    IEnumerator GameCoroutine()
    {
        currentDayTime = -1;

        while (!dayStarted)
        {
            yield return null;
        }

        currentDayTime = 0;
        while (currentDayTime < dayDuration)
        {
            currentDayTime += Time.deltaTime;
            yield return null;
        }

        FinishDay();
    }

    [Server]
    public void FinishDay()
    {
        dayStarted = false;
        currentDay++;
        RpcClearMap();
    }

    [ClientRpc]
    void RpcGenerateMap(int seed)
    {
        mapGenerator.StartGeneration(seed);

        if (isServer)
        {
            homewardBeacon.SetPosition(startRoomPos);
            teleporter.SetParent(homewardBeacon.transform);
        }
    }

    [ClientRpc]
    void RpcClearMap()
    {
        mapGenerator.ClearMap();
    }
}
