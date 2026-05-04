using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class UIManager : MonoBehaviour
{
    [SerializeField] WindowUI defaultWindow;
    private Stack<WindowUI> openWindows = new();

    [SerializeField] UIButton currentBtn;

    protected virtual void Start()
    {
        if (defaultWindow != null)
            OpenWindow(defaultWindow);

        if (currentBtn != null)
            currentBtn.OnButtonSelected();
    }

    public virtual void OnCancelInput(InputAction.CallbackContext context)
    {
        if (!context.started) return;

        CloseTopWindow();
    }

    public virtual void OpenWindow(WindowUI window)
    {
        if (window == null) return;

        if (openWindows.Count > 0 && !window._overlay)
            openWindows.Peek().CloseWindow();

        window.OpenWindow();
        openWindows.Push(window);
    }

    public virtual void SelectButton(UIButton btn)
    {
        if (currentBtn != null)
            currentBtn.OnButtonDeselected();

        btn.OnButtonSelected();
        currentBtn = btn;
    }

    public virtual void CloseTopWindow()
    {
        if (openWindows.Count == 0) return;

        WindowUI top = openWindows.Pop();
        top.CloseWindow();

        if (top._overlay) return;

        if (openWindows.Count > 0)
            openWindows.Peek().OpenWindow();
        else
            defaultWindow.OpenWindow();
    }
}
