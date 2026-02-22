using Mirror;
using Steamworks;
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using static GameManager;

public class LobbyManagerScreen : UIManagerNetwork
{
    public Sprite defaultIcon;

    [Header("References")]
    [SerializeField] NetworkIdentity identity;
    [SerializeField] Canvas worldCanvas;
    [SerializeField] RectTransform refRectTransform;
    //[SerializeField] TextMeshProUGUI lobbyNameTxt;
    [SerializeField] Camera povCamera;
    //[SerializeField] LobbyMemberUI[] lobbyMemberUIs;
    //[SerializeField] GameObject pregameWindow;
    //[SerializeField] GameObject gameWindow;
    [SerializeField] GameObject loadingWindow;
    [SerializeField] Transform loadingThing;
    [SerializeField] Transform playerTargetPos;
    public Transform rightHCIKTarget;

    [SerializeField] TextMeshProUGUI dayText;
    [SerializeField] TextMeshProUGUI timeText;
    [SerializeField] TextMeshProUGUI levelText;
    //[SerializeField] GameObject deadlineObj;
    [SerializeField] TeamBalancePair teamWhiteBalance;
    [SerializeField] TeamBalancePair teamRedBalance;
    [SerializeField] TeamBalancePair teamBlueBalance;
    [SerializeField] TeamBalancePair teamYellowBalance;
    [SerializeField] TeamBalancePair teamGreenBalance;
    [SerializeField] TeamBalancePair teamPinkBalance;
    [SerializeField] TextMeshProUGUI totalBalanceText;

    [Header("Settings")]
    [SerializeField] Vector2 rhcikTargetLimit;
    [SerializeField] float loadThingRotSpd;

    [SerializeField] TMP_Dropdown lobbyType_DP;
    [SerializeField] TMP_InputField mapSize_IP;
    [SerializeField] Toggle teamDamage_Toggle;
    [SerializeField] Toggle teamKnock_Toggle;

    [SyncVar] public int playerOnLMS = -1;
    [SyncVar] bool open = false;

    PlayerData currentPlayer;

    [Serializable]
    struct TeamBalancePair
    {
        public GameObject teamObj;
        public TextMeshProUGUI balanceText;
    }

    protected override void Start()
    {
        base.Start();

        //pregameWindow.SetActive(true);
        //gameWindow.SetActive(false);

        Instance.playMod.OnLobbyMemberDataChanged.AddListener(RefreshLobbyManagerScreen);
        LobbyManager.Instance.LobbySettings.OnLobbySettingsChanged.AddListener(RefreshLobbyManagerScreen);
    }

    private void OnDestroy()
    {
        Instance.playMod.OnLobbyMemberDataChanged.RemoveListener(RefreshLobbyManagerScreen);
        LobbyManager.Instance.LobbySettings.OnLobbySettingsChanged.RemoveListener(RefreshLobbyManagerScreen);
    }

    public void OnEscapePressed(InputAction.CallbackContext context)
    {
        if (!context.started) return;

        CloseLMS();
    }

    public void CloseLMS()
    {
        if (isLocalPlayer && !open) return;

        CmdCloseLMS(Instance.playMod.LocalPlayer.Index);
    }

    public void OnInteract(PlayerData playerData)
    {
        if (open) return;

        open = true;
        playerOnLMS = playerData.Index;
        playerData._LockPlayer = true;
        currentPlayer = playerData;

        playerData.Teleport(playerTargetPos.position);
        playerData.Skin_Data.Rigging_Manager.RpcEnableRightHandChainRig();

        RpcOpenLMS(playerData.Index);
    }

    [ClientRpc]
    void RpcOpenLMS(int index)
    {
        if (Instance.playMod.LocalPlayer.Index != index) return;
        Instance.playMod.LocalPlayer.Player_Input.actions["Esc"].started += OnEscapePressed;

        //GameManager.Instance.LocalPlayer.Skin_Data.SkinRenderer.enabled = false;
        Instance.playMod.LocalPlayer.PlayerCanvas.SetActive(false);

        povCamera.gameObject.SetActive(true);
        Instance.playMod.LocalPlayer.PlayerCamera.gameObject.SetActive(false);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        StartCoroutine(RHCIKTPosHandler());
    }

