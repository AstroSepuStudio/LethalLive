using UnityEngine;
using UnityEngine.Events;

public class WindowUI : MonoBehaviour
{
    public bool _overlay = false;
    [SerializeField] UnityEvent OnWindowOpen;
    [SerializeField] UnityEvent OnWindowClose;

    public void OpenWindow()
    {
        gameObject.SetActive(true);
        OnWindowOpen?.Invoke();
    }

    public void CloseWindow()
    {
        gameObject.SetActive(false);
        OnWindowClose?.Invoke();
    }
}
