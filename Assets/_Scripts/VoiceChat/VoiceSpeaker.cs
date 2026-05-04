using Steamworks;
using UnityEngine;

public class VoiceSpeaker : MonoBehaviour
{
    //const int SAMPLE_RATE = 22050;

    //[SerializeField] AudioSource source;
    //byte[] pcmBuffer = new byte[SAMPLE_RATE * 2];

    //public void PlayVoice(byte[] compressed, uint length, bool alive)
    //{
    //    SteamUser.DecompressVoice(
    //        compressed,
    //        length,
    //        pcmBuffer,
    //        (uint)pcmBuffer.Length,
    //        out uint bytesWritten,
    //        SAMPLE_RATE
    //    );

    //    int sampleCount = (int)bytesWritten / 2;
    //    float[] samples = new float[sampleCount];

    //    for (int i = 0; i < sampleCount; i++)
    //    {
    //        short sample = System.BitConverter.ToInt16(pcmBuffer, i * 2);
    //        samples[i] = sample / 32768f;
    //    }

    //    ConfigureSource(alive);

    //    AudioClip clip = AudioClip.Create(
    //        "Voice",
    //        sampleCount,
    //        1,
    //        SAMPLE_RATE,
    //        false
    //    );

    //    clip.SetData(samples, 0);
    //    source.PlayOneShot(clip);
    //}

    //void ConfigureSource(bool alive)
    //{
    //    if (alive)
    //    {
    //        source.spatialBlend = 1f;
    //        source.minDistance = 1f;
    //        source.maxDistance = VoicePlaybackManager.Instance.maxProximityDistance;
    //    }
    //    else
    //    {
    //        source.spatialBlend = 0f;
    //    }
    //}
}
