using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
    [SerializeField] TextMeshProUGUI senderNameText;
    [SerializeField] TextMeshProUGUI messageText;

    public void DisplayChatMessage(ChatMessage message)
    {
        gameObject.SetActive(true);
        transform.SetAsLastSibling();

        iconImage.sprite = AvatarUtils.ByteArrayToSprite(message.AvatarData);
        senderNameText.text = message.SenderName;
        messageText.text = message.Message;
    }

    public void HideChatMessage() => gameObject.SetActive(false);
    public bool IsActive() => gameObject.activeInHierarchy;
}
