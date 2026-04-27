using UnityEngine;

public class IA_PlaceBeacon : ItemAction
{
    [SerializeField] Item_HomewardBeacon homewardBeacon;
    bool placing = false;

    public override void Execute()
    {
        item.PData.Skin_Data.CharacterAnimator.SetBool("PlaceBeacon", true);
        item.PData._LockPlayer = true;
        placing = true;
    }

    public override void Cancel()
    {
        if (!placing) return;

        item.PData.Skin_Data.CharacterAnimator.SetBool("PlaceBeacon", false);
        item.PData._LockPlayer = false;
        placing = false;
    }

    public override void OnAnimationFinish()
    {
        item.PData.Skin_Data.CharacterAnimator.SetBool("PlaceBeacon", false);
        //item.PData.PlayerInventory.RemoveCurrentItem();
        item.PData._LockPlayer = false;
        placing = false;

        homewardBeacon.PlaceBeacon();
    }
}