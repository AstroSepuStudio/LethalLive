using Mirror;

public class Int_HomewardBeacon : InteractableObject
{
    [Server]
    public void GoBackToOffice(PlayerData sourceData)
    {
        sourceData.Character_Controller.enabled = false;
        sourceData.Character_Controller.transform.position = GameManager.Instance.transform.position;
        sourceData.Character_Controller.enabled = true;
        sourceData._PlayerInOffice = true;

        DisableCanvas();
        GameManager.Instance.dngMod.OnReturnOffice(sourceData);
    }
}
