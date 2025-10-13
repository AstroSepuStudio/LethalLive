using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

public class SkinManager : NetworkBehaviour
{
    [SerializeField] PlayerData pData;
    [SerializeField] SkinData[] skinsData;
    [SerializeField] GameObject skinSelectionWindow;
    [SerializeField] bool work = false;

    int skinIndex = 0;
    bool _opened = false;

    // Rig State
    [SyncVar] bool RHCR = false;

    private void Start()
    {
        for (int i = 0; i < skinsData.Length; i++)
        {
            if (i != 0)
                skinsData[i].gameObject.SetActive(false);
        }
    }

    public void SkinInput(InputAction.CallbackContext context)
    {
        if (!work) return;
        if (!pData.isLocalPlayer) return;

        if (_opened)
            CloseWindow();
        else
            OpenWindow();
    }

    void OpenWindow()
    {
        if (pData.HUDManager.OpenedWindows > 0) return;
        pData.HUDManager.OpenWindow(skinSelectionWindow);
        _opened = true;

        skinSelectionWindow.SetActive(true);
    }

    public void CloseWindow()
    {
        pData.HUDManager.CloseWindow(skinSelectionWindow);
        _opened = false;

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
        if (index >= skinsData.Length || index < 0)
        {
            Debug.LogWarning("Given skin index is invalid");
            return;
        }

        skinIndex = index;
        RHCR = pData.Skin_Data.Rigging_Manager.StopCameraRigs;

        RpcApplySkin(index, RHCR);
    }

    [ClientRpc]
    void RpcApplySkin(int index, bool rhcr)
    {
        ApplySkin(index, rhcr);
    }

    void ApplySkin(int index, bool rhcr)
    {
        index = Mathf.Clamp(index, 0, skinsData.Length - 1);

        SkinData skin = skinsData[index];
        if (skin == null) return;

        pData.Skin_Data.gameObject.SetActive(false);
        skin.Rigging_Manager.SetUp(rhcr);
        skin.gameObject.SetActive(true);
        pData.Skin_Data = skin;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        ApplySkin(skinIndex, false);
    }
}
