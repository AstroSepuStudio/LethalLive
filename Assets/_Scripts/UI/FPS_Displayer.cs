using TMPro;
using UnityEngine;

public class FPS_Displayer : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI fpsDisplay;
    [SerializeField] bool displayFPS;
    [SerializeField] bool displayAv;

    public bool DisplayFPS => displayFPS;
    public bool DisplayAv => displayAv;

    readonly float[] fpsPtick = new float[20];
    int index = 0;

    private void Start()
    {
        if (!displayFPS && !displayAv) 
            fpsDisplay.enabled = false;
    }

    float GetAverage()
    {
        float av = 0;
        foreach (var fps in fpsPtick)
        {
            av += fps;
        }
        av /= fpsPtick.Length;
        av = Mathf.Round(av * 100) / 100;

        return av;
    }

    public void SetDisplayFPS(bool display)
    {
        displayFPS = display;
        if (displayFPS) OnTick();
        if (displayAv) return;

        fpsDisplay.enabled = display;
        if (display) GameTick.OnTick += OnTick;
        else GameTick.OnTick -= OnTick;
    }

    public void SetDisplayAverageFPS(bool display)
    {
        displayAv = display;
        if (displayAv) OnTick();
        if (displayFPS) return;

        fpsDisplay.enabled = display;
        if (display) GameTick.OnTick += OnTick;
        else GameTick.OnTick -= OnTick;
    }

    private void OnTick()
    {
        float fps = 1 / Time.deltaTime;
        fpsPtick[index] = fps;
        index++;
        if (index >= fpsPtick.Length) index = 0;

        string display = "";

        if (displayFPS) display += $"{Mathf.Round(fps)}\n";
        if (displayAv) display += $"~{GetAverage()}";

        fpsDisplay.SetText(display);
    }
}
