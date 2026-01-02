using Mirror;
using Steamworks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.ProBuilder.Shapes;

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
    [field: SerializeField] public ThemeDataSO[] ThemeDatas { get; private set; }
    public LobbyManagerScreen lobbyManagerScreen;
    public DayCycleModule dayCycleModule;
    public EconomyModule economyModule;
    public Transform[] spawnPoints;

    [SerializeField] ItemSO[] itemsData;
    [SerializeField] MapGenerator mapGenerator;
    [SerializeField] Int_Teleport teleporter;
    [SerializeField] List<PlayerData> players = new ();
    [SerializeField] List<uint> deadPlayers = new ();
    [SerializeField] Int_HomewardBeacon homewardBeacon;

    /* --- EVENTS --- */
    public UnityEvent OnDungeonOpens = new();
    public UnityEvent OnDungeonCloses = new();

    [HideInInspector] 
    public PlayerData LocalPlayer;

    [SyncVar]
    public Vector3 startRoomPos;

    /* --- GAME STATE --- */
    public IReadOnlyList<PlayerData> Players => players;
    public List<PlayerData> playersOnDungeon = new();

    [SyncVar]
    public int selectedTheme = 0;
    [SyncVar]
    public int mapSeed;

    /* --- FLAGS --- */
    [SyncVar]
    public bool gameStarted = false;
    [SyncVar]
    public bool dungeonOpen = false;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public PlayerData GetPlayerByIndex(int index)
    {
        foreach (PlayerData p in Instance.Players)
        {
            if (p.Index == index)
                 return p;
        }
        return null;
    }

    [Server]
    public void RegisterPlayer(PlayerData player)
    {
        if (!players.Contains(player))
        {
            player.Index = players.Count;
            player.Team = PlayerTeam.White;
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
    public void CmdRequestTeamChange(int playerIndex, PlayerTeam team)
    {
        players[playerIndex].Team = team;
    }

    [Server]
    public void StartGame()
    {
        SteamMatchmaking.SetLobbyType(LobbyManager.Instace.CurrentLobbyID, ELobbyType.k_ELobbyTypePrivate);
        gameStarted = true;

        dayCycleModule.currentDayTime = -1;
        lobbyManagerScreen.RpcSwitchScreenState();
    }

    [Server]
    public void OnEnterDungeon(PlayerData playerData)
    {
        if (ThemeDatas[selectedTheme].loopingMusic != null)
            AudioManager.Instance.PlayMusic(ThemeDatas[selectedTheme].loopingMusic);

        playersOnDungeon.Add(playerData);
    }

    [Server]
    public void OnReturnOffice(PlayerData playerData)
    {
        AudioManager.Instance.StopMusic();

        playersOnDungeon.Remove(playerData);
    }

    [Server]
    public void CheckQuotaCompletion()
    {
        if (economyModule.IsQuotaMet)
        {
            Debug.Log("Quota Met");
            economyModule.TakeQuotaValue();
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

        foreach (var player in players)
        {
            if (!deadPlayers.Contains(player.netId)) continue;

            Transform spawn = spawnPoints.Length > 0
            ? spawnPoints[player.Index % spawnPoints.Length]
            : null;

            Vector3 spawnPos = spawn ? spawn.position : transform.position;

            player.RevivePlayer(spawnPos);
            deadPlayers.Remove(player.netId);
        }

        dayCycleModule.currentDay++;
    }

    IEnumerator QuotaNotMetSequence()
    {
        Debug.Log("Quota not completed");
        yield return null;
    }

    [Server]
    public void OpenDungeon()
    {
        OnDungeonOpens?.Invoke();
        dungeonOpen = true;

        RpcGenerateMap(mapSeed, selectedTheme);
    }

    [Server]
    public void CloseDungeon()
    {
        OnDungeonCloses?.Invoke();
        dungeonOpen = false;

        RpcClearMap();
    }

    [Server]
    public void TryOpenNewDungeon()
    {
        if (!gameStarted) return;

        if (!dayCycleModule.dayStarted)
        {
            dayCycleModule.StartDay();
            OpenDungeon();
            return;
        }

        if (playersOnDungeon.Count > 0)
        {
            Debug.Log("There is players in the dungeon");
            return;
        }

        CloseDungeon();
        OpenDungeon();
    }

    public void SetUpNewDay()
    {
        mapSeed = Random.Range(-1000000, 1000000);
        economyModule.SetNewQuota();
        //OpenDungeon();
    }

    [ClientRpc]
    void RpcGenerateMap(int seed, int theme)
    {
        mapGenerator.StartGeneration(seed, theme);

        if (isServer)
        {
            homewardBeacon.transform.position = startRoomPos;
            teleporter.SetParent(homewardBeacon.transform);
        }
    }

    [ClientRpc]
    void RpcClearMap()
    {
        mapGenerator.ClearMap();
    }

    public void RequestTheme(int index) => CmdSetThemeIndex(index);

    [Command(requiresAuthority = false)]
    void CmdSetThemeIndex(int index)
    {
        if (index >= ThemeDatas.Length || index < 0)
        {
            Debug.LogWarning("Given theme index is invalid");
            return;
        }

        selectedTheme = index;
    }

    public void PlayerDies(uint index)
        { if (!deadPlayers.Contains(index)) deadPlayers.Add(index); }
}
