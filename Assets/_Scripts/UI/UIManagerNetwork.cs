using Mirror;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class UIManagerNetwork : NetworkBehaviour
{
    [SerializeField] WindowUI defaultWindow;
    [SerializeField] WindowUI[] registeredWindows;

    [SerializeField] UIButton currentBtn;
    [SerializeField] UIButton[] registeredCurrentBtn;

    private Stack<WindowUI> openWindows = new();

    [SyncVar(hook = nameof(OnSyncStackChanged))]
    string _syncStack = "";

    [SyncVar(hook = nameof(OnSyncButtonChanged))]
    int _syncButtonIdx = -1;

    [Command(requiresAuthority = false)] void CmdSyncStack(string stack) => _syncStack = stack;

    [Command(requiresAuthority = false)] void CmdSyncButton(int idx) => _syncButtonIdx = idx;

    void OnSyncStackChanged(string oldVal, string newVal)
    {
        if (isOwned) return;
        ApplyStackFromSync(newVal);
    }

    void OnSyncButtonChanged(int oldVal, int newVal)
    {
        if (isOwned) return;
        if (newVal < 0 || newVal >= registeredCurrentBtn.Length) return;
        ApplyButtonFromSync(registeredCurrentBtn[newVal]);
    }

    private void PushSync()
    {
        if (!isClient) return;

        var indexes = new System.Text.StringBuilder();
        foreach (var w in openWindows)
        {
            int idx = System.Array.IndexOf(registeredWindows, w);
            if (idx < 0) continue;
            if (indexes.Length > 0) indexes.Append(',');
            indexes.Append(idx);
        }
        CmdSyncStack(indexes.ToString());
    }

    private void ApplyStackFromSync(string stack)
    {
        foreach (var w in openWindows) w.CloseWindow();
        openWindows.Clear();

        if (string.IsNullOrEmpty(stack)) return;

        var parts = stack.Split(',');
        for (int i = parts.Length - 1; i >= 0; i--)
        {
            if (!int.TryParse(parts[i], out int idx)) continue;
            if (idx < 0 || idx >= registeredWindows.Length) continue;

            var w = registeredWindows[idx];
            if (w == null) continue;

            if (openWindows.Count > 0 && !w._overlay)
                openWindows.Peek().CloseWindow();

            w.OpenWindow();
            openWindows.Push(w);
        }
    }

    private void ApplyButtonFromSync(UIButton btn)
    {
        if (currentBtn != null) currentBtn.OnButtonDeselected();
        btn.OnButtonSelected();
        currentBtn = btn;
    }

    protected virtual void Start()
    {
        if (defaultWindow != null)
            OpenWindow(defaultWindow);
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

        PushSync();
    }

    public virtual void SelectButton(UIButton btn)
    {
        if (currentBtn != null) currentBtn.OnButtonDeselected();
        btn.OnButtonSelected();
        currentBtn = btn;

        int idx = System.Array.IndexOf(registeredCurrentBtn, btn);
        if (idx >= 0 && isClient) CmdSyncButton(idx);
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

        PushSync();
    }
}
