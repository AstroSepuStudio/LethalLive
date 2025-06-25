using System.Collections;
using UnityEngine;

public class RiggingManager : MonoBehaviour
{
    [SerializeField] PlayerData pData;
    [SerializeField] float maxAngle = 80;
    [SerializeField] float transitionSpeed = 5f;

    Coroutine camRigTransition;
    Coroutine camTargetRigTransition;

    private void Start()
    {
        GameTick.Subscribe(OnTick);
    }

    private void OnDestroy()
    {
        GameTick.Unsubscribe(OnTick);
    }

    void OnTick()
    {
        bool isCameraInFront = IsInFront(pData.PlayerCamera.transform);
        bool isCameraTargetInFront = IsInFront(pData.LookCameraTarget);

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
        while (!Mathf.Approximately(pData.FollowCameraRig.weight, targetWeight))
        {
            pData.FollowCameraRig.weight = Mathf.MoveTowards(pData.FollowCameraRig.weight, targetWeight, Time.deltaTime * transitionSpeed);
            yield return null;
        }

        pData.FollowCameraRig.weight = targetWeight;
        camRigTransition = null;
    }

    IEnumerator CamTargetTransition(float targetWeight)
    {
        while (!Mathf.Approximately(pData.FollowCameraTargetRig.weight, targetWeight))
        {
            pData.FollowCameraTargetRig.weight = Mathf.MoveTowards(pData.FollowCameraTargetRig.weight, targetWeight, Time.deltaTime * transitionSpeed);
            yield return null;
        }

        pData.FollowCameraTargetRig.weight = targetWeight;
        camTargetRigTransition = null;
    }
}
