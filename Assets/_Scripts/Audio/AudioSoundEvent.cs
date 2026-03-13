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
            SoundLoudness.Average => 20,
            SoundLoudness.Quiet => 5,
            SoundLoudness.Loud => 40,
            SoundLoudness.Global => Mathf.Infinity,
            _ => throw new System.NotImplementedException()
        };
    }
}

public enum SoundLoudness
{
    Average,
    Quiet,
    Loud,
    Global
}