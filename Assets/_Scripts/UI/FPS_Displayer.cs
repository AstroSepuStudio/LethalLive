using TMPro;
using UnityEngine;

public class FPS_Displayer : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI fpsDisplay;
    [SerializeField] bool lockFPS;
    [SerializeField] int targetFPS;

    private void Start()
    {
        GameTick.OnTick += OnTick;

        if (lockFPS)
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = targetFPS;
        }
    }

    private void OnDestroy()
    {
        GameTick.OnTick -= OnTick;
    }

    private void OnTick()
    {
        fpsDisplay.SetText($"FPS: {Mathf.Round(1/Time.deltaTime)}");
    }
}
