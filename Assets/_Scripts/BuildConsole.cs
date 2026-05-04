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
    [SerializeField] bool debug = false;

    public bool autoScroll = true;

    bool sus = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        if (debug)
        {
            GameTick.OnTick += OnTick;
            sus = true;
        }
    }

    private void OnDestroy()
    {
        if (sus)
        {
            GameTick.OnTick -= OnTick;
        }
    }

    public void EnableConsole() => canvas.SetActive(true);

    public void DisableConsole() => canvas.SetActive(false);

    private void OnTick()
    {
        if (!debug && sus)
        {
            GameTick.OnTick -= OnTick;
            sus = false;
            return;
        }

        if (autoScroll)
        {
            scrollRect.verticalNormalizedPosition = 0f;
        }
    }

    public void SendConsoleMessage(string message)
    {
        if (!debug) return;

        GameObject instance = Instantiate(messagePrefab, content);
        instance.GetComponentInChildren<TextMeshProUGUI>().SetText(message);
    }
}
