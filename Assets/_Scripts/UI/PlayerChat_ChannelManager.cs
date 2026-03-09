using Mirror;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class PlayerChat_ChannelManager : NetworkBehaviour
{
    [SerializeField] TMP_InputField chatMessageIF;
    [SerializeField] Transform chatMessageParent;
    [SerializeField] GameObject chatMessagePrefab;
    [SerializeField] PlayerChat_Message[] messageInstances;
    [SerializeField] ChannelButtonUI[] channelButtons;
    [SerializeField] int currentChannelIndex;
    [SerializeField] int maxMessages;

    Dictionary<int, ChatMessage[]> channelMessages;
    Dictionary<int, int> channelFirstMessageIndexes;

    [HideInInspector]
    public UnityEvent<int> OnSwitchChannel;

    private void Awake()
    {
        channelMessages = new()
        {
            { 0, new ChatMessage[maxMessages] }, // Global
            { 1, new ChatMessage[maxMessages] }, // White
            { 2, new ChatMessage[maxMessages] }, // Red
            { 3, new ChatMessage[maxMessages] }, // Blue
            { 4, new ChatMessage[maxMessages] }, // Yellow
            { 5, new ChatMessage[maxMessages] }, // Green
            { 6, new ChatMessage[maxMessages] } // Pink
        };

        channelFirstMessageIndexes = new()
        {
            { 0, 0 }, // Global
            { 1, 0 }, // White
            { 2, 0 }, // Red
            { 3, 0 }, // Blue
            { 4, 0 }, // Yellow
            { 5, 0 }, // Green
            { 6, 0 } // Pink
        };

        for (int i = 0; i < channelButtons.Length; i++)
        {
            channelButtons[i].Initialize();
            if (i != 0)
                channelButtons[i].HideChannel();
        }

        if (messageInstances.Length >= maxMessages)
        {
            //Debug.Log($"There is {messageInstances.Length} messages instances for a max of {maxMessages}, this is fine");
            HideAllMessages();
            return;
        }

        //Debug.Log($"There is {messageInstances.Length} messages instances for a max of {maxMessages}, instancing new messages");
        List<PlayerChat_Message> messages = messageInstances.ToList();
        while (messages.Count < maxMessages)
        {
            GameObject newMessage = Instantiate(chatMessagePrefab, chatMessageParent);
            messages.Add(newMessage.GetComponent<PlayerChat_Message>());

        }
        messageInstances = messages.ToArray();

        HideAllMessages();
    }

    private void Start()
    {
        if (!isLocalPlayer) return;

        GameManager.Instance.playMod.LocalPlayer.OnReceiveChatMessage.AddListener(OnReceiveChatMessage);
        GameManager.Instance.playMod.LocalPlayer.OnPlayerTeamChanged.AddListener(UpdateTeamChannels);
        UpdateTeamChannels(GameManager.Instance.playMod.LocalPlayer.Team);
    }

    private void OnDestroy()
    {
        if (!isLocalPlayer) return;

        GameManager.Instance.playMod.LocalPlayer.OnReceiveChatMessage.RemoveListener(OnReceiveChatMessage);
        GameManager.Instance.playMod.LocalPlayer.OnPlayerTeamChanged.RemoveListener(UpdateTeamChannels);
    }

    private void OnReceiveChatMessage(int channelIndex, ChatMessage chatMessage)
    {
        int index = channelFirstMessageIndexes[channelIndex];

        channelMessages[channelIndex][index] = chatMessage;

        // advance pointer (ring)
        channelFirstMessageIndexes[channelIndex] =
            (index + 1) % maxMessages;

        //Debug.Log($"Stored message at {index}. Next write = {channelFirstMessageIndexes[channelIndex]}");

        if (channelIndex != currentChannelIndex)
            return;

        RebuildChannel(channelIndex);
    }

    private void HideAllMessages()
    {
        foreach (PlayerChat_Message message in messageInstances)
        {
            message.HideChatMessage();
        }
    }

    private void UpdateTeamChannels(PlayerTeam team)
    {
        //Debug.Log($"Updating team channels for {team}");

        int teamIndex = team switch
        {
            PlayerTeam.White => 1,
            PlayerTeam.Red => 2,
            PlayerTeam.Blue => 3,
            PlayerTeam.Yellow => 4,
            PlayerTeam.Green => 5,
            PlayerTeam.Pink => 6,
            _ => 0
        };

        channelButtons[teamIndex].Initialize();
        for (int i = 1; i < channelButtons.Length; i++)
        {
            if (i == teamIndex) continue;
            channelButtons[i].HideChannel();
        }

        if (currentChannelIndex != 0)
            SwitchChannel(teamIndex);
    }

    private void RebuildChannel(int channelIndex)
    {
        HideAllMessages();

        int start = channelFirstMessageIndexes[channelIndex];
        int ui = 0;

        for (int i = 0; i < maxMessages; i++)
        {
            int dataIndex = (start + i) % maxMessages;
            var msg = channelMessages[channelIndex][dataIndex];

            if (!msg.IsValid)
            {
                //Debug.Log("invalid message");
                continue;
            }

            messageInstances[ui].DisplayChatMessage(msg);
            ui++;
        }
    }

    public void SwitchChannel(int channelIndex)
    {
        if (channelIndex == currentChannelIndex)
        {
            //Debug.Log($"Already in this channel {channelIndex}");
            return;
        }

        if (channelIndex < 0)
        {
            //Debug.Log($"Invalid channel index ({channelIndex})");
            return;
        }

        currentChannelIndex = channelIndex;

        OnSwitchChannel?.Invoke(channelIndex);
        RebuildChannel(channelIndex);
    }

    public void DisplayChatMessage(ChatMessage message)
    {
        int index = 0;

        for (int i = 0; i < maxMessages; i++)
        {
            if (messageInstances[i].IsActive())
            {
                //Debug.Log($"Found a valid message of index {i} in channel {currentChannelIndex}, checking next messages");
                continue;
            }

            index = i;
            //Debug.Log($"Found an empty message instance at index {i} in channel {currentChannelIndex}");
            break;
        }

        messageInstances[index].DisplayChatMessage(message);
    }

    public void SendChatMessage()
    {
        PlayerChat_Manager.Instance.Cmd_SendChatMessage(
            GameManager.Instance.playMod.LocalPlayer.netId, 
            currentChannelIndex,
            chatMessageIF.text);

        chatMessageIF.text = "";
    }
}
