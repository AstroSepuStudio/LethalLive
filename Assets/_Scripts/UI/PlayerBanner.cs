using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static GameManager;

public class PlayerBanner : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI playerName;
    [SerializeField] Image playerIcon;
    [SerializeField] Image border;

    [SerializeField] Color defaultColor = Color.green;
    [SerializeField] Color talkingColor = Color.white;

    public LobbyMemberData MemberData { get; private set; }
    Coroutine playerTalked;
    float talkedTime;

    public void SetPlayer(LobbyMemberData memberData)
    {
        MemberData = memberData;

        playerName.text = memberData.Name;
        playerIcon.sprite = AvatarUtils.ByteArrayToSprite(memberData.AvatarData);
    }

    public void PlayerTalked()
    {
        talkedTime = Time.time;
        if (playerTalked != null) return;

        playerTalked = StartCoroutine(PlayerTalkedCor());
    }

    IEnumerator PlayerTalkedCor()
    {
        border.color = talkingColor;

        while (talkedTime > Time.time - 0.3f)
            yield return null;

        float t = 0;
        while (t < 0.3f)
        {
            t += Time.deltaTime;
            border.color = Color.Lerp(talkingColor, defaultColor, t / 0.3f);
            yield return null;
        }

        border.color = defaultColor;
        playerTalked = null;
    }
}
