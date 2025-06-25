using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Animations.Rigging;
using Mirror;

public class PlayerData : NetworkBehaviour
{
    [Header("Input & Core Systems")]
    public PlayerInput Player_Input;
    public PlayerMovement Player_Movement;
    public CameraMovement Camera_Movement;

    [Header("Physics")]
    public CharacterController Character_Controller;
    public Collider PlayerCollider;
    public LayerMask IgnorePlayer;
    public LayerMask PlayerMask;

    [Header("Visuals")]
    public Transform Model;
    public Renderer ModelRenderer;
    public Material ModelMaterial;
    public Animator CharacterAnimator;
    public EmoteWheelManager EmoteManager;
    public Rig FollowCameraTargetRig;
    public Rig FollowCameraRig;

    [Header("Camera")]
    public Transform Head;
    public Transform CameraTarget;
    public Transform CameraPivot;
    public Transform LookCameraTarget;
    public Camera PlayerCamera;
    public AudioListener PlayerAudio;

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
}
