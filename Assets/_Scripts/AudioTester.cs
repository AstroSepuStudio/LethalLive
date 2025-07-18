using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class AudioTester : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI text;
    [SerializeField] AudioSource source;
    [SerializeField] AudioSFX[] audios;
    int index = 0;

    private void Start()
    {
        text.SetText(audios[index].name);
    }

    public void PlayStopAudio(InputAction.CallbackContext ctx)
    {
        if (ctx.started)
        {
            if (source.isPlaying)
                source.Stop();
            else
                PlayAudio(ctx);
        }
    }

    public void PlayAudio(InputAction.CallbackContext ctx)
    {
        if (ctx.started)
        {
            if (index < audios.Length)
            {
                text.SetText(audios[index].name);
                source.clip = audios[index].clip;
                source.volume = audios[index].clipVolume;
                source.Play();
            }
        }
    }

    public void NextSFX(InputAction.CallbackContext ctx)
    {
        if (ctx.started)
        {
            index++;
            if (index >= audios.Length)
                index = audios.Length - 1;

            PlayAudio(ctx);
        }
    }

    public void PrevSFX(InputAction.CallbackContext ctx)
    {
        if (ctx.started)
        {
            index--;
            if (index < 0)
                index = 0;
            PlayAudio(ctx);
        }
    }
}
