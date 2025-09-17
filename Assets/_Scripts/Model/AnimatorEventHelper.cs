using UnityEngine;

public class AnimatorEventHelper : MonoBehaviour
{
    [SerializeField] SkinData skinData;

    public void PunchDetectionEvent()
    {
        if (!skinData.pData.isServer) return;
        skinData.pData.Punch_Manager.PunchDetection();
    }

    public void PunchAnimationFinished()
    {
        if (!skinData.pData.isServer) return;
        skinData.pData.Punch_Manager.PunchFinished();
    }

    public void PrimaryItemAnimationTrigger()
    {
        if (!skinData.pData.isServer) return;
        skinData.pData.PlayerInventory.PrimaryItemAnimationTrigger();
    }

    public void PrimaryItemAnimationFinishes()
    {
        if (!skinData.pData.isServer) return;
        skinData.pData.PlayerInventory.PrimaryItemAnimationFinishes();
    }

    public void SecondaryItemAnimationTrigger()
    {
        if (!skinData.pData.isServer) return;
        skinData.pData.PlayerInventory.SecondaryItemAnimationTrigger();
    }

    public void SecondaryItemAnimationFinishes()
    {
        if (!skinData.pData.isServer) return;
        skinData.pData.PlayerInventory.SecondaryItemAnimationFinishes();
    }
}
