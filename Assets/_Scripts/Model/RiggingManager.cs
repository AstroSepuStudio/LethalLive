using Mirror;
using System.Collections;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class RiggingManager : NetworkBehaviour
{
    [SerializeField] SkinData skinData;
    [SerializeField] TabletManager tabletManager;

    [SerializeField] float maxAngle = 80;
    [SerializeField] float transitionSpeed = 5f;
    [SerializeField] RigBuilder rigBuilder;
    [SerializeField] Rig FollowCameraTargetRig;
    [SerializeField] Rig FollowCameraRig;
    [SerializeField] Rig RightHandChainRig;
    [SerializeField] Rig LeftHandChainRig;
    [SerializeField] Rig LookAtTabletChainRig;
    [SerializeField] Transform RHCRTarget;
    [SerializeField] Transform LHCRTarget;
    [SerializeField] Transform customTarget;

    Coroutine camRigTransition;
    Coroutine camTargetRigTransition;
    Coroutine leftHandRigTransition;
    Coroutine lookAtTabletRigTransition;

    Transform RHCRTargetTarget;
    Transform LHCRTargetTarget;

    public bool StopCameraRigs { get; private set; }

    private void OnDestroy()
    {
        GameTick.OnTick -= OnTick;
    }

    private void OnDisable()
    {
        StopCameraRigs = false;
        RHCRTargetTarget = null;
        LHCRTargetTarget = null;
        RightHandChainRig.weight = 0f;
        LeftHandChainRig.weight = 0f;
        LookAtTabletChainRig.weight = 0f;
        leftHandRigTransition = null;
        lookAtTabletRigTransition = null;
        GameTick.OnTick -= OnTick;
    }

    public void SetUp(bool RHCR)
    {
        GameTick.OnTick += OnTick;

        if (RHCR)
        {
            StopCameraRigs = true;
            RHCRTargetTarget = GameManager.Instance.lobbyManagerScreen.rightHCIKTarget;
            RightHandChainRig.weight = 1f;
        }
    }

    private void Update()
    {
        if (RHCRTargetTarget != null)
            RHCRTarget.position = RHCRTargetTarget.position;

        if (LHCRTargetTarget != null)
            LHCRTarget.position = LHCRTargetTarget.position;
    }

    void OnTick()
    {
        if (skinData.pData.Skin_Data != skinData || StopCameraRigs) return;

        bool isCameraInFront = IsInFront(skinData.pData.PlayerCamera.transform);
        bool isCameraTargetInFront = IsInFront(skinData.pData.LookCameraTarget);

        if (isCameraInFront)
        {
            if (camRigTransition != null) StopCoroutine(camRigTransition);
            if (camTargetRigTransition != null) StopCoroutine(camTargetRigTransition);

            camRigTransition = StartCoroutine(CamTransition(1f));
            camTargetRigTransition = StartCoroutine(CamTargetTransition(0f));
        }
        else if (isCameraTargetInFront)
        {
            if (camRigTransition != null) StopCoroutine(camRigTransition);
            if (camTargetRigTransition != null) StopCoroutine(camTargetRigTransition);

            camRigTransition = StartCoroutine(CamTransition(0f));
            camTargetRigTransition = StartCoroutine(CamTargetTransition(1f));
        }
        else if (!isCameraInFront && !isCameraTargetInFront)
        {
            if (camRigTransition != null) StopCoroutine(camRigTransition);
            if (camTargetRigTransition != null) StopCoroutine(camTargetRigTransition);

            camRigTransition = StartCoroutine(CamTransition(0f));
            camTargetRigTransition = StartCoroutine(CamTargetTransition(0f));
        }
    }

    bool IsInFront(Transform obj)
    {
        Vector3 toTarget = (obj.position - transform.position).normalized;
        float dot = Vector3.Dot(transform.forward, toTarget);
        float angle = Mathf.Acos(dot) * Mathf.Rad2Deg;

        return angle <= maxAngle;
    }

    IEnumerator CamTransition(float targetWeight)
    {
        while (!Mathf.Approximately(FollowCameraRig.weight, targetWeight))
        {
            FollowCameraRig.weight = Mathf.MoveTowards(FollowCameraRig.weight, targetWeight, Time.deltaTime * transitionSpeed);
            yield return null;
        }

        FollowCameraRig.weight = targetWeight;
        camRigTransition = null;
    }

    IEnumerator CamTargetTransition(float targetWeight)
    {
        while (!Mathf.Approximately(FollowCameraTargetRig.weight, targetWeight))
        {
            FollowCameraTargetRig.weight = Mathf.MoveTowards(FollowCameraTargetRig.weight, targetWeight, Time.deltaTime * transitionSpeed);
            yield return null;
        }

        FollowCameraTargetRig.weight = targetWeight;
        camTargetRigTransition = null;
    }

    [ClientRpc]
    public void RpcEnableRightHandChainRig()
    {
        RightHandChainRig.weight = 1f;
        RHCRTargetTarget = GameManager.Instance.lobbyManagerScreen.rightHCIKTarget;

        FollowCameraRig.weight = 0;
        FollowCameraTargetRig.weight = 0;
        StopCameraRigs = true;
    }

    [ClientRpc]
    public void RpcAnimateRightHandChainRig(Vector3 initialPos, Vector3 finalPos, float duration)
    {
        StartCoroutine(AnimateRightHandChainRigCor(initialPos, finalPos, duration));
    }

    public void AnimateRightHandChainRig(Vector3 initialPos, Vector3 finalPos, float duration)
    {
        StartCoroutine(AnimateRightHandChainRigCor(initialPos, finalPos, duration));
    }

    IEnumerator AnimateRightHandChainRigCor(Vector3 initialPos, Vector3 finalPos, float duration)
    {
        RightHandChainRig.weight = 1f;
        customTarget.position = initialPos;
        RHCRTargetTarget = customTarget;

        FollowCameraRig.weight = 0;
        FollowCameraTargetRig.weight = 0;
        StopCameraRigs = true;

        float timer = 0;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            customTarget.position = Vector3.Lerp(initialPos, finalPos, timer / duration);
            yield return null;
        }

        customTarget.position = finalPos;

        RHCRTargetTarget = null;
        RightHandChainRig.weight = 0f;
        StopCameraRigs = false;
    }

    [ClientRpc]
    public void RpcDisableRightHandChainRig()
    {
        RHCRTargetTarget = null;
        RightHandChainRig.weight = 0f;
        StopCameraRigs = false;
    }

    public void EnableLeftHandChainRig(bool enable)
    {
        if (enable && LeftHandChainRig.weight > 0.5f ||
            !enable && LeftHandChainRig.weight < 0.5f) return;
        CmdEnableLeftHandChainRig(enable);
    }

    [Command]
    void CmdEnableLeftHandChainRig(bool enable) => RpcEnableLeftHandChainRig(enable);

    [ClientRpc]
    public void RpcEnableLeftHandChainRig(bool enable)
    {
        if (enable)
            LHCRTargetTarget = tabletManager.leftHandIKTarget;
        else
            LHCRTargetTarget = null;

        if (leftHandRigTransition != null) StopCoroutine(leftHandRigTransition);
        leftHandRigTransition = StartCoroutine(LeftHandTransition(enable ? 1f : 0f));
    }

    public void EnableLookAtTabletRig(bool enable)
    {
        if (enable && LookAtTabletChainRig.weight > 0.5f ||
            !enable && LookAtTabletChainRig.weight < 0.5f) return;
        CmdEnableLookAtTabletRig(enable);
    }

    [Command] void CmdEnableLookAtTabletRig(bool enable) => RpcEnableLookAtTabletRig(enable);

    [ClientRpc] public void RpcEnableLookAtTabletRig(bool enable)
    {
        if (lookAtTabletRigTransition != null) StopCoroutine(lookAtTabletRigTransition);
        lookAtTabletRigTransition = StartCoroutine(LookAtTabletTransition(enable ? 1f : 0f));
    }

    IEnumerator LeftHandTransition(float targetWeight)
    {
        while (!Mathf.Approximately(LeftHandChainRig.weight, targetWeight))
        {
            LeftHandChainRig.weight = Mathf.MoveTowards(LeftHandChainRig.weight, targetWeight, Time.deltaTime * transitionSpeed);
            yield return null;
        }

        LeftHandChainRig.weight = targetWeight;
        leftHandRigTransition = null;
    }

    IEnumerator LookAtTabletTransition(float targetWeight)
    {
        while (!Mathf.Approximately(LookAtTabletChainRig.weight, targetWeight))
        {
            LookAtTabletChainRig.weight = Mathf.MoveTowards(LookAtTabletChainRig.weight, targetWeight, Time.deltaTime * transitionSpeed);
            yield return null;
        }

        LookAtTabletChainRig.weight = targetWeight;
        lookAtTabletRigTransition = null;
    }

    [ClientRpc]
    public void RpcSetEmoteIK(bool disabled)
    {
        if (disabled)
        {
            if (camRigTransition != null) StopCoroutine(camRigTransition);
            if (camTargetRigTransition != null) StopCoroutine(camTargetRigTransition);

            camRigTransition = StartCoroutine(CamTransition(0f));
            camTargetRigTransition = StartCoroutine(CamTargetTransition(0f));
            StopCameraRigs = true;
        }
        else
        {
            if (!RightHandChainRig.weight.Equals(1f))
                StopCameraRigs = false;
        }
    }
}
