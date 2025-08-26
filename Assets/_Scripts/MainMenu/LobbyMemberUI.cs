using Steamworks;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyMemberUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] LobbyManagerScreen screenManager;
    [SerializeField] Image playerIcon;
    [SerializeField] Image background;
    [SerializeField] TextMeshProUGUI playerName;
    [SerializeField] TextMeshProUGUI playerPing;

    Coroutine gettingName;
    Coroutine gettingIcon;
    WaitForSeconds requestDelay = new WaitForSeconds(0.05f);

    public void SetTeam(PlayerTeam team)
    {
        switch (team)
        {
            case PlayerTeam.Hololive: background.color = screenManager.hololiveTeamColor; break;
            case PlayerTeam.Gamers: background.color = screenManager.gamersTeamColor; break;
            case PlayerTeam.HoloX: background.color = screenManager.holoXTeamColor; break;
            case PlayerTeam.English: background.color = screenManager.hololiveEnglishTeamColor; break;
        }
    }

    public void AssignPlayer(GameManager.LobbyMemberData memberData)
    {
        playerName.text = memberData.Name;
        playerIcon.sprite = ByteArrayToSprite(memberData.AvatarData);
        playerPing.text = memberData.Ping.ToString();
        SetTeam(memberData.Team);

        //if (gettingName != null) StopCoroutine(gettingName);
        //if (gettingIcon != null) StopCoroutine(gettingIcon);

        //gettingName = StartCoroutine(SetPlayerName(memberData.SteamID));
        //gettingIcon = StartCoroutine(GetSteamAvatar(memberData.SteamID));
    }

    public void RemovePlayer()
    {

    }

    public Sprite ByteArrayToSprite(byte[] imageData)
    {
        if (imageData == null || imageData.Length == 0)
        {
            Debug.Log("byte array is empty or null");
            return null;
        }

        Texture2D tex = new (2, 2);
        bool isLoaded = tex.LoadImage(imageData);

        if (!isLoaded)
            return null;

        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
    }

    private IEnumerator SetPlayerName(CSteamID steamId)
    {
        string name = "";
        while (string.IsNullOrEmpty(name))
        {
            name = SteamFriends.GetFriendPersonaName(steamId);
            yield return null;
        }

        playerName.text = name;
    }


    IEnumerator GetSteamAvatar(CSteamID steamID)
    {
        int avatarInt = SteamFriends.GetLargeFriendAvatar(steamID);

        while (avatarInt == -1)
        {
            yield return null;
            avatarInt = SteamFriends.GetLargeFriendAvatar(steamID);
        }

        uint width = 0, height = 0;
        bool success = false;

        while ((!success || width == 0 || height == 0))
        {
            success = SteamUtils.GetImageSize(avatarInt, out width, out height);
            if (!success || width == 0 || height == 0)
            {
                yield return requestDelay;
            }
        }

        if (!success || width == 0 || height == 0)
        {
            Debug.LogWarning("Steam avatar size could not be retrieved.");
            playerIcon.sprite = screenManager.defaultIcon;
            yield break;
        }

        byte[] imageData = new byte[4 * (int)width * (int)height];
        bool gotImage = SteamUtils.GetImageRGBA(avatarInt, imageData, imageData.Length);
        while (!gotImage)
        {
            gotImage = SteamUtils.GetImageRGBA(avatarInt, imageData, imageData.Length);
            if (!gotImage)
            {
                yield return requestDelay;
            }
        }

        if (!gotImage)
        {
            Debug.LogWarning("Steam avatar image could not be retrieved.");
            playerIcon.sprite = screenManager.defaultIcon;
            yield break;
        }

        Texture2D avatar = new ((int)width, (int)height, TextureFormat.RGBA32, false, true);
        avatar.LoadRawTextureData(imageData);
        avatar.Apply();

        playerIcon.sprite = Sprite.Create(avatar, new Rect(0, 0, avatar.width, avatar.height), new Vector2(0.5f, 0.5f));
    }
}
