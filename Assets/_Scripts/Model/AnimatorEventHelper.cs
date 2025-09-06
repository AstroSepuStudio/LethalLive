using UnityEngine;

public class AnimatorEventHelper : MonoBehaviour
{
    [SerializeField] SkinData skinData;

    public void PunchDetectionEvent()
    {
        skinData.pData.Punch_Manager.PunchDetection();
    }

    public void PunchAnimationFinished()
    {
        skinData.pData.Punch_Manager.PunchFinished();
    }

    public void PrimaryItemAnimationTrigger()
    {
        skinData.pData.PlayerInventory.PrimaryItemAnimationTrigger();
    }

    public void PrimaryItemAnimationFinishes()
    {
        skinData.pData.PlayerInventory.PrimaryItemAnimationFinishes();
    }

    public void SecondaryItemAnimationTrigger()
    {
        skinData.pData.PlayerInventory.SecondaryItemAnimationTrigger();
    }

    public void SecondaryItemAnimationFinishes()
    {
        skinData.pData.PlayerInventory.SecondaryItemAnimationFinishes();
    }
}
