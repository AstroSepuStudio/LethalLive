using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

public class SkinManager : NetworkBehaviour
{
    [SerializeField] PlayerData pData;
    [SerializeField] SkinData[] skinsData;
    [SerializeField] GameObject skinSelectionWindow;

    [SyncVar(hook = nameof(OnSkinIndexChanged))]
    int skinIndex = 0;

    //[SerializeField] SkinSO[] skins;
    //GameObject currentModel;

    public void SkinInput(InputAction.CallbackContext context)
    {
        if (HUD_Manager.OpenWindows.Contains(skinSelectionWindow))
            CloseWindow();
        else
            OpenWindow();
    }

    void OpenWindow()
    {
        if (HUD_Manager.OpenWindows.Count > 0) return;
        HUD_Manager.OpenWindows.Add(skinSelectionWindow);

        skinSelectionWindow.SetActive(true);
    }

    public void CloseWindow()
    {
        if (HUD_Manager.OpenWindows.Contains(skinSelectionWindow))
            HUD_Manager.OpenWindows.Remove(skinSelectionWindow);

        skinSelectionWindow.SetActive(false);
    }

    public void SetSkinIndex(int index)
    {
        if (!isLocalPlayer) return;
        CmdRequestSkinChange(index);
    }

    [Command]
    void CmdRequestSkinChange(int index)
    {
        skinIndex = index;
    }

    void OnSkinIndexChanged(int oldIndex, int newIndex)
    {
        ApplySkin(newIndex);
    }

    void ApplySkin(int index)
    {
        index = Mathf.Clamp(index, 0, skinsData.Length - 1);

        SkinData skin = skinsData[index];
        if (skin == null) return;

        pData.Skin_Data.gameObject.SetActive(false);
        skin.gameObject.SetActive(true);
        pData.Skin_Data = skin;

        //var skin = skins[index];
        //if (skin == null) return;

        //if (currentModel != null)
        //{
        //    if (pData.Skin_Data != null)
        //        Destroy(pData.CameraPivot.gameObject);
        //    Destroy(currentModel);
        //}

        //currentModel = Instantiate(skin.modelPrefab, pData.Model);
        //pData.Skin_Data = currentModel.GetComponent<SkinData>();
        //pData.Skin_Data.pData = pData;
        //Debug.Log($"{netId} changed its skin to {skin.skinName} (index: {index})");
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        ApplySkin(skinIndex);
    }
}
