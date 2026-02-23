using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using LethalLive;

public class GraphicsSettingsManager : MonoBehaviour
{
    #region - Types -
    public enum ScreenMode
    {
        Borderless,
        Fullscreen,
        Windowed
    }

    [Serializable]
    public struct ResolutionOption
    {
        public int width;
        public int height;
        public int refreshRate;
    }
    #endregion

    #region - References -
    [Header("UI")]
    [SerializeField] TMP_Dropdown resolutionsDropdown;
    [SerializeField] TMP_InputField targetFpsIF;
    #endregion

    #region - Settings State - 
    [Header("Window")]
    [SerializeField] ScreenMode currentScreenMode;
    [SerializeField] int currentResolutionIndex;
    [SerializeField] List<ResolutionOption> availableResolutions;

    [Header("Visual")]
    [SerializeField] bool vsyncEnabled;
    [SerializeField] int targetFPS = -1;
    [SerializeField] bool postProcessingEnabled = true;
    #endregion

    Settings UserSettings => SettingsManager.Instance.UserSettings;

    private void Start()
    {
        BuildResolutionList();
        LoadSettings();
        ApplyAllSettings();
    }

    #region - Resolution -
    void BuildResolutionList()
    {
        availableResolutions = new List<ResolutionOption>();
        resolutionsDropdown.ClearOptions();

        var dropdownOptions = new List<string>();
        var seen = new HashSet<string>();

        foreach (Resolution res in Screen.resolutions)
        {
            int refreshRate = Mathf.RoundToInt((float)res.refreshRateRatio.value);
            string key = $"{res.width}x{res.height}@{refreshRate}";

            if (!seen.Add(key))
                continue;

            availableResolutions.Add(new ResolutionOption
            {
                width = res.width,
                height = res.height,
                refreshRate = refreshRate
            });
        }

        if (availableResolutions.Count == 0)
        {
            availableResolutions.Add(new ResolutionOption
            {
                width = Screen.currentResolution.width,
                height = Screen.currentResolution.height,
                refreshRate = Screen.currentResolution.refreshRate
            });
        }

        availableResolutions.Sort((a, b) =>
        {
            int w = b.width.CompareTo(a.width);
            if (w != 0) return w;

            int h = b.height.CompareTo(a.height);
            if (h != 0) return h;

            return b.refreshRate.CompareTo(a.refreshRate);
        });

        foreach (var res in availableResolutions)
            dropdownOptions.Add($"{res.width} x {res.height} @ {res.refreshRate}Hz");

        resolutionsDropdown.AddOptions(dropdownOptions);
        resolutionsDropdown.value = GetDefaultResolutionIndex();
        resolutionsDropdown.RefreshShownValue();
    }
    #endregion

    #region - Setters -
    void SetResolution(int index)
    {
        if (index < 0 || index >= availableResolutions.Count)
            return;

        currentResolutionIndex = index;
        ApplyResolution();
        SaveSettings();
    }

    void SetScreenMode(ScreenMode mode)
    {
        currentScreenMode = mode;
        ApplyResolution();
        SaveSettings();
    }

    void SetVSync(bool enabled)
    {
        vsyncEnabled = enabled;
        ApplyVSyncAndFPS();
        SaveSettings(); 
    }

    void SetTargetFPS(int fps)
    {
        if (fps <= 0)
        {
            fps = 60;
            targetFpsIF.SetTextWithoutNotify("60");
        }

        targetFPS = fps;
        ApplyVSyncAndFPS();
        SaveSettings();
    }

    void SetPostProcessing(bool enabled)
    {
        postProcessingEnabled = enabled;
        ApplyPostProcessing();
        SaveSettings();
    }
    #endregion

    #region - Apply Methods -
    void ApplyAllSettings()
    {
        ApplyResolution();
        ApplyVSyncAndFPS();
        ApplyPostProcessing();
    }

    void ApplyResolution()
    {
        var res = availableResolutions[currentResolutionIndex];

        Screen.SetResolution(
            res.width,
            res.height,
            ConvertScreenMode(currentScreenMode),
            res.refreshRate
        );
    }

    void ApplyVSyncAndFPS()
    {
        if (vsyncEnabled)
        {
            QualitySettings.vSyncCount = 1;
            Application.targetFrameRate = -1;
        }
        else
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = targetFPS <= 0 ? -1 : targetFPS;
        }
    }

    void ApplyPostProcessing()
    {
        Camera cam = null;

        if (GameManager.Instance != null)
            if (GameManager.Instance.playMod.LocalPlayer != null)
                cam = GameManager.Instance.playMod.LocalPlayer.PlayerCamera;
        
        if (cam == null)
            cam = Camera.main;

        if (cam == null)
        {
            Debug.LogWarning("[Settings - PostProcessing] No camera was found, couldn't apply saved settings");
            return;
        }

        cam.GetUniversalAdditionalCameraData().renderPostProcessing = postProcessingEnabled;
    }
    #endregion

    #region - Persistence -
    void SaveSettings()
    {
        UserSettings.SetResolutionIndex(currentResolutionIndex);
        UserSettings.SetScreenModeIndex((int)currentScreenMode);
        UserSettings.SetVsync(vsyncEnabled);
        UserSettings.SetTargetFPS(targetFPS);
        UserSettings.SetPostProcessing(postProcessingEnabled);
        SettingsManager.Instance.SaveSettings();
    }

    void LoadSettings()
    {
        currentResolutionIndex =
            UserSettings.GetResolutionIndex();

        currentScreenMode =
            (ScreenMode)UserSettings.GetScreenModeIndex();

        vsyncEnabled = UserSettings.GetVsyncEnabled();

        targetFPS = UserSettings.GetTargetFPS();

        postProcessingEnabled = UserSettings.GetPostProcessingEnabled();
    }
    #endregion

    #region - Helpers -
    int GetDefaultResolutionIndex()
    {
        Resolution current = Screen.currentResolution;

        for (int i = 0; i < availableResolutions.Count; i++)
        {
            if (availableResolutions[i].width == current.width &&
                availableResolutions[i].height == current.height)
                return i;
        }

        return Mathf.Max(0, availableResolutions.Count - 1);
    }

    FullScreenMode ConvertScreenMode(ScreenMode mode)
    {
        return mode switch
        {
            ScreenMode.Fullscreen => FullScreenMode.ExclusiveFullScreen,
            ScreenMode.Borderless => FullScreenMode.FullScreenWindow,
            _ => FullScreenMode.Windowed
        };
    }
    #endregion

    #region - UI Callbacks -
    public void OnResolutionChanged(int value) => SetResolution(value);
    public void OnScreenModeChanged(int value) => SetScreenMode((ScreenMode)value);
    public void OnVSyncChanged(bool enabled) => SetVSync(enabled);
    public void OnPostProcessingChanged(bool enabled) => SetPostProcessing(enabled);
    public void OnTargetFPSChanged(string fps) => SetTargetFPS(int.Parse(fps));
    #endregion
}
