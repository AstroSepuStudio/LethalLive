using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] PlayerData pData;

    [Header("Ground Detection")]
    [SerializeField] Transform feet;
    [SerializeField] float groundRadius;
    [SerializeField] float raycastLenght = 0.2f;

    [Header("Movement")]
    [SerializeField] float gravity;
    [SerializeField] float groundedGravity = -5f;
    [SerializeField] float coyoteTime = 0.2f;
    [SerializeField] float walkSpeed = 5f;
    [SerializeField] float sprintSpeed = 9f;
    [SerializeField] float crouchSpeed = 2f;
    [SerializeField] float movSpeed = 5f;
    [SerializeField] float rotationSpeed = 10f;
    [SerializeField] float animTransitionSpeed = 1f;
    [SerializeField] float speedMultiplier = 1f;
    [SerializeField] float friction = 3f;
    Vector3 externalMomentum = Vector3.zero;

    [Header("Crouching")]
    [SerializeField] float crouchHeight = 1f;
    [SerializeField] Vector3 crouchCenter = new(0f, 0.5f, 0f);
    [SerializeField] float normalHeight = 2f;
    [SerializeField] Vector3 normalCenter = new(0f, 1f, 0f);

    [Header("Jumping")]
    [SerializeField] float jumpForce;

    Vector2 movementInput;
    Vector3 velocity;
    [SerializeField] Collider[] res;
    bool IsGrounded()
    {
        res = Physics.OverlapSphere(feet.position, groundRadius);
        for (int i = 0; i < res.Length; i++)
        {
            if (res[i] != pData.PlayerCollider) return true;
        }
        if (Physics.Raycast(feet.position + Vector3.down * groundRadius, Vector3.down, raycastLenght, pData.PlayerMask)) return true;

        return false;
    }
    bool IsSomethingAbove() => Physics.CheckSphere(pData.Head.position, groundRadius, pData.IgnorePlayer);

    // --- FLAGS --- //
    [Header("Flags")]
    [SerializeField] float groundedTime;
    [SerializeField] float jumpTime;
    [SerializeField] bool _isGrounded;
    [SerializeField] bool _tryJump;
    [SerializeField] bool _isSprinting = false;
    public bool IsCrouching { get; private set; } = false;
    [SerializeField] bool _wantsToCrouch = false;
    [SerializeField] bool _wantsToSprint = false;
    [SerializeField] bool _wantsToUncrouch = false;

    [Header("Network Variables")]
    [SyncVar(hook = nameof(OnStandMovBlendChanged))]
    float standMovBlend;
    [SyncVar(hook = nameof(OnCrouchMovBlendChanged))]
    float crouchMovBlend;
    [SyncVar(hook = nameof(OnStandCrouchChanged))]
    float standCrouchBlend;

    private void Start()
    {
        normalCenter = pData.Character_Controller.center;
        normalHeight = pData.Character_Controller.height;

        crouchCenter = normalCenter * 0.5f;
        crouchHeight = normalHeight * 0.5f;

        movSpeed = walkSpeed;

        pData.CameraTarget.localPosition = new Vector3(0, normalHeight - 0.1f, 0);
    }

    #region Synvars
    void OnStandMovBlendChanged(float oldValue, float newValue)
    {
        if (pData.Skin_Data.CharacterAnimator != null)
            pData.Skin_Data.CharacterAnimator.SetFloat("StandMov", newValue);
    }

    void OnCrouchMovBlendChanged(float oldValue, float newValue)
    {
        if (pData.Skin_Data.CharacterAnimator != null)
            pData.Skin_Data.CharacterAnimator.SetFloat("CrouchMov", newValue);
    }

    void OnStandCrouchChanged(float oldValue, float newValue)
    {
        if (pData.Skin_Data.CharacterAnimator != null)
            pData.Skin_Data.CharacterAnimator.SetFloat("StandCrouch", newValue);
    }
    #endregion

    #region Input Methods
    public void OnMove(InputAction.CallbackContext context)
    {
        if (!isLocalPlayer) return;

        Vector2 input = context.ReadValue<Vector2>();
        CmdSendMovementInput(input);
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (!isLocalPlayer) return;
        if (context.canceled) return;

        CmdSendJumpInput();
    }

    public void OnStartSprint(InputAction.CallbackContext context)
    {
        if (!isLocalPlayer) return;

        if (context.started)
            CmdStartSprint();
        else if (context.canceled)
            CmdStopSprint();
    }

    public void OnStartCrouch(InputAction.CallbackContext context)
    {
        if (!isLocalPlayer) return;

        if (context.started)
            CmdStartCrouch();
        else if (context.canceled)
            CmdStopCrouch();
    }
    #endregion

    #region Commands
    [Command]
    void CmdSendMovementInput(Vector2 input) => movementInput = input;

    [Command]
    void CmdSendJumpInput() => jumpTime = 0.2f;

    [Command]
    void CmdStartSprint()
    {
        if (IsCrouching)
        {                
            if (IsSomethingAbove())
            {
                _wantsToSprint = true;
                return;
            }
            else
            {
                ServerStopCrouch();
                _wantsToCrouch = true;
            }
        }

        _isSprinting = true;
        movSpeed = sprintSpeed;
    }

    [Command]
    void CmdStopSprint()
    {
        if (IsCrouching && IsSomethingAbove())
        {
            _wantsToSprint = false;
            return;
        }

        _isSprinting = false;
        movSpeed = walkSpeed;

        if (_wantsToCrouch)
        {
            ServerStartCrouch();
            _wantsToCrouch = false;
        }
    }

    [Command]
    void CmdStartCrouch() => ServerStartCrouch();

    [Command]
    void CmdStopCrouch() => ServerStopCrouch();
    #endregion

    #region Helpers
    void ServerStartCrouch()
    {
        if (_isSprinting)
        {
            _wantsToCrouch = true;
            return;
        }

        StartCrouch();
        RpcStartCrouch();
    }

    void ServerStopCrouch()
    {
        if (_isSprinting)
        {
            _wantsToCrouch = false;
            return;
        }

        if (IsSomethingAbove())
        {
            _wantsToUncrouch = true;
            return;
        }

        StopCrouch();
        RpcStopCrouch();
    }

    [ClientRpc]
    void RpcStartCrouch()
    {
        StartCrouch();
    }

    [ClientRpc]
    void RpcStopCrouch()
    {
        StopCrouch();
    }

    void StartCrouch()
    {
        IsCrouching = true;
        pData.Character_Controller.height = crouchHeight;
        pData.Character_Controller.center = crouchCenter;
        pData.CameraTarget.localPosition = new Vector3(0, crouchHeight - 0.1f, 0);
    }

    void StopCrouch()
    {
        IsCrouching = false;
        pData.Character_Controller.height = normalHeight;
        pData.Character_Controller.center = normalCenter;
        pData.CameraTarget.localPosition = new Vector3(0, normalHeight - 0.1f, 0);
    }
    #endregion

    void Update()
    {
        if (!isServer) return;
        if (pData.Skin_Data.Ragdoll_Manager.IsKnocked || pData.CameraPivot == null) return;

        UpdateMoveSpeed();

        if (_wantsToUncrouch || _wantsToSprint)
        {
            if (!IsSomethingAbove())
            {
                ServerStopCrouch();
                if (_wantsToSprint)
                {
                    _isSprinting = true;
                    movSpeed = sprintSpeed;
                }

                _wantsToSprint = false;
                _wantsToUncrouch = false;
            }
        }

        groundedTime = IsGrounded() ? coyoteTime : groundedTime > 0 ? groundedTime - Time.deltaTime : 0;
        _isGrounded = groundedTime > 0f;

        jumpTime = jumpTime > 0 ? jumpTime - Time.deltaTime : 0;
        _tryJump = jumpTime > 0f;

        if (_isGrounded && _tryJump)
        {
            velocity.y = jumpForce;
            jumpTime = 0f; _tryJump = false;
            groundedTime = 0f; _isGrounded = false;
        }

        if (_isGrounded && velocity.y <= 0)
            velocity.y = groundedGravity;
        else
            velocity.y += gravity * Time.deltaTime;

        Vector3 camForward = pData.CameraPivot.forward;
        Vector3 camRight = pData.CameraPivot.right;
        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 move = (camForward * movementInput.y + camRight * movementInput.x).normalized;
        if (!Mathf.Approximately(move.magnitude, 0) && !pData._IsPlayerAimLocked)
        {
            Quaternion targetRotation = Quaternion.LookRotation(move);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            pData.EmoteManager.PlayerMoves();
        }
        float multiplier = speedMultiplier < 0 ? 0 : speedMultiplier;
        move *= movSpeed * multiplier * (pData.Player_Stats.speed / 100f);

        velocity.x = move.x + externalMomentum.x;
        velocity.z = move.z + externalMomentum.z;
        pData.Character_Controller.Move(velocity * Time.deltaTime);

        externalMomentum = Vector3.Lerp(externalMomentum, Vector3.zero, Time.deltaTime * friction);

        float speed = new Vector2(velocity.x, velocity.z).magnitude;
        if (pData.Skin_Data.CharacterAnimator == null) return;
        if (IsCrouching)
        {
            standCrouchBlend = Mathf.MoveTowards(pData.Skin_Data.CharacterAnimator.GetFloat("StandCrouch"), 1, Time.deltaTime * animTransitionSpeed);
            float targetBlend = Mathf.Approximately(speed, 0f) ? 0f : speed;
            crouchMovBlend = Mathf.MoveTowards(crouchMovBlend, targetBlend, Time.deltaTime * animTransitionSpeed);
        }
        else
        {
            standCrouchBlend = Mathf.MoveTowards(pData.Skin_Data.CharacterAnimator.GetFloat("StandCrouch"), 0, Time.deltaTime * animTransitionSpeed);
            float targetBlend = Mathf.Approximately(speed, 0f) ? 0f : speed;
            standMovBlend = Mathf.MoveTowards(standMovBlend, targetBlend, Time.deltaTime * animTransitionSpeed);
        }
    }

    void UpdateMoveSpeed()
    {
        if (IsCrouching)
            movSpeed = crouchSpeed;
        else if (_isSprinting)
            movSpeed = sprintSpeed;
        else
            movSpeed = walkSpeed;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;

        Gizmos.DrawWireSphere(feet.transform.position, groundRadius);
        Gizmos.DrawWireSphere(pData.Head.transform.position, groundRadius);
        Gizmos.DrawLine(feet.position + Vector3.down * groundRadius, feet.position + Vector3.down * groundRadius + Vector3.down * raycastLenght);
    }

    public void ChangeSpeedMultiplier(float delta)
    {
        speedMultiplier += delta;
    }

    public void AddMomentum(Vector3 force)
    {
        externalMomentum += force;
    }

    public Vector3 KillMomentum()
    {
        Vector3 momentum = externalMomentum;
        externalMomentum = Vector3.zero;
        return momentum;
    }
}
