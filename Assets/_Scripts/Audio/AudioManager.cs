using UnityEngine;
using LethalLive;
using System.Collections;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public enum PlayState { Play, Stop, Pause };
    public static AudioManager Instance;

    [Header("Audio Sources")]
    public AudioSource sfxSource;
    public AudioSource musicSource;
    public AudioSource ambienceSource;

    AudioSFX currentSong;
    AudioSFX currentAmbience;

    readonly Dictionary<AudioSource, (float timer, AudioSFX sfx, GameObject goSrc, SoundLoudness loudness)> controlledSources = new();
    const float hearingBroadcastInterval = 0.5f;

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


    void Update()
    {
        var keys = new List<AudioSource>(controlledSources.Keys);
        foreach (var src in keys)
        {
            if (src == null || !src.isPlaying)
            {
                controlledSources.Remove(src);
                continue;
            }

            var data = controlledSources[src];
            data.timer -= Time.deltaTime;

            if (data.timer <= 0f)
            {
                data.timer = hearingBroadcastInterval;
                BroadcastToHearing(src.transform.position, data.goSrc, data.sfx, data.loudness);
            }

            controlledSources[src] = data;
        }
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
        if (category == SoundLoudness.NoSound) return;

        var soundEvent = new AudioSoundEvent(position, source, sfx, category);
        HearingEventBroadcaster.Instance.Broadcast(soundEvent);
    }

    public void PlayOneShot(AudioSFX sfx)
    {
        float typeVolume = GetTypeVolume(sfx.audioType);
        float finalVolume = UserSettings.GetGlobalVolume() * typeVolume * sfx.clipVolume;

        sfxSource.PlayOneShot(sfx.clip, finalVolume);
    }

    public void PlayOneShot(AudioSource src, AudioSFX sfx, Vector3 position)
    {
        float typeVolume = GetTypeVolume(sfx.audioType);
        float finalVolume = UserSettings.GetGlobalVolume() * typeVolume * sfx.clipVolume;

        src.transform.position = position;
        src.PlayOneShot(sfx.clip, finalVolume);
    }

    public void PlayOneShotAndDestroy(Vector3 position, AudioSFX sfx, GameObject goSrc = null, SoundLoudness category = SoundLoudness.NoSound)
    {
        float typeVolume = GetTypeVolume(sfx.audioType);
        float finalVolume = UserSettings.GetGlobalVolume() * typeVolume * sfx.clipVolume;

        AudioSource.PlayClipAtPoint(sfx.clip, position, finalVolume);

        GameObject go = goSrc ? goSrc : null;
        BroadcastToHearing(position, go, sfx, category);
    }

    public void PlayOneShotAndDestroy(AudioSource src, AudioSFX sfx, GameObject goSrc = null, SoundLoudness category = SoundLoudness.NoSound)
    {
        float typeVolume = GetTypeVolume(sfx.audioType);
        float finalVolume = UserSettings.GetGlobalVolume() * typeVolume * sfx.clipVolume;

        src.PlayOneShot(sfx.clip, finalVolume);
        Destroy(src.gameObject, sfx.clip.length + 0.1f);

        GameObject go = goSrc ? goSrc : src.gameObject;
        BroadcastToHearing(src.transform.position, go, sfx, category);
    }

    public void PlayOneShot(AudioSource src, AudioSFX sfx, GameObject goSrc = null, SoundLoudness category = SoundLoudness.NoSound)
    {
        float typeVolume = GetTypeVolume(sfx.audioType);
        float finalVolume = UserSettings.GetGlobalVolume() * typeVolume * sfx.clipVolume;

        src.PlayOneShot(sfx.clip, finalVolume);

        GameObject go = goSrc ? goSrc : src.gameObject;
        BroadcastToHearing(src.transform.position, go, sfx, category);
    }

    public void PlayOneShotWithDelay(AudioSource src, AudioSFX sfx, float delay, GameObject goSrc = null, SoundLoudness category = SoundLoudness.NoSound)
        => StartCoroutine(DelayPlay(src, sfx, delay, goSrc, category));

    IEnumerator DelayPlay(AudioSource src, AudioSFX sfx, float delay, GameObject goSrc = null, SoundLoudness category = SoundLoudness.NoSound)
    {
        yield return new WaitForSeconds(delay);
        PlayOneShot(src, sfx, goSrc, category);
    }

    public void PlayOneShot(AudioSource src, AudioSFX sfx, float multiplier, GameObject goSrc = null, SoundLoudness category = SoundLoudness.NoSound)
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

    public void PlayControllerSFX(AudioSource src, AudioSFX sfx, PlayState playState,
        GameObject goSrc = null, SoundLoudness category = SoundLoudness.NoSound)
    {
        if (src == null || sfx == null) return;

        switch (playState)
        {
            case PlayState.Play:
                float typeVolume = GetTypeVolume(sfx.audioType);
                float finalVolume = UserSettings.GetGlobalVolume() * typeVolume * sfx.clipVolume;

                if (src.clip != sfx.clip)
                    src.clip = sfx.clip;

                src.volume = finalVolume;
                src.loop = true;

                if (!src.isPlaying) src.Play();

                GameObject go = goSrc ? goSrc : src.gameObject;

                if (!controlledSources.ContainsKey(src))
                    BroadcastToHearing(src.transform.position, go, sfx, category);

                controlledSources[src] = (hearingBroadcastInterval, sfx, go, category);
                break;

            case PlayState.Pause:
                src.Pause();
                controlledSources.Remove(src);
                break;

            case PlayState.Stop:
                src.Stop();
                controlledSources.Remove(src);
                break;
        }
    }

    public void StopControlledSFX(AudioSource src)
    {
        if (src == null) return;
        src.Stop();
        controlledSources.Remove(src);
    }
}
