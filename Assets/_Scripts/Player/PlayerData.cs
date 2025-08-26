using UnityEngine;
using UnityEngine.InputSystem;
using Mirror;
using Steamworks;
using System.Collections;

public enum PlayerTeam { Hololive, Gamers, HoloX, English };

public class PlayerData : NetworkBehaviour
{
    [Header("Input & Core Systems")]
    public PlayerInput Player_Input;
    public PlayerMovement Player_Movement;
    public PunchManager Punch_Manager;
    public PlayerStats Player_Stats;
    public ItemInventory PlayerInventory;
    public InteractonDetection Item_PickUp;
    public PlayerInputHandler InputHandler;
    public bool _LockPlayer = false;
    public HUD_Manager HUDManager;
    public AudioSource Quiet_AS;
    public AudioSource Modest_AS;
    public AudioSource Loud_AS;

    [Header("Physics")]
    public CharacterController Character_Controller;
    public Collider PlayerCollider;
    public LayerMask IgnorePlayer;
    public LayerMask PlayerMask;

    [Header("Visuals")]
    public Transform Model;
    public EmoteWheelManager EmoteManager;
    public HUD_Manager HUDmanager;
    public SkinData Skin_Data;

    [Header("Camera")]
    public Transform Head;
    public Transform CameraTarget;
    public bool _IsPlayerAimLocked;
    public Transform CameraPivot;
    public Transform LookCameraTarget;
    public CameraMovement Camera_Movement;
    public Camera PlayerCamera;
    public AudioListener PlayerAudio;

    [Header("Steam")]
    public CSteamID SteamID;
    public string PlayerName;
    public byte[] AvatarData;

    [SyncVar(hook = nameof(OnCameraHorizChanged))] float syncedHoriz;
    [SyncVar(hook = nameof(OnCameraVertChanged))] float syncedVert;
    [SyncVar(hook = nameof(OnCameraDistanceChanged))] float syncedDistance;

    [SyncVar]
    public int Index = -1;

    [SyncVar]
    public int Ping;

    [SyncVar]
    public PlayerTeam Team;

    [Header("Canvas")]
    public GameObject PlayerCanvas;

    private void Start()
    {
        if (isLocalPlayer)
        {
            PlayerAudio.enabled = true;
            SettingsManager.Instance.LockMouse();
            GameManager.Instance.LocalPlayer = this;
        }
        else
        {
            PlayerCanvas.SetActive(false);
            PlayerAudio.enabled = false;
            PlayerCamera.enabled = false;
        }
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        GameManager.Instance.RegisterPlayer(this);
    }

    public override void OnStopServer()
    {
        GameManager.Instance.UnregisterPlayer(this);
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();

        StartCoroutine(SendSteamDataToServer());
    }

    private IEnumerator SendSteamDataToServer()
    {
        string name = SteamFriends.GetPersonaName();

        int avatar_id;
        float timeout = 5f;
        float timer = 0f;

        while ((avatar_id = SteamFriends.GetMediumFriendAvatar(SteamUser.GetSteamID())) == -1 && timer < timeout)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        if (avatar_id == -1) yield break;

        Debug.Log("avatar id got: " + avatar_id);
        byte[] avatarData = null;

        uint width = 0, height = 0;
        bool success = false;
        timer = 0f;

        while (!success && timer < timeout)
        {
            success = SteamUtils.GetImageSize(avatar_id, out width, out height) && width > 0 && height > 0;
            timer += Time.deltaTime;
            yield return null;
        }

        if (!success)
        {
            Debug.LogWarning("Failed to get avatar image size within timeout.");
            yield break;
        }

        Debug.Log($"Got image size: {width}x{height}");

        byte[] image = new byte[width * height * 4];
        success = false;
        timer = 0f;

        while (!success && timer < timeout)
        {
            success = SteamUtils.GetImageRGBA(avatar_id, image, image.Length);
            timer += Time.deltaTime;
            yield return null;
        }

        if (success)
        {
            Debug.Log("Got image RGBA data");

            Texture2D avatar = new Texture2D((int)width, (int)height, TextureFormat.RGBA32, false, true);
            avatar.LoadRawTextureData(image);
            avatar.Apply();

            avatarData = avatar.EncodeToPNG();
        }
        else Debug.LogWarning("Failed to get avatar image data within timeout.");

        CmdSendSteamInfo(SteamUser.GetSteamID(), name, avatarData);
    }


    [Command]
    private void CmdSendSteamInfo(CSteamID steamID, string name, byte[] avatarData)
    {
        SteamID = steamID;
        PlayerName = name;
        AvatarData = avatarData;
    }

    [Command]
    public void CmdSetCameraData(float h, float v, float dist)
    {
        syncedHoriz = h;
        syncedVert = v;
        syncedDistance = dist;
    }

    void OnCameraHorizChanged(float _, float h) => ApplyCameraRotation();
    void OnCameraVertChanged(float _, float v) => ApplyCameraRotation();
    void OnCameraDistanceChanged(float _, float d) => ApplyCameraRotation();

    void ApplyCameraRotation()
    {
        if (isLocalPlayer) return;

        Quaternion targetRotation = Quaternion.Euler(syncedVert, syncedHoriz, 0f);
        CameraPivot.rotation = targetRotation;

        Vector3 dir = -CameraPivot.forward;
        PlayerCamera.transform.position = CameraPivot.position + dir * syncedDistance;

        if (Physics.Linecast(CameraPivot.position, PlayerCamera.transform.position + dir * 0.5f, out RaycastHit hit, IgnorePlayer))
        {
            Vector3 safePos = CameraPivot.position + dir * (hit.distance - 0.5f);
            PlayerCamera.transform.position = safePos;
        }
    }

    public void OnClientPrimary(InputAction.CallbackContext context)
    {
        if (!isLocalPlayer || !context.started || HUDmanager.OpenedWindow) return;

        if (PlayerInventory.HasPrimaryAction)
        {
            PlayerInventory.PrimaryInput();
        }
        else if (!PlayerInventory.HasTwoHandedEquipped)
        {
            Punch_Manager.OnPunchInput();
        }
    }

    public void OnClientSecondary(InputAction.CallbackContext context)
    {
        if (!isLocalPlayer || !context.started || HUDmanager.OpenedWindow) return;

        if (PlayerInventory.HasSecondaryAction)
        {
            PlayerInventory.SecondaryInput();
        }
    }
}
