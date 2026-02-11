using UnityEngine;

[System.Serializable]
public class Settings
{
    // - Controls -
    [SerializeField] float Sensitivity = 8f;

    public float GetSensitivity() => Sensitivity;

    // - Audio -
    [SerializeField] float GlobalVolume = 1f;
    [SerializeField] float MusicVolume = 1f;
    [SerializeField] float SFXVolume = 1f;
    [SerializeField] float AmbienceVolume = 1f;
    [SerializeField] float VoiceChatVolume = 1f;
    [SerializeField] bool HoldToTalk = false;

    public float GetGlobalVolume() => GlobalVolume;
    public float GetMusicVolume() => MusicVolume;
    public float GetSFXVolume() => SFXVolume;
    public float GetAmbienceVolume() => AmbienceVolume;
    public float GetVoiceChatVolume() => VoiceChatVolume;
    public bool GetHoldToTalk() => HoldToTalk;

    // - Graphics -
    [SerializeField] int ScreenModeIndex = 0;
    [SerializeField] int ResolutionIndex = 0;
    [SerializeField] int TargetFPS = 60;
    [SerializeField] bool VsyncEnabled = true;
    [SerializeField] bool PostProcessingEnabled = true;

    public int GetScreenModeIndex() => ScreenModeIndex;
    public int GetResolutionIndex() => ResolutionIndex;
    public int GetTargetFPS() => TargetFPS;
    public bool GetVsyncEnabled() => VsyncEnabled;
    public bool GetPostProcessingEnabled() => PostProcessingEnabled;

    public event System.Action OnSettingsChanged;

    public void SetSensitivity(float value)
    {
        Sensitivity = value;
        OnSettingsChanged?.Invoke();
    }

    public void SetGlobalVolume(float value)
    {
        GlobalVolume = value;
        OnSettingsChanged?.Invoke();
    }

    public void SetMusicVolume(float value)
    {
        MusicVolume = value;
        OnSettingsChanged?.Invoke();
    }

    public void SetSFXVolume(float value)
    {
        SFXVolume = value;
        OnSettingsChanged?.Invoke();
    }

    public void SetAmbienceVolume(float value)
    {
        AmbienceVolume = value;
        OnSettingsChanged?.Invoke();
    }

    public void SetVoiceChatVolume(float value)
    {
        VoiceChatVolume = value;
        OnSettingsChanged?.Invoke();
    }

    public void SetScreenModeIndex(int index)
    {
        ScreenModeIndex = index;
        OnSettingsChanged?.Invoke();
    }

    public void SetResolutionIndex(int index)
    {
        ScreenModeIndex = index;
        OnSettingsChanged?.Invoke();
    }

    public void SetTargetFPS(int fps)
    {
        TargetFPS = fps;
        OnSettingsChanged?.Invoke();
    }

    public void SetVsync(bool enabled)
    {
        VsyncEnabled = enabled;
        OnSettingsChanged?.Invoke();
    }

    public void SetPostProcessing(bool enabled)
    {
        PostProcessingEnabled = enabled;
        OnSettingsChanged?.Invoke();
    }

    public void SetHoldToTalk(bool enabled)
    {
        HoldToTalk = enabled;
        OnSettingsChanged?.Invoke();
    }
}
