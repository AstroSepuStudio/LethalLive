using Mirror;
using System.Collections;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class RiggingManager : NetworkBehaviour
{
    [SerializeField] SkinData skinData;
    [SerializeField] float maxAngle = 80;
    [SerializeField] float transitionSpeed = 5f;
    [SerializeField] RigBuilder rigBuilder;
    [SerializeField] Rig FollowCameraTargetRig;
    [SerializeField] Rig FollowCameraRig;
    [SerializeField] Rig RightHandChainRig;
    [SerializeField] Transform RHCRTarget;

    Coroutine camRigTransition;
    Coroutine camTargetRigTransition;

    Transform RHCRTargetTarget;
    public bool StopCameraRigs { get; private set; }

    private void OnDestroy()
    {
        GameTick.OnTick -= OnTick;
    }

    private void OnDisable()
    {
        StopCameraRigs = false;
        RHCRTargetTarget = null;
        RightHandChainRig.weight = 0f;
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
    public void RpcDisableRightHandChainRig()
    {
        RHCRTargetTarget = null;
        RightHandChainRig.weight = 0f;
        StopCameraRigs = false;
    }
}
