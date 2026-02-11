using Mirror;

public class PlayerChat_Manager : NetworkBehaviour
{
    public static PlayerChat_Manager Instance;

    private void Awake()
    {
        Instance = this;
    }

    [Command(requiresAuthority = false)]
    public void Cmd_SendChatMessage(uint senderID, int channelIndex, string message)
    {
        PlayerData pData = GameManager.Instance.playMod.GetPlayerByNetId(senderID);

        ChatMessage chatMessage = new()
        {
            AvatarData = pData.AvatarData,
            SenderName = pData.PlayerName,
            Message = message,
            Team = pData.Team
        };

        foreach (var player in GameManager.Instance.playMod.Players)
        {
            if (channelIndex != 0 && pData.Team != player.Team) continue;
            pData.Rpc_ReceiveChatMessage(channelIndex, chatMessage);
        }
    }
}
