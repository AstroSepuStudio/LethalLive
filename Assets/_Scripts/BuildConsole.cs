using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BuildConsole : MonoBehaviour
{
    public static BuildConsole Instance;

    [SerializeField] GameObject canvas;
    [SerializeField] ScrollRect scrollRect;
    [SerializeField] GameObject messagePrefab;
    [SerializeField] Transform content;
    public bool autoScroll = true;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
            Destroy(gameObject);
    }

    public void EnableConsole() => canvas.SetActive(true);

    public void DisableConsole() => canvas.SetActive(false);

    private void Update()
    {
        if (autoScroll)
        {
            scrollRect.verticalNormalizedPosition = 0f;
        }
    }

    public void SendConsoleMessage(string message)
    {
        GameObject instance = Instantiate(messagePrefab, content);
        instance.GetComponentInChildren<TextMeshProUGUI>().SetText(message);
    }
}
