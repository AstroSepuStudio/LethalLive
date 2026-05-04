using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static GameManager;

public class PlayerBanner : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI playerName;
    [SerializeField] Image playerIcon;
    public Image border;

    public Color defaultColor = Color.white;
    public Color talkingColor = Color.green;

    public float lastTalkTime;
    public bool IsTalking;

    public LobbyMemberData MemberData { get; private set; }

    public virtual void SetPlayer(LobbyMemberData memberData)
    {
        MemberData = memberData;

        playerName.text = memberData.Name;
        playerIcon.sprite = AvatarUtils.ByteArrayToSprite(memberData.AvatarData);
    }
}
