using Mirror;
using Steamworks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static GameManager;

public class DeathOverlayManager : NetworkBehaviour
{
    [SerializeField] GameObject overlayObj;
    [SerializeField] CanvasGroup overlayGroup;
    [SerializeField] Transform content;
    [SerializeField] List<PlayerBanner> playerBanners;

    private void Start() => overlayGroup.alpha = 0f;

    void RefreshOverlay()
    {
        LobbyMemberData[] members = Instance.playMod.CachedMemberData;

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

            player.lastTalkTime = Time.time;

            if (!player.IsTalking)
            {
                player.IsTalking = true;
                StartCoroutine(PlayerTalkedCor(player));
            }
            break;
        }
    }

    IEnumerator PlayerTalkedCor(PlayerBanner player)
    {
        player.border.color = player.talkingColor;

        while (Time.time - player.lastTalkTime < 0.3f)
        {
            yield return null;
        }

        float t = 0;
        while (t < 0.3f)
        {
            t += Time.deltaTime;
            player.border.color = Color.Lerp(player.talkingColor, player.defaultColor, t / 0.3f);
            yield return null;
        }

        player.border.color = player.defaultColor;
        player.IsTalking = false;
    }

    public void EnableOverlay()
    {
        Instance.playMod.OnLobbyMemberDataChanged.AddListener(RefreshOverlay);
        RefreshOverlay();
        StartCoroutine(EnableOverlayCor());
    }

    public void DisableOverlay()
    {
        Instance.playMod.OnLobbyMemberDataChanged.RemoveListener(RefreshOverlay);
        StartCoroutine(DisableOverlayCor());
    }

    IEnumerator EnableOverlayCor()
    {
        overlayObj.SetActive(true);

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

        overlayObj.SetActive(false);
    }
}
