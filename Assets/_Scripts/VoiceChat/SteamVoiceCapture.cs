using Mirror;
using Steamworks;
using UnityEngine;

public class SteamVoiceCapture : NetworkBehaviour
{
    const int SEND_RATE = 20;
    float sendTimer;

    bool recording;

    public override void OnStartLocalPlayer()
    {
        if (isLocalPlayer)
        {
            SteamUser.StartVoiceRecording();
            recording = true;
        }
    }

    void OnDestroy()
    {
        if (isLocalPlayer)
            SteamUser.StopVoiceRecording();
    }

    void Update()
    {
        if (!isLocalPlayer || !recording)
            return;

        sendTimer += Time.deltaTime;
        if (sendTimer < 1f / SEND_RATE)
            return;

        sendTimer = 0f;

        SteamUser.GetAvailableVoice(out uint compressedSize);

        if (compressedSize == 0)
            return;

        byte[] buffer = new byte[compressedSize];

        SteamUser.GetVoice(
            true,
            buffer,
            compressedSize,
            out uint bytesWritten
        );

        CmdSendVoice(buffer, bytesWritten);
    }

    [Command(channel = Channels.Unreliable)]
    void CmdSendVoice(byte[] data, uint length)
    {
        RpcReceiveVoice(data, length, netIdentity.netId);
    }

    [ClientRpc(channel = Channels.Unreliable)]
    void RpcReceiveVoice(byte[] data, uint length, uint senderNetId)
    {
        VoicePlaybackManager.Instance.HandleVoice(senderNetId, data, length);
    }
}