    [Command(requiresAuthority = false)]
    void CmdCloseLMS(int index)
    {
        if (!open || index != playerOnLMS) return;

        currentPlayer.Skin_Data.Rigging_Manager.RpcDisableRightHandChainRig();
        currentPlayer._LockPlayer = false;
        currentPlayer = null;

        open = false;
        playerOnLMS = -1;

        RpcCloseLMS(index);
    }

    [ClientRpc]
    public void RpcCloseLMS(int index)
    {
        if (Instance.playMod.LocalPlayer.Index != index) return;
        Instance.playMod.LocalPlayer.Player_Input.actions["Esc"].started -= OnEscapePressed;

        //GameManager.Instance.LocalPlayer.Skin_Data.SkinRenderer.enabled = true;
        Instance.playMod.LocalPlayer.PlayerCanvas.SetActive(true);
        
        Instance.playMod.LocalPlayer.PlayerCamera.gameObject.SetActive(true);
        povCamera.gameObject.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void RefreshLobbyManagerScreen()
    {
        //LobbyMemberData[] members = Instance.playMod.CachedMemberData;

        int lobbyIndex = LobbyManager.Instance.LobbySettings.Lobby_Type switch
        {
            ELobbyType.k_ELobbyTypeFriendsOnly => 0,
            ELobbyType.k_ELobbyTypePublic => 1,
            ELobbyType.k_ELobbyTypePrivate => 2,
            _ => 0,
        };

        lobbyType_DP.SetValueWithoutNotify(lobbyIndex);
        mapSize_IP.SetTextWithoutNotify(LobbyManager.Instance.LobbySettings.MapSize.ToString());
        teamDamage_Toggle.SetIsOnWithoutNotify(LobbyManager.Instance.LobbySettings.TeamDamage);
        teamKnock_Toggle.SetIsOnWithoutNotify(LobbyManager.Instance.LobbySettings.TeamKnock);

        //for (int i = 0; i < lobbyMemberUIs.Length; i++)
        //{
        //    if (i >= members.Length)
        //    {
        //        lobbyMemberUIs[i].gameObject.SetActive(false);
        //        continue;
        //    }

        //    lobbyMemberUIs[i].gameObject.SetActive(true);
        //    lobbyMemberUIs[i].AssignPlayer(members[i]);
        //}

        if (Instance == null) return;

        dayText.SetText($"{Instance.dayMod.currentDay}/{LobbySettings.Instance.MaxDays}");

        if (Instance.dayMod.currentDayTime == -1)
        {
            timeText.SetText("--:--");
        }
        else
        {
            float minutesSinceStart = Instance.dayMod.currentDayTime * (960f / 900f);
            int totalMinutes = 8 * 60 + Mathf.RoundToInt(minutesSinceStart);

            int hours = totalMinutes / 60;
            int minutes = totalMinutes % 60;

            timeText.SetText($"{hours:00}:{minutes:00}");
        }

        //if (Instance.dayMod.currentDay >= LobbySettings.Instance.MaxDays)
        //    deadlineObj.SetActive(true);
        //else 
        //    deadlineObj.SetActive(false);

        SetTeamBalance(Instance.ecoMod.teamsBalance[PlayerTeam.White], teamWhiteBalance);
        SetTeamBalance(Instance.ecoMod.teamsBalance[PlayerTeam.Red], teamRedBalance);
        SetTeamBalance(Instance.ecoMod.teamsBalance[PlayerTeam.Blue], teamBlueBalance);
        SetTeamBalance(Instance.ecoMod.teamsBalance[PlayerTeam.Yellow], teamYellowBalance);
        SetTeamBalance(Instance.ecoMod.teamsBalance[PlayerTeam.Green], teamGreenBalance);
        SetTeamBalance(Instance.ecoMod.teamsBalance[PlayerTeam.Pink], teamPinkBalance);

        totalBalanceText.SetText($"${Instance.ecoMod.TotalBalance}");

        levelText.SetText(Instance.dngMod.ThemeDatas[Instance.dngMod.selectedTheme].levelName);
    }

    void SetTeamBalance(float balance, TeamBalancePair pair)
    {
        if (balance > 0)
        {
            pair.teamObj.SetActive(true);
            pair.balanceText.SetText($"${balance}");
        }
        else pair.teamObj.SetActive(false);
    }

    public void RequestTeamSwap(int index)
    {
        PlayerTeam team = (PlayerTeam)index;

        Instance.playMod.CmdRequestTeamChange(Instance.playMod.LocalPlayer.Index, team);
    }

    public void SetLobbyType(int index)
    {
        if (!identity.isServer) return;

        ELobbyType type = lobbyType_DP.options[index].text switch
        {
            "Friends Only" => ELobbyType.k_ELobbyTypeFriendsOnly,
            "Public" => ELobbyType.k_ELobbyTypePublic,
            "Private" => ELobbyType.k_ELobbyTypePrivate,
            _ => ELobbyType.k_ELobbyTypeFriendsOnly,
        };

        LobbyManager.Instance.LobbySettings.SetLobbyType(type);
    }

    public void SetMapSize(string value)
    {
        if (!identity.isServer) return;

        if (int.TryParse(value, out int size))
        {
            LobbyManager.Instance.LobbySettings.SetMapSize(size);
        }
    }

    public void TeamDamage(bool value)
    {
        if (!identity.isServer) return;

        LobbyManager.Instance.LobbySettings.SetTeamDamage(value);
    }

    public void TeamKnock(bool value)
    {
        if (!identity.isServer) return;

        LobbyManager.Instance.LobbySettings.SetTeamKnock(value);
    }

    public void StartGame()
    {
        if (!identity.isServer) return;

        Instance.StartGame();
    }

    public void StartDay()
    {
        Instance.dayMod.StartDay();
    }

    //[ClientRpc]
    //public void RpcSwitchScreenState()
    //{
    //    float duration = UnityEngine.Random.Range(1f, 3f);
    //    StartCoroutine(SwitchCoroutine(duration));
    //}

    //IEnumerator SwitchCoroutine(float duration)
    //{
    //    pregameWindow.SetActive(false);
    //    loadingWindow.SetActive(true);

    //    float timer = 0;
    //    while (timer <= duration)
    //    {
    //        loadingThing.localRotation = Quaternion.Euler(0, 0, loadingThing.localRotation.eulerAngles.z - loadThingRotSpd * Time.deltaTime);

    //        timer += Time.deltaTime;
    //        yield return null;
    //    }

    //    loadingWindow.SetActive(false);
    //    gameWindow.SetActive(true);
    //}

    public void RequestThemeSelection(int index)
    {
        Instance.dngMod.RequestTheme(index);
    }

    IEnumerator RHCIKTPosHandler()
    {
        float w8timer = 0;
        float timer = 0;
        float syncDelay = 0.01f;
        while (open || w8timer < 0.1f)
        {
            while (timer < syncDelay)
            {
                w8timer += Time.deltaTime;
                timer += Time.deltaTime;
                yield return null;
            }

            timer = 0;

            Vector2 mousePos = Input.mousePosition;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle (
                refRectTransform, mousePos, worldCanvas.worldCamera, out Vector2 localPoint))
            {
                CmdRequestRHCIKTPosChange(localPoint);
            }
            else
                CmdRequestRHCIKTPosChange(Vector2.zero);

            yield return null;
        }
    }

    [Command(requiresAuthority = false)]
    public void CmdRequestRHCIKTPosChange(Vector2 pos)
    {
        RpcSetRHCIKTPos(pos);
    }

    [ClientRpc]
    void RpcSetRHCIKTPos(Vector2 pos)
    {
        pos.x = Mathf.Clamp(pos.x, -rhcikTargetLimit.x, rhcikTargetLimit.x);
        pos.y = Mathf.Clamp(pos.y, -rhcikTargetLimit.y, rhcikTargetLimit.y);

        rightHCIKTarget.localPosition = pos;
    }
}
