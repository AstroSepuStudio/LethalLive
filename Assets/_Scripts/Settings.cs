[System.Serializable]
public class Settings
{
    public float Sensitivity = 8f;
    public float GlobalVolume = 1f;
    public float MusicVolume = 1f;
    public float SFXVolume = 1f;
    public float AmbienceVolume = 1f;
    public float VoiceChatVolume = 1f;

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
}
