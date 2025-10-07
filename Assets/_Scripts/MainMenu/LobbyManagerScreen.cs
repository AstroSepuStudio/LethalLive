using Mirror;
using Steamworks;
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class LobbyManagerScreen : UIManager
{
    public Sprite defaultIcon;

    [Header("References")]
    [SerializeField] NetworkIdentity identity;
    [SerializeField] TextMeshProUGUI lobbyNameTxt;
    [SerializeField] Camera povCamera;
    [SerializeField] LobbyMemberUI[] lobbyMemberUIs;
    [SerializeField] GameObject pregameWindow;
    [SerializeField] GameObject gameWindow;
    [SerializeField] GameObject loadingWindow;
    [SerializeField] Transform loadingThing;

    [SerializeField] TextMeshProUGUI dayText;
    [SerializeField] TextMeshProUGUI timeText;
    [SerializeField] GameObject deadlineObj;
    [SerializeField] TeamBalancePair hololiveTeamBalance;
    [SerializeField] TeamBalancePair gamersTeamBalance;
    [SerializeField] TeamBalancePair holoXTeamBalance;
    [SerializeField] TeamBalancePair englishTeamBalance;
    [SerializeField] TextMeshProUGUI totalBalanceText;

    [Header("Settings")]
    [SerializeField] float loadThingRotSpd;

    [SerializeField] TMP_Dropdown lobbyType_DP;
    [SerializeField] TMP_InputField mapSize_IP;
    [SerializeField] Toggle teamDamage_Toggle;
    [SerializeField] Toggle teamKnock_Toggle;

    [Header("Colors")]
    public Color hololiveTeamColor = Color.cyan;
    public Color gamersTeamColor = Color.yellow;
    public Color holoXTeamColor = Color.magenta;
    public Color hololiveEnglishTeamColor = Color.blue;

    bool open = false;

    [Serializable]
    struct TeamBalancePair
    {
        public TextMeshProUGUI teamText;
        public TextMeshProUGUI balanceText;
    }

    protected override void Start()
    {
        base.Start();

        GameTick.OnSecond += OnSecond;

        pregameWindow.SetActive(true);
        gameWindow.SetActive(false);

        if (GameManager.Instance.isLocalPlayer)
        {
            GameManager.Instance.LocalPlayer.Player_Input.actions["Esc"].canceled += OnEscapePressed;
        }
    }

    private void OnDestroy()
    {
        GameTick.OnSecond -= OnSecond;

        if (GameManager.Instance.isLocalPlayer)
        {
            GameManager.Instance.LocalPlayer.Player_Input.actions["Esc"].canceled -= OnEscapePressed;
        }
    }

    void OnEscapePressed(InputAction.CallbackContext context)
    {
        if (open)
            CloseLobbyManagerScreen();
    }

    public void OnInteract(PlayerData playerData) => GameManager.Instance.CmdRequestOpenLMS(playerData.Index);

    public void OpenLobbyManagerScreen(int index)
    {
        if (GameManager.Instance.LocalPlayer.Index != index || open) return;

        open = true;

        povCamera.gameObject.SetActive(true);
        GameManager.Instance.LocalPlayer.PlayerCamera.gameObject.SetActive(false);
        GameManager.Instance.LocalPlayer._LockPlayer = true;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void CloseLobbyManagerScreen()
    {
        GameManager.Instance.LocalPlayer.PlayerCamera.gameObject.SetActive(true);
        povCamera.gameObject.SetActive(false);
        GameManager.Instance.LocalPlayer._LockPlayer = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void OnSecond()
    {
        if (!NetworkClient.isConnected) return;

        if (identity.isLocalPlayer)
            GameManager.Instance.CmdSetPlayerPing(GameManager.Instance.LocalPlayer.Index, (int)(NetworkTime.rtt * 1000));

        if (!identity.isServer) return;

        GameManager.Instance.RequestScreenRefresh();
    }

    public void RefreshScreen(GameManager.LobbyMemberData[] members)
    {
        int lobbyIndex = LobbyManager.Instace.LobbySettings.Lobby_Type switch
        {
            ELobbyType.k_ELobbyTypeFriendsOnly => 0,
            ELobbyType.k_ELobbyTypePublic => 1,
            ELobbyType.k_ELobbyTypePrivate => 2,
            _ => 0,
        };

        lobbyType_DP.SetValueWithoutNotify(lobbyIndex);
        mapSize_IP.SetTextWithoutNotify(LobbyManager.Instace.LobbySettings.MapSize.ToString());
        teamDamage_Toggle.SetIsOnWithoutNotify(LobbyManager.Instace.LobbySettings.TeamDamage);
        teamKnock_Toggle.SetIsOnWithoutNotify(LobbyManager.Instace.LobbySettings.TeamKnock);

        for (int i = 0; i < lobbyMemberUIs.Length; i++)
        {
            if (i >= members.Length)
            {
                lobbyMemberUIs[i].gameObject.SetActive(false);
                continue;
            }

            lobbyMemberUIs[i].gameObject.SetActive(true);
            lobbyMemberUIs[i].AssignPlayer(members[i]);
        }

        if (GameManager.Instance == null) return;

        dayText.SetText($"{GameManager.Instance.currentDay}/{LobbySettings.Instance.MaxDays}");

        if (GameManager.Instance.currentDayTime == -1)
        {
            timeText.SetText("--:--");
        }
        else
        {
            float minutesSinceStart = GameManager.Instance.currentDayTime * (960f / 900f);
            int totalMinutes = 8 * 60 + Mathf.RoundToInt(minutesSinceStart);

            int hours = totalMinutes / 60;
            int minutes = totalMinutes % 60;

            timeText.SetText($"{hours:00}:{minutes:00}");
        }

        if (GameManager.Instance.currentDay >= LobbySettings.Instance.MaxDays)
            deadlineObj.SetActive(true);
        else 
            deadlineObj.SetActive(false);

        if (GameManager.Instance.teamHololiveBalance > 0)
        {
            hololiveTeamBalance.teamText.gameObject.SetActive(true);
            hololiveTeamBalance.balanceText.SetText($"${GameManager.Instance.teamHololiveBalance}");
        }
        else hololiveTeamBalance.teamText.gameObject.SetActive(false);

        if (GameManager.Instance.teamHololiveGamers > 0)
        {
            gamersTeamBalance.teamText.gameObject.SetActive(true);
            gamersTeamBalance.balanceText.SetText($"${GameManager.Instance.teamHololiveBalance}");
        }
        else gamersTeamBalance.teamText.gameObject.SetActive(false);

        if (GameManager.Instance.teamHoloXBalance > 0)
        {
            holoXTeamBalance.teamText.gameObject.SetActive(true);
            holoXTeamBalance.balanceText.SetText($"${GameManager.Instance.teamHololiveBalance}");
        }
        else holoXTeamBalance.teamText.gameObject.SetActive(false);

        if (GameManager.Instance.teamEnglishBalance > 0)
        {
            englishTeamBalance.teamText.gameObject.SetActive(true);
            englishTeamBalance.balanceText.SetText($"${GameManager.Instance.teamHololiveBalance}");
        }
        else englishTeamBalance.teamText.gameObject.SetActive(false);

        totalBalanceText.SetText($"${GameManager.Instance.totalBalance}");
    }

    public void RequestTeamSwap(int index)
    {
        PlayerTeam team = (PlayerTeam)index;

        GameManager.Instance.CmdRequestTeamChange(GameManager.Instance.LocalPlayer.Index, team);
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

        LobbyManager.Instace.LobbySettings.SetLobbyType(type);
    }

    public void SetMapSize(string value)
    {
        if (!identity.isServer) return;

        if (int.TryParse(value, out int size))
        {
            LobbyManager.Instace.LobbySettings.SetMapSize(size);
        }
    }

    public void TeamDamage(bool value)
    {
        if (!identity.isServer) return;

        LobbyManager.Instace.LobbySettings.SetTeamDamage(value);
    }

    public void TeamKnock(bool value)
    {
        if (!identity.isServer) return;

        LobbyManager.Instace.LobbySettings.SetTeamKnock(value);
    }

    public void StartGame()
    {
        if (!identity.isServer) return;

        GameManager.Instance.StartGame();
    }

    public void StartDay()
    {
        GameManager.Instance.StartDay();
    }

    public IEnumerator SwitchScreenState()
    {
        pregameWindow.SetActive(false);
        loadingWindow.SetActive(true);

        float timer = 0;
        float duration = UnityEngine.Random.Range(1f, 3f);
        while (timer <= duration)
        {
            loadingThing.localRotation = Quaternion.Euler(0, 0, loadingThing.localRotation.eulerAngles.z - loadThingRotSpd * Time.deltaTime);

            timer += Time.deltaTime;
            yield return null;
        }

        loadingWindow.SetActive(false);
        gameWindow.SetActive(true);
    }

    public void RequestSkinChange(int index)
    {
        GameManager.Instance.LocalPlayer.Skin_Manager.SetSkinIndex(index);
    }

    public void RequestThemeSelection(int index)
    {
        GameManager.Instance.RequestTheme(index);
    }
}
