using UnityEngine;

public enum AudioType
{
    Default,
    Music,
    SFX,
    Ambience,
    VoiceChat
}

[CreateAssetMenu(menuName = "LethalLive/AudioSFX")]
public class AudioSFX : ScriptableObject
{
    public AudioClip clip;
    [Range(0f, 1f)] public float clipVolume = 1f;
    public AudioType audioType = AudioType.Default;
}
