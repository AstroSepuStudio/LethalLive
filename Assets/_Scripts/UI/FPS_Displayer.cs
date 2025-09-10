using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class FPS_Displayer : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI fpsDisplay;
    [SerializeField] bool lockFPS;
    [SerializeField] int targetFPS;

    List<float> fpsPtick = new();

    float GetAverage()
    {
        float av = 0;
        foreach (var fps in fpsPtick)
        {
            av += fps;
        }

        return av / fpsPtick.Count;
    }

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
        float fps = 1 / Time.deltaTime;
        fpsPtick.Add(fps);

        fpsDisplay.SetText($"{Mathf.Round(fps)} FPS \n" +
            $"{GetAverage()} Av.");
    }
}
