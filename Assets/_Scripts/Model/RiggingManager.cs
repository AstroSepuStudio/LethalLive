using System.Collections;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class RiggingManager : MonoBehaviour
{
    [SerializeField] SkinData skinData;
    [SerializeField] float maxAngle = 80;
    [SerializeField] float transitionSpeed = 5f;
    [SerializeField] RigBuilder rigBuilder;
    [SerializeField] Rig FollowCameraTargetRig;
    [SerializeField] Rig FollowCameraRig;
    [SerializeField] Rig TwoHandedItemRig;

    Coroutine camRigTransition;
    Coroutine camTargetRigTransition;
    Coroutine twoHandedRigTransition;
    float twoHandedBlend;

    private void Start()
    {
        GameTick.OnTick += OnTick;
        //rigBuilder.Build();
    }

    private void OnDestroy()
    {
        GameTick.OnTick -= OnTick;
    }

    void OnTick()
    {
        if (skinData.pData.Skin_Data != skinData) return;

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

    public void EnableTwoHandedRig()
    {
        if (twoHandedRigTransition != null)
            StopCoroutine(twoHandedRigTransition);

        twoHandedRigTransition = StartCoroutine(SmoothBlendTwoHandedRig(1));
    }

    public void DisableTwoHandedRig()
    {
        if (twoHandedRigTransition != null)
            StopCoroutine(twoHandedRigTransition);

        twoHandedRigTransition = StartCoroutine(SmoothBlendTwoHandedRig(0));
    }

    IEnumerator SmoothBlendTwoHandedRig(float target)
    {
        target = Mathf.Clamp01(target);

        while (!Mathf.Approximately(twoHandedBlend, target))
        {
            twoHandedBlend = Mathf.MoveTowards(twoHandedBlend, target, Time.deltaTime * transitionSpeed);
            TwoHandedItemRig.weight = twoHandedBlend;
            yield return null;
        }

        twoHandedBlend = target;
        TwoHandedItemRig.weight = twoHandedBlend;
    }
}
