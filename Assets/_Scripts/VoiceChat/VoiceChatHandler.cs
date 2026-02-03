using Mirror;
using SimpleVoiceChat;
using UnityEngine;
using UnityEngine.InputSystem;

public class VoiceChatHandler : NetworkBehaviour
{
    [SerializeField] private Speaker speaker;
    [SerializeField] private GameObject speakingIcon;

    private float speakTime;

    public void OnVoiceChatTrigger(InputAction.CallbackContext context)
    {
        if (!context.started || !isLocalPlayer) return;

        Recorder.Instance.SwitchState();
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();

        Recorder.OnSendDataToNetwork += Cmd_SendVoiceToServer;
    }

    void OnDestroy()
    {
        if (isLocalPlayer)
            Recorder.OnSendDataToNetwork -= Cmd_SendVoiceToServer;
    }

    void Update()
    {
        if (Time.time > speakTime)
            speakingIcon.SetActive(false);
    }

    [Command]
    public void Cmd_SendVoiceToServer(byte[] voiceData)
    {
        Rpc_SendVoice(voiceData);
    }

    [ClientRpc(includeOwner = false)]
    private void Rpc_SendVoice(byte[] voiceData)
    {
        speaker.ProcessVoiceData(voiceData);
        ShowSpeakingIconAboveHead();
    }

    void ShowSpeakingIconAboveHead()
    {
        speakTime = Time.time + 0.3f;
        speakingIcon.SetActive(true);
    }
}
