using Mirror;
using System;
using System.Collections;
using UnityEngine;

public class CameraMovement : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] PlayerData pData;

    [Header("Settings")]
    [SerializeField] float sensitivity = 9f;
    [SerializeField] float minVertical = -80f;
    [SerializeField] float maxVertical = 80f;
    [SerializeField] float zoomSensitivity = 1f;
    [SerializeField] float minZoom = 1f;
    [SerializeField] float maxZoom = 6f;
    [SerializeField] float smoothTime = 0.2f;

    Vector3 velocity = Vector3.zero;
    float distanceToTarget = 4f;
    float horizontal;
    float vertical;
    bool _stop;

    public struct CameraData
    {
        public float horiz;
        public float vert;
        public float distance;
    }

    [SyncVar(hook = nameof(OnCameraDataChanged))]
    CameraData syncedCameraData;

    private void Start()
    {
        pData.CameraPivot.SetParent(null);

        if (!isLocalPlayer)
        {
            pData.PlayerAudio.enabled = false;
            pData.PlayerCamera.enabled = false;
        }
    }

    private void LateUpdate()
    {
        if (pData == null) return;

        pData.CameraPivot.position = Vector3.SmoothDamp(
            pData.CameraPivot.position,
            pData.CameraTarget.position,
            ref velocity,
            smoothTime);

        if (!isLocalPlayer || _stop) return;

        float scrollInput = pData.Player_Input.actions["Zoom"].ReadValue<float>();
        distanceToTarget = Mathf.Clamp(distanceToTarget - scrollInput * zoomSensitivity * Time.deltaTime, minZoom, maxZoom);

        Vector2 mouseDelta = pData.Player_Input.actions["Look"].ReadValue<Vector2>();
        float mouseX = mouseDelta.x * sensitivity / 10;
        float mouseY = mouseDelta.y * sensitivity / 10;

        horizontal += mouseX;
        vertical -= mouseY;
        vertical = Mathf.Clamp(vertical, minVertical, maxVertical);

        Quaternion targetRotation = Quaternion.Euler(vertical, horizontal, 0f);
        pData.CameraPivot.rotation = targetRotation;

        //Vector3 dir = (pData.PlayerCamera.transform.position - pData.CameraPivot.position).normalized;
        Vector3 back = -pData.CameraPivot.forward;
        pData.PlayerCamera.transform.position = pData.CameraPivot.position + back * distanceToTarget;
        
        if (Physics.Linecast(pData.CameraPivot.position, pData.PlayerCamera.transform.position + back * 0.12f, out RaycastHit hit, pData.IgnorePlayer))
        {
            Vector3 safePos = pData.CameraPivot.position + back * (hit.distance - 0.12f);
            pData.PlayerCamera.transform.position = safePos;
        }

        if (Vector3.Distance(pData.PlayerCamera.transform.position, pData.CameraPivot.position) <= 0.5f ||
            Vector3.Distance(pData.PlayerCamera.transform.position, pData.Head.position) <= 0.5f ||
            Vector3.Distance(pData.PlayerCamera.transform.position, pData.transform.position) <= 0.5f)
        {
            float blend = Mathf.MoveTowards(pData.ModelMaterial.GetFloat("_Tweak_transparency"), -1, Time.deltaTime * 6f);
            pData.ModelMaterial.SetFloat("_Tweak_transparency", blend);

            if (Mathf.Approximately(blend, -1))
            {
                pData.ModelRenderer.enabled = false;
            }
        }
        else
        {
            float blend = Mathf.MoveTowards(pData.ModelMaterial.GetFloat("_Tweak_transparency"), 0, Time.deltaTime * 6f);
            pData.ModelMaterial.SetFloat("_Tweak_transparency", blend);
            pData.ModelRenderer.enabled = true;
        }

        CmdSetCameraData(horizontal, vertical, distanceToTarget);
    }

    [Command]
    void CmdSetCameraData(float h, float v, float dist)
    {
        syncedCameraData = new CameraData { horiz = h, vert = v, distance = dist };
    }

    void OnCameraDataChanged(CameraData oldData, CameraData newData)
    {
        if (isLocalPlayer) return;

        Quaternion targetRotation = Quaternion.Euler(newData.vert, newData.horiz, 0f);
        pData.CameraPivot.rotation = targetRotation;

        Vector3 dir = (pData.PlayerCamera.transform.position - pData.CameraPivot.position).normalized;
        pData.PlayerCamera.transform.position = pData.CameraPivot.position + dir * newData.distance;

        if (Physics.Linecast(pData.CameraPivot.position, pData.PlayerCamera.transform.position + dir * 0.5f, out RaycastHit hit, pData.IgnorePlayer))
        {
            Vector3 safePos = pData.CameraPivot.position + dir * (hit.distance - 0.5f);
            pData.PlayerCamera.transform.position = safePos;
        }
    }

    public void PauseCamera()
    {
        _stop = true;
    }

    public void ResumeCamera()
    {
        _stop = false;
    }

    public void ForcePlayerToAim()
    {
        StartCoroutine(SmoothAimRotation());
    }

    private IEnumerator SmoothAimRotation()
    {
        Vector3 camForward = pData.CameraPivot.forward;
        camForward.y = 0f;
        camForward.Normalize();

        Vector3 startForward = pData.transform.forward;
        Quaternion startRotation = Quaternion.LookRotation(startForward);
        Quaternion targetRotation = Quaternion.LookRotation(camForward);

        pData._IsPlayerAimLocked = true;

        float duration = 0.1f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            pData.transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
            yield return null;
        }

        pData.transform.rotation = targetRotation;
    }

    public void StopForcePlayerToAim()
    {
        pData._IsPlayerAimLocked = false;
    }
}
