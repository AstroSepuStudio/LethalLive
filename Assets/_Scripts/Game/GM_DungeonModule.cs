using Mirror;
using UnityEngine;
using UnityEngine.Events;
using static GameManager;

public class GM_DungeonModule : NetworkBehaviour
{
    [field: SerializeField] public ThemeDataSO[] ThemeDatas { get; private set; }

    [SerializeField] MapGenerator mapGenerator;
    [SerializeField] Int_Teleport teleporter;
    [SerializeField] Int_HomewardBeacon homewardBeacon;

    public UnityEvent OnDungeonOpens = new();
    public UnityEvent OnDungeonCloses = new();
    public UnityEvent<int> OnThemeChangedEv = new();

    [SyncVar]
    public Vector3 startRoomPos;

    [SyncVar(hook = nameof(OnThemeChanged))]
    public int selectedTheme = 0;

    [SyncVar]
    public int mapSeed;

    [SyncVar]
    public bool dungeonOpen = false;

    private void OnThemeChanged(int oldValue, int newValue) => OnThemeChangedEv?.Invoke(newValue);

    [Server]
    public void OnEnterDungeon(PlayerData playerData)
    {
        if (ThemeDatas[selectedTheme].loopingMusic != null)
            AudioManager.Instance.PlayMusic(ThemeDatas[selectedTheme].loopingMusic);

        Instance.playMod.playersOnDungeon.Add(playerData);
    }

    [Server]
    public void OnReturnOffice(PlayerData playerData)
    {
        AudioManager.Instance.StopMusic();

        Instance.playMod.playersOnDungeon.Remove(playerData);
    }

    [Server]
    public void OpenDungeon()
    {
        OnDungeonOpens?.Invoke();
        dungeonOpen = true;

        mapSeed = Random.Range(-1000000, 1000000);
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
        if (!Instance.gameStarted)
            Instance.StartGame();

        if (!Instance.dayMod.dayStarted)
        {
            mapGenerator.OnDungeonGenerated.AddListener(StartDay);
            OpenDungeon();
            return;
        }

        if (Instance.playMod.playersOnDungeon.Count > 0)
        {
            Debug.Log("There is players in the liminal space");
            return;
        }

        CloseDungeon();
        OpenDungeon();
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

    void StartDay()
    {
        Instance.dayMod.StartDay();
        mapGenerator.OnDungeonGenerated.RemoveListener(StartDay);
    }
}
