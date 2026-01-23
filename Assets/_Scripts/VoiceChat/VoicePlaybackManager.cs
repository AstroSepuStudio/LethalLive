using Mirror;
using UnityEngine;

public class VoicePlaybackManager : MonoBehaviour
{
    public static VoicePlaybackManager Instance;
    public float maxProximityDistance = 20f;

    void Awake()
    {
        Instance = this;
    }

    public void HandleVoice(uint senderNetId, byte[] data, uint length)
    {
        // Do not play own voice
        if (senderNetId == GameManager.Instance.playMod.LocalPlayer.netId)
            return;

        if (!NetworkClient.spawned.TryGetValue(senderNetId, out NetworkIdentity id))
            return;

        if (id.isLocalPlayer)
            return;

        if (!id.TryGetComponent<PlayerData>(out var senderState))
            return;

        // Cross-state mute
        if (senderState.Player_Stats.dead != GameManager.Instance.playMod.LocalPlayer.Player_Stats.dead)
            return;

        PlayerData pData = GameManager.Instance.playMod.GetPlayerByNetId(senderNetId);

        VoiceSpeaker speaker = pData.Voice_Speaker;
        Debug.Log(data);
        Debug.Log(length);
        speaker.PlayVoice(data, length, senderState.Player_Stats.dead);
    }
}
