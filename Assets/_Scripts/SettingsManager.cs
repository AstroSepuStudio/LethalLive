using System.IO;
using UnityEngine;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance;
    public Settings UserSettings;
    string SettingsPath => Path.Combine(Application.persistentDataPath, "UserSettings.json");

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            LoadSettings();
        }
    }

    private void OnDestroy()
    {
        SaveSettings();
    }

    public void SaveSettings()
    {
        string json = JsonUtility.ToJson(UserSettings, true);
        File.WriteAllText(SettingsPath, json);
    }

    public void LoadSettings()
    {
        if (!File.Exists(SettingsPath))
        {
            UserSettings = new Settings();
            SaveSettings();
            return;
        }

        string json = File.ReadAllText(SettingsPath);
        UserSettings = JsonUtility.FromJson<Settings>(json);
    }

    public void LockMouse()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void UnlockMouse()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
