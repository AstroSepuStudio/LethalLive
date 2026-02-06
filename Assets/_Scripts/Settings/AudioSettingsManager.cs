using UnityEngine;

public class AudioSettingsManager : MonoBehaviour
{
    SettingsManager SettingsManager => SettingsManager.Instance;

    [SerializeField] PlayerData pData;

    public void OnGlobalVolumeChanged(float value)
    {
        SettingsManager.UserSettings.SetGlobalVolume(value);
        SettingsManager.SaveSettings();
    }

    public void OnMusicVolumeChanged(float value)
    {
        SettingsManager.UserSettings.SetMusicVolume(value);
        SettingsManager.SaveSettings();
    }

    public void OnSFXVolumeChanged(float value)
    {
        SettingsManager.UserSettings.SetSFXVolume(value);
        SettingsManager.SaveSettings();
    }

    public void OnAmbienceVolumeChanged(float value)
    {
        SettingsManager.UserSettings.SetAmbienceVolume(value);
        SettingsManager.SaveSettings();
    }

    public void OnVoiceChatVolumeChanged(float value)
    {
        SettingsManager.UserSettings.SetVoiceChatVolume(value);
        SettingsManager.SaveSettings();
    }

    public void OnVoiceChatModeChanged(int index)
    {
        bool hold2Talk = index == 1;

        if (GameManager.Instance != null)
            if (GameManager.Instance.playMod.LocalPlayer != null)
                GameManager.Instance.playMod.LocalPlayer.VCHandler.SetVoiceChatMode(hold2Talk);

        SettingsManager.UserSettings.SetHoldToTalk(hold2Talk);
        SettingsManager.SaveSettings();
    }
}
