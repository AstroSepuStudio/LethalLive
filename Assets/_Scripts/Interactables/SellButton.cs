using UnityEngine;

public class SellButton : InteractableObject
{
    [SerializeField] SellPoint sellPoint;

    public override void OnInteract(PlayerData sourceData)
    {
        if (isServer)
        {
            sellPoint.SellItems();
        }
    }
}
