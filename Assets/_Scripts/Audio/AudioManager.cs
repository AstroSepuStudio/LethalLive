using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Audio Sources")]
    public AudioSource sfxSource;
    public AudioSource musicSource;
    public AudioSource ambienceSource;

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
        if (currentSong != null)
            musicSource.volume = UserSettings.GlobalVolume * UserSettings.MusicVolume * currentSong.clipVolume;
        else
            musicSource.volume = UserSettings.GlobalVolume * UserSettings.MusicVolume;

        if (currentAmbience != null)
            ambienceSource.volume = UserSettings.GlobalVolume * UserSettings.AmbienceVolume * currentAmbience.clipVolume;
        else
            ambienceSource.volume = UserSettings.GlobalVolume * UserSettings.AmbienceVolume;
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

    public void PlayOneShotAndDestroy(Vector3 position, AudioSFX sfx)
    {
        float typeVolume = GetTypeVolume(sfx.audioType);
        float finalVolume = UserSettings.GlobalVolume * typeVolume * sfx.clipVolume;

        AudioSource.PlayClipAtPoint(sfx.clip, position, finalVolume);
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
