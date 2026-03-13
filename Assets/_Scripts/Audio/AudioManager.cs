using UnityEngine;
using LethalLive;
using System.Collections;
using UnityEngine.UIElements;

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
            musicSource.volume = UserSettings.GetGlobalVolume() * UserSettings.GetMusicVolume() * currentSong.clipVolume;
        else
            musicSource.volume = UserSettings.GetGlobalVolume() * UserSettings.GetMusicVolume();

        if (currentAmbience != null)
            ambienceSource.volume = UserSettings.GetGlobalVolume() * UserSettings.GetAmbienceVolume() * currentAmbience.clipVolume;
        else
            ambienceSource.volume = UserSettings.GetGlobalVolume() * UserSettings.GetAmbienceVolume();
    }

    private float GetTypeVolume(AudioType type)
    {
        return type switch
        {
            AudioType.Music => UserSettings.GetMusicVolume(),
            AudioType.SFX => UserSettings.GetSFXVolume(),
            AudioType.Ambience => UserSettings.GetAmbienceVolume(),
            AudioType.VoiceChat => UserSettings.GetVoiceChatVolume(),
            _ => 1f
        };
    }

    private void BroadcastToHearing(Vector3 position, GameObject source, AudioSFX sfx, SoundLoudness category)
    {
        if (HearingEventBroadcaster.Instance == null) return;

        var soundEvent = new AudioSoundEvent(position, source, sfx, category);
        HearingEventBroadcaster.Instance.Broadcast(soundEvent);
    }

    public void PlayOneShot(AudioSFX sfx)
    {
        float typeVolume = GetTypeVolume(sfx.audioType);
        float finalVolume = UserSettings.GetGlobalVolume() * typeVolume * sfx.clipVolume;

        sfxSource.PlayOneShot(sfx.clip, finalVolume);
    }

    public void PlayOneShotAndDestroy(Vector3 position, AudioSFX sfx, GameObject goSrc = null, SoundLoudness category = SoundLoudness.Average)
    {
        float typeVolume = GetTypeVolume(sfx.audioType);
        float finalVolume = UserSettings.GetGlobalVolume() * typeVolume * sfx.clipVolume;

        AudioSource.PlayClipAtPoint(sfx.clip, position, finalVolume);

        GameObject go = goSrc ? goSrc : null;
        BroadcastToHearing(position, go, sfx, category);
    }

    public void PlayOneShotAndDestroy(AudioSource src, AudioSFX sfx, GameObject goSrc = null, SoundLoudness category = SoundLoudness.Average)
    {
        float typeVolume = GetTypeVolume(sfx.audioType);
        float finalVolume = UserSettings.GetGlobalVolume() * typeVolume * sfx.clipVolume;

        src.PlayOneShot(sfx.clip, finalVolume);
        Destroy(src.gameObject, sfx.clip.length + 0.1f);

        GameObject go = goSrc ? goSrc : src.gameObject;
        BroadcastToHearing(src.transform.position, go, sfx, category);
    }

    public void PlayOneShot(AudioSource src, AudioSFX sfx, GameObject goSrc = null, SoundLoudness category = SoundLoudness.Average)
    {
        float typeVolume = GetTypeVolume(sfx.audioType);
        float finalVolume = UserSettings.GetGlobalVolume() * typeVolume * sfx.clipVolume;

        src.PlayOneShot(sfx.clip, finalVolume);

        GameObject go = goSrc ? goSrc : src.gameObject;
        BroadcastToHearing(src.transform.position, go, sfx, category);
    }

    public void PlayOneShot(AudioSource src, AudioSFX sfx, float multiplier, GameObject goSrc = null, SoundLoudness category = SoundLoudness.Average)
    {
        float typeVolume = GetTypeVolume(sfx.audioType);
        float finalVolume = UserSettings.GetGlobalVolume() * typeVolume * sfx.clipVolume * multiplier;

        src.PlayOneShot(sfx.clip, finalVolume);

        GameObject go = goSrc ? goSrc : src.gameObject;
        BroadcastToHearing(src.transform.position, go, sfx, category);
    }

    public void PlayMusic(AudioSFX song)
    {
        musicSource.clip = song.clip;
        musicSource.volume = UserSettings.GetGlobalVolume() * UserSettings.GetMusicVolume() * song.clipVolume;
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
        ambienceSource.volume = UserSettings.GetGlobalVolume() * UserSettings.GetAmbienceVolume() * ambience.clipVolume;
        ambienceSource.Play();
        currentAmbience = ambience;
    }

    public void StopAmbience()
    {
        ambienceSource.Stop();
    }
}
