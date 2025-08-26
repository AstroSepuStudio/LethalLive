using UnityEngine;

public class Item_Ball : ItemBase
{
    public override void PrimaryAction()
    {
        Debug.Log("Primary action");
    }

    public override void SecondaryAction()
    {
        Debug.Log("Secondary action");
    }
}
