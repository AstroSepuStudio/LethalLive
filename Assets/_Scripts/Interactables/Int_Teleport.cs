using UnityEngine;

public class Int_Teleport : InteractableObject
{
    [SerializeField] Transform targetPosition;

    public void SetTeleportPos(Vector3 pos)
    {
        targetPosition.position = pos;
    }

    public override void OnInteract(PlayerData sourceData)
    {
        sourceData.Character_Controller.enabled = false;
        sourceData.Character_Controller.transform.position = targetPosition.position;
        sourceData.Character_Controller.enabled = true;

        DisableCanvas();
    }
}
