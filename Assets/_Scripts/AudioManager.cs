using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Audio Sources")]
    public AudioSource sfxSource;
    public AudioSource musicSource;
    public AudioSource ambienceSource;
    public AudioSource voiceChatSource;

    AudioSFX currentSong;
    AudioSFX currentAmbience;

    Settings UserSettings => SettingsManager.Instance.UserSettings;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    private void Start()
    {
        UserSettings.OnSettingsChanged += OnSettingsChanged;
    }

    private void OnDestroy()
    {
        if (UserSettings != null)
            UserSettings.OnSettingsChanged -= OnSettingsChanged;
    }

    void OnSettingsChanged()
    {
        musicSource.volume = UserSettings.GlobalVolume * UserSettings.MusicVolume * currentSong.clipVolume;
        ambienceSource.volume = UserSettings.AmbienceVolume * UserSettings.AmbienceVolume * currentAmbience.clipVolume;
        voiceChatSource.volume = UserSettings.VoiceChatVolume;
    }

    private float GetTypeVolume(AudioType type)
    {
        return type switch
        {
            AudioType.Music => UserSettings.MusicVolume,
            AudioType.SFX => UserSettings.SFXVolume,
            AudioType.Ambience => UserSettings.AmbienceVolume,
            AudioType.VoiceChat => UserSettings.VoiceChatVolume,
            _ => 1f
        };
    }

    public void PlayOneShot(AudioSFX sfx)
    {
        float typeVolume = GetTypeVolume(sfx.audioType);
        float finalVolume = UserSettings.GlobalVolume * typeVolume * sfx.clipVolume;

        sfxSource.PlayOneShot(sfx.clip, finalVolume);
    }

    public void PlayOneShot(AudioSource source, AudioSFX sfx)
    {
        float typeVolume = GetTypeVolume(sfx.audioType);
        float finalVolume = UserSettings.GlobalVolume * typeVolume * sfx.clipVolume;

        source.PlayOneShot(sfx.clip, finalVolume);
    }

    public void PlayOneShot(AudioSource source, AudioSFX sfx, float multiplier)
    {
        float typeVolume = GetTypeVolume(sfx.audioType);
        float finalVolume = UserSettings.GlobalVolume * typeVolume * sfx.clipVolume * multiplier;

        source.PlayOneShot(sfx.clip, finalVolume);
    }

    public void PlayMusic(AudioSFX song)
    {
        musicSource.clip = song.clip;
        musicSource.volume = UserSettings.GlobalVolume * UserSettings.MusicVolume * song.clipVolume;
        musicSource.Play();
        currentSong = song;
    }

    public void StopMusic()
    {
        musicSource.Stop();
    }

    public void PlayAmbience(AudioSFX ambience)
    {
        ambienceSource.clip = ambience.clip;
        ambienceSource.volume = UserSettings.GlobalVolume * UserSettings.AmbienceVolume * ambience.clipVolume;
        ambienceSource.Play();
        currentAmbience = ambience;
    }

    public void StopAmbience()
    {
        ambienceSource.Stop();
    }
}
