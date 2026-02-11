using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class PlayerChat_ChannelManager : MonoBehaviour
{
    [SerializeField] TMP_InputField chatMessageIF;
    [SerializeField] Transform chatMessageParent;
    [SerializeField] GameObject chatMessagePrefab;
    [SerializeField] PlayerChat_Message[] messageInstances;
    [SerializeField] GameObject[] channelButtons;
    [SerializeField] int currentChannelIndex;
    [SerializeField] int maxMessages;

    Dictionary<int, ChatMessage[]> channelMessages;
    Dictionary<int, int> channelFirstMessageIndexes;

    private void Awake()
    {
        HideAllMessages();
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

        if (messageInstances.Length >= maxMessages)
        {
            Debug.Log($"There is {messageInstances.Length} messages instances for a max of {maxMessages}, this is fine");
            return;
        }

        Debug.Log($"There is {messageInstances.Length} messages instances for a max of {maxMessages}, instancing new messages");
        List<PlayerChat_Message> messages = messageInstances.ToList();
        while (messages.Count < maxMessages)
        {
            GameObject newMessage = Instantiate(chatMessagePrefab, chatMessageParent);
            messages.Add(newMessage.GetComponent<PlayerChat_Message>());
        }
        messageInstances = messages.ToArray();
    }

    private void Start()
    {
        GameManager.Instance.playMod.LocalPlayer.OnReceiveChatMessage.AddListener(OnReceiveChatMessage);
        GameManager.Instance.playMod.LocalPlayer.OnPlayerTeamChanged.AddListener(UpdateTeamChannels);
        UpdateTeamChannels(GameManager.Instance.playMod.LocalPlayer.Team);
    }

    private void OnDestroy()
    {
        GameManager.Instance.playMod.LocalPlayer.OnReceiveChatMessage.RemoveListener(OnReceiveChatMessage);
        GameManager.Instance.playMod.LocalPlayer.OnPlayerTeamChanged.RemoveListener(UpdateTeamChannels);
    }

    private void OnReceiveChatMessage(int channelIndex, ChatMessage chatMessage)
    {
        Debug.Log($"Receiving chat message from server,\n" +
            $"target channel: {channelIndex}\n" +
            $"message: {chatMessage}");

        int index = channelFirstMessageIndexes[channelIndex];
        bool foundEmpty = false;
        for (int i = 0; i < channelMessages[channelIndex].Length; i++)
        {
            if (channelMessages[channelIndex][i].IsValid)
            {
                Debug.Log($"Found a valid message of index {i} in channel {currentChannelIndex}, checking next messages");
                continue;
            }

            index = i;
            foundEmpty = true;
            Debug.Log($"Found an empty message instance at index {i} in channel {currentChannelIndex}");
            break;
        }

        if (!foundEmpty)
        {
            index++;
            if (index >= messageInstances.Length) index = 0;
            channelFirstMessageIndexes[channelIndex] += index;
        }

        channelMessages[channelIndex][index] = chatMessage;
        Debug.Log($"Setting message {index} of chat channel {channelIndex} to {chatMessage}");

        if (channelIndex != currentChannelIndex)
        {
            Debug.Log($"Active chat channel {currentChannelIndex} doesn't match target chat channel {channelIndex}, not displaying chat message");
            return;
        }

        DisplayChatMessage(chatMessage);
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
        Debug.Log($"Updating team channels for {team}");

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

        channelButtons[teamIndex].SetActive(true);
        for (int i = 1; i < channelButtons.Length; i++)
        {
            if (i == teamIndex) continue;
            channelButtons[i].SetActive(false);
        }
    }

    public void SwitchChannel(int channelIndex)
    {
        if (channelIndex == currentChannelIndex)
        {
            Debug.Log($"Already in this channel {channelIndex}");
            return;
        }

        if (channelIndex < 0 || 
            channelIndex >= messageInstances.Length)
        {
            Debug.Log($"Invalid channel index ({channelIndex})");
            return;
        }

        Debug.Log($"Changing player chat channel to {channelIndex}");
        currentChannelIndex = channelIndex;
        HideAllMessages();

        for (int i = 0; i < messageInstances.Length; i++)
        {
            if (!channelMessages[channelIndex][i].IsValid)
            {
                Debug.Log($"Found invalid message of index {i} in channel {channelIndex}, canceling action");
                break;
            }
            messageInstances[i].DisplayChatMessage(channelMessages[channelIndex][i]);
        }
    }

    public void DisplayChatMessage(ChatMessage message)
    {
        int index = 0;

        for (int i = 0; i < messageInstances.Length; i++)
        {
            if (messageInstances[i].IsActive())
            {
                Debug.Log($"Found a valid message of index {i} in channel {currentChannelIndex}, checking next messages");
                continue;
            }

            index = i;
            Debug.Log($"Found an empty message instance at index {i} in channel {currentChannelIndex}");
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
