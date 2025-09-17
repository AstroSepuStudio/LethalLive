using UnityEngine;

public class Item_Crowbar : ItemBase
{
    [SerializeField] ItemHurtbox hurtBox;

    public override void PrimaryAction()
    {
        if (isServer)
            InUse = true;

        pData.Skin_Data.CharacterAnimator.SetTrigger("Atk_Crowbar");
        pData.Skin_Data.CharacterAnimator.SetLayerWeight(2, 1);

        hurtBox.EnableHitbox();
        pData.Camera_Movement.ForcePlayerToAim();
    }

    public override void PrimaryAnimationFinish()
    {
        if (isServer)
            InUse = false;

        pData.Skin_Data.CharacterAnimator.SetLayerWeight(2, 0);

        hurtBox.DisableHitbox();
        pData.Camera_Movement.StopForcePlayerToAim();
    }
}
