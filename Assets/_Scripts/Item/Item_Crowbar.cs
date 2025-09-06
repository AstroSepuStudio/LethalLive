using UnityEngine;

public class Item_Crowbar : ItemBase
{
    [SerializeField] ItemHurtbox hurtBox;

    public override void PrimaryAction()
    {
        if (isServer)
            InUse = true;

        pData.Skin_Data.CharacterAnimator.SetTrigger("Atk_Crowbar");
        pData.Skin_Data.CharacterAnimator.SetLayerWeight(3, 1);

        hurtBox.EnableHitbox();
    }

    public override void PrimaryAnimationFinish()
    {
        if (isServer)
            InUse = false;

        pData.Skin_Data.CharacterAnimator.SetLayerWeight(3, 0);

        hurtBox.DisableHitbox();
    }
}
