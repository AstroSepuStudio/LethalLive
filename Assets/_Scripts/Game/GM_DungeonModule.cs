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
    [SerializeField] float generationCD = 300;

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

    [SyncVar]
    public float genTimer = 0;

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
        StartCoroutine(GenerationCooldown());

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
    public void ResetCooldown() => genTimer = 0;

    [Server]
    public void TryOpenNewDungeon()
    {
        if (genTimer > 0)
        {
            Debug.Log($"Generation on cooldown {genTimer}");
            return;
        }

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

    IEnumerator GenerationCooldown()
    {
        genTimer = generationCD;
        while (genTimer > 0)
        {
            genTimer -= Time.deltaTime;
            yield return null;
        }
        genTimer = 0;
    }
}
