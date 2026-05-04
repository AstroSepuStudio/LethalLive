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
        playerIcon.sprite = AvatarUtils.ByteArrayToSprite(memberData.AvatarData);
        playerPing.text = memberData.Ping.ToString();
        SetTeam(memberData.Team);
    }
}
