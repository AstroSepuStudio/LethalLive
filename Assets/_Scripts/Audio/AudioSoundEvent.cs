using UnityEngine;

[System.Serializable]
public class AudioSoundEvent
{
    public Vector3 position;
    public GameObject source;
    public AudioSFX sfx;
    public SoundLoudness category;

    public AudioSoundEvent(Vector3 position, GameObject source, AudioSFX sfx, SoundLoudness category = SoundLoudness.Average)
    {
        this.position = position;
        this.source = source;
        this.sfx = sfx;
        this.category = category;
    }

    public float GetRadius()
    {
        return category switch
        {
            SoundLoudness.Average => 16,
            SoundLoudness.Quiet => 4,
            SoundLoudness.Loud => 32,
            SoundLoudness.Global => Mathf.Infinity,
            _ => 16
        };
    }
}

public enum SoundLoudness
{
    Average,
    Quiet,
    Loud,
    Global,
    NoSound
}