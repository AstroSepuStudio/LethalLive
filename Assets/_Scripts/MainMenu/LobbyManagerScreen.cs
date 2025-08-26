using Mirror;
using Steamworks;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyManagerScreen : UIManager
{
    public Sprite defaultIcon;

    [SerializeField] NetworkIdentity identity;
    [SerializeField] TextMeshProUGUI lobbyNameTxt;
    [SerializeField] Camera povCamera;
    [SerializeField] LobbyMemberUI[] lobbyMemberUIs;

    [SerializeField] TMP_Dropdown lobbyType_DP;
    [SerializeField] TMP_InputField mapSize_IP;
    [SerializeField] Toggle teamDamage_Toggle;
    [SerializeField] Toggle teamKnock_Toggle;

    [Header("Colors")]
    public Color hololiveTeamColor = Color.cyan;
    public Color gamersTeamColor = Color.yellow;
    public Color holoXTeamColor = Color.magenta;
    public Color hololiveEnglishTeamColor = Color.blue;

    protected override void Start()
    {
        base.Start();

        GameTick.OnSecond += OnSecond;
    }

    private void OnDestroy()
    {
        GameTick.OnSecond -= OnSecond;
    }

    public void OnInteract(PlayerData playerData) => GameManager.Instance.CmdRequestOpenLMS(playerData.Index);

    public void OpenLobbyManagerScreen(int index)
    {
        if (GameManager.Instance.LocalPlayer.Index != index) return;

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
}
