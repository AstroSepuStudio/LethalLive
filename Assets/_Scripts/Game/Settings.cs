[System.Serializable]
public class Settings
{
    // - Controls -
    public float Sensitivity = 8f;

    // - Audio -
    public float GlobalVolume = 1f;
    public float MusicVolume = 1f;
    public float SFXVolume = 1f;
    public float AmbienceVolume = 1f;
    public float VoiceChatVolume = 1f;
    public bool HoldToTalk = false;

    // - Graphics -
    public int ScreenModeIndex = 0;
    public int ResolutionIndex = 0;
    public int targetFPS = 60;
    public bool VsyncEnabled = true;
    public bool PostProcessingEnabled = true;

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
        targetFPS = fps;
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
