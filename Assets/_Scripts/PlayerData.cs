using UnityEngine;
using UnityEngine.InputSystem;
using Mirror;

public class PlayerData : NetworkBehaviour
{
    [Header("Input & Core Systems")]
    public PlayerInput Player_Input;
    public PlayerMovement Player_Movement;
    public PunchManager Punch_Manager;
    public PlayerStats Player_Stats;
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

    [SyncVar(hook = nameof(OnCameraHorizChanged))] float syncedHoriz;
    [SyncVar(hook = nameof(OnCameraVertChanged))] float syncedVert;
    [SyncVar(hook = nameof(OnCameraDistanceChanged))] float syncedDistance;

    [Header("Canvas")]
    public GameObject PlayerCanvas;

    private void Start()
    {
        if (isLocalPlayer)
        {
            PlayerAudio.enabled = true;
        }
        else
        {
            PlayerCanvas.SetActive(false);
            PlayerAudio.enabled = false;
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
}
