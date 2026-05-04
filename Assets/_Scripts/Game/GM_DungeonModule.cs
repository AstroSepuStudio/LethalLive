using Mirror;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using static GameManager;

public class GM_DungeonModule : NetworkBehaviour
{
    [field: SerializeField] public ThemeDataSO[] ThemeDatas { get; private set; }

    [SerializeField] DungeonGenerator mapGenerator;
    [SerializeField] Int_Teleport teleporter;
    [SerializeField] Int_HomewardBeacon homewardBeacon;
    [SerializeField] InteractableObject dungeonOpenerInt;

    public UnityEvent OnDungeonOpens = new();
    public UnityEvent OnDungeonCloses = new();
    public UnityEvent<int> OnThemeChangedEv = new();

    public Int_HomewardBeacon HomewardBeacon => homewardBeacon;

    [SyncVar(hook = nameof(SetHomewardBeaconPosition))]
    public Vector3 startRoomPos;

    [SyncVar(hook = nameof(OnThemeChanged))]
    public int selectedTheme = 0;

    [SyncVar]
    public int mapSeed;

    [SyncVar]
    public bool dungeonOpen = false;

    private void OnThemeChanged(int oldValue, int newValue) => OnThemeChangedEv?.Invoke(newValue);

    public override void OnStartServer()
    {
        base.OnStartServer();

        dungeonOpen = false;
    }

    [Server]
    public void OnEnterDungeon(PlayerData playerData)
    {
        Instance.playMod.playersOnDungeon.Add(playerData);
        playerData.RpcOnEnterDungeon();
    }

    [Server]
    public void OnReturnOffice(PlayerData playerData)
    {
        Instance.playMod.playersOnDungeon.Remove(playerData);
        playerData.RpcOnReturnOffice();
    }

    [Server]
    public void OpenDungeon()
    {
        dungeonOpenerInt.SetLabel("Close connection");

        OnDungeonOpens?.Invoke();
        dungeonOpen = true;

        Instance.progressionMod.ApplyForDay(Instance.dayMod.currentDay);
        Instance.Teleporter.EnableInteractable();

        mapSeed = LobbySettings.Instance.UseSetSeed
        ? Instance.Seed
        : Random.Range(int.MinValue, int.MaxValue);

        RpcGenerateMap(mapSeed, selectedTheme, Instance.progressionMod.CurrentMapSize);
    }

    [Server]
    public void CloseDungeon()
    {
        dungeonOpenerInt.SetLabel("Open connection");
        Instance.Teleporter.DisableInteractable();

        OnDungeonCloses?.Invoke();
        dungeonOpen = false;

        RpcClearMap();
    }

    [Server]
    public void TryOpenNewDungeon()
    {
        if (Instance.onDeadTime) return;

        if (!Instance.gameStarted)
            Instance.StartGame();

        if (!Instance.dayMod.dayStarted)
        {
            mapGenerator.OnDungeonGenerated.AddListener(StartDay);
            OpenDungeon();
            return;
        }

        if (!dungeonOpen) return;

        if (Instance.playMod.playersOnDungeon.Count > 0)
        {
            AlertMessagerManager.Instance.SendAlert(
                "comrades inside", 
                "There is people in the liminal space", 
                AlertMessage.Severity.Medium);

            Debug.Log("There is players in the liminal space");
            return;
        }

        if (Instance.ecoMod.TotalBalance < Instance.ecoMod.targetQuota)
        {
            AlertMessagerManager.Instance.SendAlert(
                "quota not met", 
                "cannot close the connection until the quota has been met", 
                AlertMessage.Severity.Medium);

            Debug.Log("Quota have not been met");
            return;
        }
        else
        {
            AlertMessagerManager.Instance.SendAlert(
                "quota completed",
                "you have completed today's quota, you can rest for now",
                AlertMessage.Severity.Low);

            Debug.Log("Quota have been met, finishing the day");
            Instance.dayMod.FinishDay();
        }
    }

    [ClientRpc]
    void RpcGenerateMap(int seed, int theme, int mapSize)
    {
        mapGenerator.StartGeneration(seed, theme, mapSize);
    }

    private void SetHomewardBeaconPosition(Vector3 oldValue, Vector3 newValue)
    {
        if (isServer)
        {
            homewardBeacon.transform.position = newValue;
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

    void StartDay()
    {
        Instance.dayMod.StartDay();
        mapGenerator.OnDungeonGenerated.RemoveListener(StartDay);
    }
}
