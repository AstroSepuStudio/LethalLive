using Mirror;
using SimpleVoiceChat;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class VoiceChatHandler : NetworkBehaviour
{
    [SerializeField] PlayerData pData;
    [SerializeField] Speaker speaker;
    [SerializeField] GameObject speakingIcon;
    [SerializeField] Image micImage;

    float speakTime;
    bool hold2Talk;

    private void Start()
    {
        if (!Recorder.Instance.IsRecording)
        {
            micImage.fillAmount = 1;
            micImage.color = Color.red;
            return;
        }
    }

    public void SetVoiceChatMode(bool hold2Talk) => this.hold2Talk = hold2Talk;

    public void OnVoiceChatTrigger(InputAction.CallbackContext context)
    {
        if (!isLocalPlayer) return;

        if (hold2Talk)
        {
            if (context.started)
                Recorder.Instance.SwitchState(true);
            else if (context.canceled)
                Recorder.Instance.SwitchState(false);
        }
        else
        {
            if (context.started)
                Recorder.Instance.SwitchState();
        }
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        Recorder.OnSendDataToNetwork += OnLocalVoiceCaptured;
    }

    void OnLocalVoiceCaptured(byte[] voiceData)
    {
        bool speakerIsDead = pData.Player_Stats.dead;

        if (speakerIsDead)
            pData.DeathOvManager.PlayerTalked(pData.SteamID);

        Cmd_SendVoiceToServer(voiceData, speakerIsDead);
    }

    void Update()
    {
        if (Time.time > speakTime)
            speakingIcon.SetActive(false);
    }

    [Command]
    void Cmd_SendVoiceToServer(byte[] voiceData, bool speakerIsDead)
    {
        Rpc_SendVoice(voiceData, speakerIsDead);
    }

    [ClientRpc(includeOwner = false)]
    void Rpc_SendVoice(byte[] voiceData, bool speakerIsDead)
    {
        // Alive listeners never hear dead players
        if (!pData.Player_Stats.dead && speakerIsDead)
            return;

        speaker.ConfigureSpatialMode(speakerIsDead);

        speaker.ProcessVoiceData(voiceData);
        if (pData.Player_Stats.dead)
            pData.DeathOvManager.PlayerTalked(pData.SteamID);
        ShowSpeakingIconAboveHead();
    }

    void ShowSpeakingIconAboveHead()
    {
        speakTime = Time.time + 0.3f;
        speakingIcon.SetActive(true);
    }

    public void VoiceDetected(float decibels)
    {
        if (!Recorder.Instance.IsRecording)
        {
            micImage.fillAmount = 1;
            micImage.color = Color.red;
            return;
        }

        micImage.color = Color.white;

        float minDb = -60f;
        float maxDb = -20f;

        float l = Mathf.Clamp01((decibels - minDb) / (maxDb - minDb));
        micImage.fillAmount = l;
    }
}
