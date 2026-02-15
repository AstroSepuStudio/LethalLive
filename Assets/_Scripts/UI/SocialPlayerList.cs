using Mirror;
using Steamworks;
using System.Collections.Generic;
using UnityEngine;
using static GameManager;

public class SocialPlayerList : NetworkBehaviour
{
    [SerializeField] List<PlayerBannerMicMod> playerBanners;
    [SerializeField] Transform playerPanelParent;

    [Server]
    public void RefreshPlayers()
    {
        List<LobbyMemberData> players = new();

        foreach (var player in Instance.playMod.Players)
        {
            players.Add(new LobbyMemberData
            {
                SteamID = player.SteamID,
                netID = player.netId,
                Name = player.PlayerName,
                AvatarData = player.AvatarData,
                Team = player.Team,
                Ping = player.Ping
            });
        }

        Rpc_RefreshOverlay(players.ToArray());
    }

    [TargetRpc]
    void Rpc_RefreshOverlay(LobbyMemberData[] members)
    {
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
