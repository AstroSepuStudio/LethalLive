using Mirror;

public class Int_HomewardBeacon : InteractableObject
{
    [Server]
    public void GoBackToOffice(PlayerData sourceData)
    {
        sourceData.Character_Controller.enabled = false;
        sourceData.Character_Controller.transform.position = GameManager.Instance.transform.position;
        sourceData.Character_Controller.enabled = true;

        DisableCanvas();

        AudioManager.Instance.StopMusic();
    }
}
