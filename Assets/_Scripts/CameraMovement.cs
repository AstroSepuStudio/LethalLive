using System.Collections;
using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    [Header("References")]
    [SerializeField] PlayerData pData;
    [SerializeField] Transform lookTarget;
    [SerializeField] Camera pCamera;
    [SerializeField] AudioListener audioListener;

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

    private void Start()
    {
        pData.CameraPivot.SetParent(null);

        if (!pData.isLocalPlayer)
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

        if (!pData.isLocalPlayer || _stop) return;

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
            float blend = Mathf.MoveTowards(pData.Skin_Data.SkinMaterial.GetFloat("_Tweak_transparency"), -1, Time.deltaTime * 6f);
            pData.Skin_Data.SkinMaterial.SetFloat("_Tweak_transparency", blend);

            if (Mathf.Approximately(blend, -1))
            {
                pData.Skin_Data.SkinRenderer.enabled = false;
            }
        }
        else
        {
            if (pData.Skin_Data.SkinMaterial != null)
            {
                float blend = Mathf.MoveTowards(pData.Skin_Data.SkinMaterial.GetFloat("_Tweak_transparency"), 0, Time.deltaTime * 6f);
                pData.Skin_Data.SkinMaterial.SetFloat("_Tweak_transparency", blend);
                pData.Skin_Data.SkinRenderer.enabled = true;
            }
        }

        pData.CmdSetCameraData(horizontal, vertical, distanceToTarget);
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
