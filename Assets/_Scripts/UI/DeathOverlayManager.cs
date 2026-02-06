using Mirror;
using Steamworks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static GameManager;

public class DeathOverlayManager : NetworkBehaviour
{
    [SerializeField] CanvasGroup overlayGroup;
    [SerializeField] Transform content;
    [SerializeField] List<PlayerBanner> playerBanners;

    private void Start()
    {
        overlayGroup.alpha = 0f;
    }

    [Server]
    public void RefreshPlayers()
    {
        List<LobbyMemberData> players = new();

        foreach (var player in Instance.playMod.Players)
        {
            if (!player.Player_Stats.dead) continue;

            players.Add(new LobbyMemberData
            {
                SteamID = player.SteamID,
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
            GameObject banner = Instantiate(playerBanners[0].gameObject, content);
            playerBanners.Add(banner.GetComponent<PlayerBanner>());
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

    public void EnableOverlay() => StartCoroutine(EnableOverlayCor());
    public void DisableOverlay() => StartCoroutine(DisableOverlayCor());

    IEnumerator EnableOverlayCor()
    {
        float t = 0;
        while (t < 2f)
        {
            overlayGroup.alpha = t / 2;
            t += Time.deltaTime;
            yield return null;
        }
        overlayGroup.alpha = 1;
    }

    IEnumerator DisableOverlayCor()
    {
        float t = 1;
        while (t > 0)
        {
            overlayGroup.alpha = t;
            t -= Time.deltaTime;
            yield return null;
        }
        overlayGroup.alpha = 0;
    }
}
