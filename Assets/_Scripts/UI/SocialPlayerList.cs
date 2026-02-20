using Mirror;
using Steamworks;
using System.Collections.Generic;
using UnityEngine;
using static GameManager;

public class SocialPlayerList : NetworkBehaviour
{
    [SerializeField] List<PlayerBannerMicMod> playerBanners;
    [SerializeField] Transform playerPanelParent;

    private void Start()
    {
        Instance.playMod.OnLobbyMemberDataChanged.AddListener(OnLobbyMemberDataChanged);
    }

    private void OnDestroy()
    {
        Instance.playMod.OnLobbyMemberDataChanged.AddListener(OnLobbyMemberDataChanged);
    }

    void OnLobbyMemberDataChanged()
    {
        LobbyMemberData[] members = Instance.playMod.CachedMemberData;

        foreach (var banner in playerBanners)
            banner.gameObject.SetActive(false);

        while (playerBanners.Count < members.Length)
        {
            GameObject banner = Instantiate(playerBanners[0].gameObject, playerPanelParent);
            playerBanners.Add(banner.GetComponent<PlayerBannerMicMod>());
        }

        for (var i = 0; i < members.Length; i++)
        {
            playerBanners[i].SetPlayer(members[i]);
            playerBanners[i].gameObject.SetActive(true);
        }
    }

    public void PlayerTalked(CSteamID steamID)
    {
        foreach (var player in playerBanners)
        {
            if (player.MemberData.SteamID != steamID) continue;

            player.PlayerTalked();
            break;
        }
    }
}
