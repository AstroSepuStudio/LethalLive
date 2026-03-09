using LethalLive;
using Steamworks;
using System.Collections;
using UnityEngine;

public class MainMenuManager : UIManager
{
    [Header("References")]
    [SerializeField] GameObject cameraParent;
    [SerializeField] GameObject menuScene;
    [SerializeField] GameObject office;
    [SerializeField] Transform menuCam;

    [Header("Animation")]
    [SerializeField] Transform initialPosition;
    [SerializeField] Transform resetPosition;
    [SerializeField] float menuCamSpd = 4f;
    [SerializeField] float distanceToReset = 1f;

    Coroutine menuCamCor;

    protected override void Start()
    {
        base.Start();

        LobbyManager.Instance.OnLobbyCreatedEvent.AddListener(HideMainMenu);
        LobbyManager.Instance.OnLobbyJoinedEvent.AddListener(HideMainMenu);

        LobbyManager.Instance.OnLobbyLeaveEvent.AddListener(DisplayMainMenu);
        LobbyManager.Instance.OnLobbyKickedEvent.AddListener(DisplayMainMenu);

        SwitchCameraAnimation(true);
    }

    private void HideMainMenu(LobbyEnter_t arg0)
    {
        gameObject.SetActive(false);
        cameraParent.SetActive(false);
        if (menuScene != null) menuScene.SetActive(false);
        if (office != null) office.SetActive(true);
        SwitchCameraAnimation(false);
    }

    private void HideMainMenu(LobbyCreated_t arg0)
    {
        gameObject.SetActive(false);
        cameraParent.SetActive(false);
        if (menuScene != null) menuScene.SetActive(false);
        if (office != null) office.SetActive(true);
        SwitchCameraAnimation(false);
    }

    private void DisplayMainMenu()
    {
        gameObject.SetActive(true);
        cameraParent.SetActive(true);
        if (menuScene != null) menuScene.SetActive(true);
        if (office != null) office.SetActive(false);

        SettingsManager.Instance.UnlockMouse();
        SwitchCameraAnimation(true);
    }

    private void DisplayMainMenu(LobbyKicked_t arg0)
    {
        gameObject.SetActive(true);
        cameraParent.SetActive(true);
        if (menuScene != null) menuScene.SetActive(true);
        if (office != null) office.SetActive(false);

        SettingsManager.Instance.UnlockMouse();
        SwitchCameraAnimation(true);
    }

    public void QuitApplication()
    {
        Application.Quit();
    }

    private void SwitchCameraAnimation(bool start)
    {
        if (menuCamCor != null)
        {
            StopCoroutine(menuCamCor);
            if (!start) return;
        }

        menuCamCor = StartCoroutine(MenuCamAnimation());
    }

    IEnumerator MenuCamAnimation()
    {
        menuCam.position = initialPosition.position;
        Vector3 movDir = (resetPosition.position - initialPosition.position).normalized;
        float cycleLength = Vector3.Distance(initialPosition.position, resetPosition.position);
        float travelled = 0f;
        WaitForEndOfFrame frame = new();

        while (true)
        {
            yield return frame;
            float step = menuCamSpd * Time.deltaTime;
            travelled += step;

            if (travelled >= cycleLength)
                travelled -= cycleLength;

            menuCam.position = initialPosition.position + movDir * travelled;
        }
    }
}
