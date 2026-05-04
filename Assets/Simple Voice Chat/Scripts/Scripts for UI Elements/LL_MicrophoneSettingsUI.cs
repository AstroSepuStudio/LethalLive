using SimpleVoiceChat;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LL_MicrophoneSettingsUI : MonoBehaviour
{
    [SerializeField] private string currentMicrophone;

    [Header("Links")]
    [SerializeField] private TMP_Dropdown audioDevicesDropDown;
    [SerializeField] private TMP_Text stateLabel;

    [Header("Test Microphone")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip audioClip;

    void OnEnable()
    {
        RefreshMicrophones();
        RefreshStateLabel();
    }

    // Called by UI dropdown
    public void OnAudioDeviceDropDownChanged()
    {
        if (Microphone.devices.Length == 0)
            return;

        currentMicrophone = Microphone.devices[audioDevicesDropDown.value];

        Debug.Log($"[Mic Settings] Selected device: {currentMicrophone}");

        if (Recorder.Instance != null)
            Recorder.Instance.SetMicrophone(currentMicrophone);
        else
            Debug.LogWarning("[Mic Settings] Recorder not found");
    }

    void RefreshStateLabel()
    {
        stateLabel.text = Microphone.devices.Length > 0
            ? "Microphone: READY"
            : "No microphone detected";
    }

    // Refresh device list
    public void RefreshMicrophones()
    {
        Debug.Log("[Mic Settings] Refreshing microphone list");

        audioDevicesDropDown.ClearOptions();

        var options = new List<TMP_Dropdown.OptionData>();

        foreach (var device in Microphone.devices)
        {
            Debug.Log($"Detected mic: {device}");
            options.Add(new TMP_Dropdown.OptionData(device));
        }

        audioDevicesDropDown.options = options;
        audioDevicesDropDown.RefreshShownValue();

        if (Microphone.devices.Length > 0)
        {
            currentMicrophone = Microphone.devices[0];
            audioDevicesDropDown.value = 0;

            if (Recorder.Instance != null)
                Recorder.Instance.SetMicrophone(currentMicrophone);
        }
    }

    // Test recording (local only)
    public void StartRec()
    {
        if (string.IsNullOrEmpty(currentMicrophone))
            return;

        Debug.Log("[Mic Settings] Start test recording");

        audioClip = Microphone.Start(currentMicrophone, false, 5, 44100);
    }

    public void StopRec()
    {
        if (string.IsNullOrEmpty(currentMicrophone))
            return;

        Debug.Log("[Mic Settings] Stop test recording");

        Microphone.End(currentMicrophone);

        if (audioClip != null)
        {
            audioSource.clip = audioClip;
            audioSource.Play();
        }
    }
}
