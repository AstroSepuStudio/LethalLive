using LethalLive;
using Steamworks;
using System.Collections;
using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    [Header("References")]
    [SerializeField] PlayerData pData;
    [SerializeField] Transform lookTarget;
    [SerializeField] Camera pCamera;
    [SerializeField] AudioListener audioListener;
    [SerializeField] GameObject crosshair;

    [Header("Settings")]
    [SerializeField] float minVertical = -80f;
    [SerializeField] float maxVertical = 80f;
    [SerializeField] float zoomSensitivity = 1f;
    [SerializeField] float minZoom = 1f;
    [SerializeField] float maxZoom = 6f;
    [SerializeField] float smoothTime = 0.2f;
    [SerializeField] float offsetSpeed = 5f;

    Vector3 velocity = Vector3.zero;
    float distanceToTarget = 4f;
    float horizontal;
    float vertical;
    bool _stop;
    bool _cameraInputLocked;

    private void Start()
    {
        pData.CameraPivot.SetParent(null);
        crosshair.SetActive(false);

        LobbyManager.Instance.OnLobbyLeaveEvent.AddListener(DestroyGameobject);
        LobbyManager.Instance.OnLobbyKickedEvent.AddListener(DestroyGameobject);
    }

    private void OnDestroy()
    {
        LobbyManager.Instance.OnLobbyLeaveEvent.AddListener(DestroyGameobject);
        LobbyManager.Instance.OnLobbyKickedEvent.RemoveListener(DestroyGameobject);
    }

    private void OnEnable()
    {
        Vector3 back = -pData.CameraPivot.forward;
        pData.PlayerCamera.transform.position = pData.CameraPivot.position + back * distanceToTarget;

        if (Physics.Linecast(pData.CameraPivot.position, pData.PlayerCamera.transform.position + back * 0.12f, out RaycastHit hit, pData.IgnorePlayer))
        {
            Vector3 safePos = pData.CameraPivot.position + back * (hit.distance - 0.12f);
            pData.PlayerCamera.transform.position = safePos;
        }
    }

    void DestroyGameobject() => Destroy(gameObject);
    void DestroyGameobject(LobbyKicked_t arg0) => Destroy(gameObject);

    private void LateUpdate()
    {
        if (pData == null) return;

        pData.CameraPivot.position = Vector3.SmoothDamp(
            pData.CameraPivot.position,
            pData.CameraTarget.position,
            ref velocity,
            smoothTime);
        
        pCamera.transform.localRotation = Quaternion.identity;

        if (pData._LockPlayer || Cursor.lockState == CursorLockMode.None) return;
        if (!pData.isLocalPlayer || _stop || pData.HUDManager.OpenedWindow) return;
        if (_cameraInputLocked) return;

        Vector2 camOffset = pData.Player_Input.actions["AdjustCam"].ReadValue<Vector2>();
        float xOffset = Mathf.Clamp(pData.CameraTarget.localPosition.x + camOffset.x * Time.deltaTime * offsetSpeed, -0.6f, 0.6f);
        float yOffset = Mathf.Clamp(pData.CameraTarget.localPosition.y + camOffset.y * Time.deltaTime * offsetSpeed, 0.8f, 1.6f);
        pData.CameraTarget.localPosition = new(xOffset, yOffset, 0f);

        float scrollInput = pData.Player_Input.actions["Zoom"].ReadValue<float>();
        distanceToTarget = Mathf.Clamp(distanceToTarget - scrollInput * zoomSensitivity * Time.deltaTime, minZoom, maxZoom);

        Vector2 mouseDelta = pData.Player_Input.actions["Look"].ReadValue<Vector2>();
        float mouseX = mouseDelta.x * SettingsManager.Instance.UserSettings.GetSensitivity() * Time.deltaTime;
        float mouseY = mouseDelta.y * SettingsManager.Instance.UserSettings.GetSensitivity() * Time.deltaTime;

        horizontal += mouseX;
        vertical -= mouseY;
        vertical = Mathf.Clamp(vertical, minVertical, maxVertical);

        Quaternion targetRotation = Quaternion.Euler(vertical, horizontal, 0f);
        pData.CameraPivot.rotation = targetRotation;

        Vector3 back = -pData.CameraPivot.forward;
        pData.PlayerCamera.transform.position = pData.CameraPivot.position + back * distanceToTarget;

        if (Physics.Linecast(pData.CameraPivot.position, pData.PlayerCamera.transform.position + back * 0.12f, out RaycastHit hit, pData.IgnorePlayer))
        {
            Vector3 safePos = pData.CameraPivot.position + back * (hit.distance - 0.12f);
            pData.PlayerCamera.transform.position = safePos;
        }

        pData.CmdSetCameraData(horizontal, vertical, distanceToTarget, pData.CameraTarget.localPosition.x, pData.CameraTarget.localPosition.y);
    }

    public void PauseCamera() => _stop = true;
    public void ResumeCamera() => _stop = false;

    public void LockCameraInput() => _cameraInputLocked = true;
    public void UnlockCameraInput() => _cameraInputLocked = false;

    public void ForcePlayerToAimDirection(Vector3 worldDirection)
    {
        Vector3 dir = worldDirection;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return;
        StopAllCoroutines();
        StartCoroutine(SmoothAimRotation(dir.normalized));
    }

    public void ServerClearForcedAim() => StopAllCoroutines();

    private IEnumerator SmoothAimRotation(Vector3 targetForward)
    {
        Quaternion startRotation = Quaternion.LookRotation(pData.transform.forward);
        Quaternion targetRotation = Quaternion.LookRotation(targetForward);

        float duration = 0.1f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            pData.transform.rotation = Quaternion.Slerp(startRotation, targetRotation, elapsed / duration);
            yield return null;
        }

        pData.transform.rotation = targetRotation;
    }

    public void StartTeleport()
    {
        pCamera.enabled = false;
        StartCoroutine(TeleportCoroutine());
    }
    
    IEnumerator TeleportCoroutine()
    {
        yield return new WaitForSeconds(0.2f);
        pCamera.enabled = true;
        velocity = Vector3.zero;
    }
}
