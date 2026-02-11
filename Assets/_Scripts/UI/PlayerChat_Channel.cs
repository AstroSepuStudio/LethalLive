using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlayerChat_Channel : MonoBehaviour
{
    [SerializeField] TMP_InputField chatMessageIF;
    [SerializeField] PlayerChat_Message[] messageInstances;
    [SerializeField] int currentChannelIndex;
    [SerializeField] int maxMessages;

    Dictionary<int, ChatMessage[]> channelMessages;

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
    }

    private void Start()
    {
        PlayerChat_Manager.Instance.OnReceiveChatMessage.AddListener(OnReceiveChatMessage);
    }

    private void OnReceiveChatMessage(int channelIndex, ChatMessage chatMessage)
    {
        int index = 0;
        for (int i = 0; i < channelMessages[channelIndex].Length; i++)
        {
            if (channelMessages[channelIndex][i].IsValid) continue;
            index = i; break;
        }

        channelMessages[channelIndex][index] = chatMessage;

        if (channelIndex != currentChannelIndex) return;

        DisplayChatMessage(chatMessage);
    }

    private void HideAllMessages()
    {
        foreach (PlayerChat_Message message in messageInstances)
        {
            message.HideChatMessage();
        }
    }

    public void SwitchChannel(int channelIndex)
    {
        if (channelIndex < 0 || channelIndex >= messageInstances.Length) return;

        currentChannelIndex = channelIndex;
        HideAllMessages();

        for (int i = 0; i < messageInstances.Length; i++)
        {
            messageInstances[i].DisplayChatMessage(channelMessages[channelIndex][i]);
        }
    }

    public void DisplayChatMessage(ChatMessage message)
    {
        int index = 0;

        for (int i = 0; i < messageInstances.Length; i++)
        {
            if (messageInstances[i].IsActive())
                continue;

            index = i;
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
    }
}
