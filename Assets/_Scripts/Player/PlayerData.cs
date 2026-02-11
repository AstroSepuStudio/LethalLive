using UnityEngine;
using UnityEngine.InputSystem;
using Mirror;
using Steamworks;
using System.Collections;
using UnityEngine.Events;

public enum PlayerTeam { White, Red, Blue, Yellow, Green, Pink }

public class PlayerData : NetworkBehaviour
{
    [Header("Input & Core Systems")]
    public PlayerInput Player_Input;
    public PlayerMovement Player_Movement;
    public SpectatorMovement Spectator_Movement;
    public PunchManager Punch_Manager;
    public PlayerStats Player_Stats;
    public ItemInventory PlayerInventory;
    public SkinManager Skin_Manager;
    public HUD_Manager HUDManager;
    public TabletManager TabletManager;
    public AudioSource Quiet_AS;
    public AudioSource Modest_AS;
    public AudioSource Loud_AS;
    public VoiceChatHandler VCHandler;
    public DeathOverlayManager DeathOvManager;
    public UnityEvent<PlayerTeam> OnPlayerTeamChanged;
    public UnityEvent<int, ChatMessage> OnReceiveChatMessage;
    [SerializeField] NetworkTransformHybrid netTransform;
    [SerializeField] float tpDelay = 0.5f;

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

    [Header("Audio")]
    public float VoiceChatVolume = 1;

    [SyncVar(hook = nameof(OnCameraPivotChanged))] 
    float syncedHoriz;
    [SyncVar(hook = nameof(OnCameraPivotChanged))] 
    float syncedVert;
    [SyncVar(hook = nameof(OnCameraPivotChanged))] 
    float syncedDistance;
    [SyncVar(hook = nameof(OnCameraPivotChanged))]
    float syncedHorizOffset = 0.3f;
    [SyncVar(hook = nameof(OnCameraPivotChanged))]
    float syncedVertOffset = 1.4f;

    [SyncVar]
    public int Index = -1;

    [SyncVar]
    public int Ping;

    [SyncVar]
    public PlayerTeam Team;

    [SyncVar]
    public bool _LockPlayer = false;

    [Header("Canvas")]
    public GameObject PlayerCanvas;

    private void Start()
    {
        if (isLocalPlayer)
        {
            PlayerAudio.enabled = true;
            //SettingsManager.Instance.LockMouse();
            GameManager.Instance.playMod.LocalPlayer = this;
        }
        else
        {
            PlayerCanvas.SetActive(false);
            PlayerAudio.enabled = false;
            PlayerCamera.enabled = false;
        }
    }

    [TargetRpc]
    public void RPC_OnPlayerTeamChanged(PlayerTeam team) => OnPlayerTeamChanged?.Invoke(team);

    [TargetRpc]
    public void Rpc_ReceiveChatMessage(int channelIndex, ChatMessage chatMessage)
    {
        OnReceiveChatMessage?.Invoke(channelIndex, chatMessage);
    }

    public void SetLockPlayer(bool locked) => _LockPlayer = locked;

    public override void OnStartServer()
    {
        base.OnStartServer();
        GameManager.Instance.playMod.RegisterPlayer(this);
    }

    public override void OnStopServer()
    {
        GameManager.Instance.playMod.UnregisterPlayer(this);
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

        GameManager.Instance.lobbyManagerScreen.RefreshScreen();
    }

    [Command]
    public void CmdSetCameraData(float h, float v, float dist, float hO, float vO)
    {
        syncedHoriz = h;
        syncedVert = v;
        syncedDistance = dist;
        syncedHorizOffset = hO;
        syncedVertOffset = vO;
    }

    void OnCameraPivotChanged(float _, float __) => ApplyCameraRotation();

    void ApplyCameraRotation()
    {
        if (isLocalPlayer) return;

        CameraTarget.localPosition = new(syncedHorizOffset, syncedVertOffset, 0f);

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
        if (!isLocalPlayer || HUDmanager.OpenedWindow) return;

        if (Spectator_Movement.enabled)
        {
            Spectator_Movement.PrimaryAction(context);
            return;
        }

        if (PlayerInventory.HasPrimaryAction)
        {
            PlayerInventory.PrimaryInput(context);
        }
        else if (!PlayerInventory.HasTwoHandedEquipped && context.started)
        {
            Punch_Manager.OnPunchInput();
        }
    }

    public void OnClientSecondary(InputAction.CallbackContext context)
    {
        if (!isLocalPlayer || HUDmanager.OpenedWindow) return;

        if (Spectator_Movement.enabled)
        {
            Spectator_Movement.SecondaryAction(context);
            return;
        }

        if (PlayerInventory.HasSecondaryAction)
        {
            PlayerInventory.SecondaryInput(context);
        }
    }

    [Server]
    public void OnPlayerDeath(AttackStat stat, Vector3 momentum)
    {
        GameManager.Instance.playMod.PlayerDies(netId);
        Skin_Data.Ragdoll_Manager.EnableRagdoll(momentum);
        Rpc_OnPlayerDeath();
    }

    [TargetRpc]
    void Rpc_OnPlayerDeath()
    {
        Player_Movement.enabled = false;
        Spectator_Movement.enabled = true;
        DeathOvManager.EnableOverlay();
    }

    [Server]
    public void RevivePlayer(Vector3 position)
    {
        Player_Stats.ResetStats();
        Skin_Data.Ragdoll_Manager.DisableRagdoll();
        Teleport(position);

        Rpc_RevivePlayer();
    }

    [TargetRpc]
    public void Rpc_RevivePlayer()
    {
        Spectator_Movement.enabled = false;
        Player_Movement.enabled = true;
        DeathOvManager.DisableOverlay();
    }

    [Server]
    public void Teleport(Vector3 position)
    {
        StartCoroutine(TeleportCoroutine(position));
    }

    IEnumerator TeleportCoroutine(Vector3 position)
    {
        PlayerCollider.enabled = false;
        Character_Controller.enabled = false;
        netTransform.ServerTeleport(position, transform.rotation);
        //transform.position = position;
        float timer = 0;
        while (timer < tpDelay)
        {
            //netTransform.ServerTeleport(position, transform.rotation);
            timer += Time.deltaTime;
            yield return null;
        }

        PlayerCollider.enabled = true;
        Character_Controller.enabled = true;
    }
}
