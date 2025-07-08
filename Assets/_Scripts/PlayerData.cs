using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Animations.Rigging;
using Mirror;
using System.Collections.Generic;

public class PlayerData : NetworkBehaviour
{
    [Header("Input & Core Systems")]
    public PlayerInput Player_Input;
    public PlayerMovement Player_Movement;
    public CameraMovement Camera_Movement;
    public PunchManager Punch_Manager;
    public PlayerStats Player_Stats;

    [Header("Physics")]
    public CharacterController Character_Controller;
    public Collider PlayerCollider;
    public LayerMask IgnorePlayer;
    public LayerMask PlayerMask;
    public RagdollManager Ragdoll_Manager;

    [Header("Visuals")]
    public Transform Model;
    public Transform RightHand;
    public Renderer ModelRenderer;
    public Material ModelMaterial;
    public Animator CharacterAnimator;
    public EmoteWheelManager EmoteManager;
    public Rig FollowCameraTargetRig;
    public Rig FollowCameraRig;
    public HUD_Manager HUDmanager;

    [Header("Camera")]
    public Transform Head;
    public Transform CameraTarget;
    public Transform CameraPivot;
    public Transform LookCameraTarget;
    public Camera PlayerCamera;
    public AudioListener PlayerAudio;
    public bool _IsPlayerAimLocked;

    [Header("Canvas")]
    public GameObject PlayerCanvas;

    private void Start()
    {
        ModelMaterial = new Material(ModelMaterial);
        ModelRenderer.material = ModelMaterial;

        if (!isLocalPlayer)
        {
            PlayerCanvas.SetActive(false);
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
}
