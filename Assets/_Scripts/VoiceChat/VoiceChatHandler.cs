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
    bool hearYS;

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
    public void SetHearYourself(bool hearYS) => this.hearYS = hearYS;

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
        Cmd_SendVoiceToServer(voiceData);
    }

    void Update()
    {
        if (Time.time > speakTime)
            speakingIcon.SetActive(false);
    }

    [Command]
    void Cmd_SendVoiceToServer(byte[] voiceData)
    {
        Rpc_SendVoice(voiceData);
    }

    [ClientRpc]
    void Rpc_SendVoice(byte[] voiceData)
    {
        //pData.PlayerTalked();
        GameManager.Instance.playMod.LocalPlayer.PlayerTalked(pData.SteamID);

        if (isLocalPlayer && !hearYS) return;

        // Alive listeners never hear dead players
        if (!pData.Player_Stats.dead && GameManager.Instance.playMod.LocalPlayer.Player_Stats.dead)
            return;

        speaker.ConfigureSpatialMode(pData.Player_Stats.dead);
        speaker.ProcessVoiceData(voiceData);
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
