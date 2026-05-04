using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public struct ChatMessage
{
    public byte[] AvatarData;
    public string SenderName;
    public string Message;
    public PlayerTeam Team;

    public bool IsValid =>
        !string.IsNullOrWhiteSpace(SenderName) &&
        !string.IsNullOrWhiteSpace(Message);

    public override string ToString()
    {
        return $"SenderName: {SenderName}, Message: {Message}, Team: {Team}";
    }
}

public class PlayerChat_Message : MonoBehaviour
{
    [SerializeField] Image iconImage;
    [SerializeField] Image backgroundImg;
    [SerializeField] TextMeshProUGUI senderNameText;
    [SerializeField] TextMeshProUGUI messageText;

    public ChatMessage? CurrentChatMessage { get; private set; }

    public void DisplayChatMessage(ChatMessage message)
    {
        gameObject.SetActive(true);
        transform.SetAsLastSibling();

        iconImage.sprite = AvatarUtils.ByteArrayToSprite(message.AvatarData);
        senderNameText.text = message.SenderName;
        messageText.text = message.Message;

        Color textColor = PlayerTeamColorUtils.GetPlayerTeamTextColor(message.Team);
        senderNameText.color = textColor;
        messageText.color = textColor;

        backgroundImg.color = PlayerTeamColorUtils.GetPlayerTeamColor(message.Team);

        CurrentChatMessage = message;
    }

    public void HideChatMessage()
    {
        gameObject.SetActive(false);
        CurrentChatMessage = null;
    }

    public bool IsActive() => gameObject.activeInHierarchy;
}
