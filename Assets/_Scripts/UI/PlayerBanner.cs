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
        playerIcon.sprite = ByteArrayToSprite(memberData.AvatarData);
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

    public Sprite ByteArrayToSprite(byte[] imageData)
    {
        if (imageData == null || imageData.Length == 0)
        {
            Debug.Log("byte array is empty or null");
            return null;
        }

        Texture2D tex = new(2, 2);
        bool isLoaded = tex.LoadImage(imageData);

        if (!isLoaded)
            return null;

        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
    }
}
