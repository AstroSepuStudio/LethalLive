using UnityEngine;

public class AnimatorEventHelper : MonoBehaviour
{
    [SerializeField] SkinData skinData;
    //[SerializeField] AudioSFX[] footsteps;
    //int fs_index = 0;

    public void PunchDetectionEvent()
    {
        Debug.Log("Animation punch event triggered");
        skinData.pData.Punch_Manager.PunchDetection();
    }

    //public void PlayQuietAudio(AudioSFX audio)
    //{
    //    AudioManager.Instance.PlayOneShot(skinData.pData.Quiet_AS, audio);
    //}

    //public void PlayModestAudio(AudioSFX audio)
    //{
    //    AudioManager.Instance.PlayOneShot(skinData.pData.Modest_AS, audio);
    //}

    //public void PlayLoudAudio(AudioSFX audio)
    //{
    //    AudioManager.Instance.PlayOneShot(skinData.pData.Loud_AS, audio);
    //}

    //public void PlayFootstep()
    //{
    //    AudioManager.Instance.PlayOneShot(skinData.pData.Modest_AS, footsteps[fs_index], 0.35f);
    //    fs_index++;
    //    if (fs_index >= footsteps.Length)
    //        fs_index = 0;
    //}

    //public void PlayFootstepSprint()
    //{
    //    AudioManager.Instance.PlayOneShot(skinData.pData.Loud_AS, footsteps[fs_index]);
    //    fs_index++;
    //    if (fs_index >= footsteps.Length)
    //        fs_index = 0;
    //}
}
