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

    WaitForSeconds requestDelay = new(0.05f);

    public void SetTeam(PlayerTeam team)
    {
        switch (team)
        {
            case PlayerTeam.White: background.color = Color.white; break;
            case PlayerTeam.Red: background.color = Color.red; break;
            case PlayerTeam.Blue: background.color = Color.blue; break;
            case PlayerTeam.Yellow: background.color = Color.yellow; break;
            case PlayerTeam.Green: background.color = Color.green; break;
            case PlayerTeam.Pink: background.color = Color.pink; break;
        }
    }

    public void AssignPlayer(GameManager.LobbyMemberData memberData)
    {
        playerName.text = memberData.Name;
        playerIcon.sprite = ByteArrayToSprite(memberData.AvatarData);
        playerPing.text = memberData.Ping.ToString();
        SetTeam(memberData.Team);
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
}
