using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class KeybindDisplay : MonoBehaviour
{
    [Header("Input")]
    public InputActionAsset inputActionAsset;
    [Tooltip("Format: 'ActionMap/ActionName', e.g. 'Player/Jump'")]
    public string actionPath = "Player/Jump";
    [Tooltip("Leave empty to use the first active binding")]
    public string controlScheme = "";

    [Header("UI")]
    public TextMeshProUGUI keybindText;

    private InputAction _action;

    void Awake()
    {
        ResolveAction();
    }

    void Start()
    {
        RefreshDisplay();
        TabletManager.OnKeyRebindCompletedEvent.AddListener(RefreshDisplay);
    }

    void OnEnable()
    {
        if (_action != null)
            RefreshDisplay();
    }

    void OnDisable()
    {
        TabletManager.OnKeyRebindCompletedEvent.RemoveListener(RefreshDisplay);
    }

    void OnDestroy()
    {
        TabletManager.OnKeyRebindCompletedEvent.RemoveListener(RefreshDisplay);
    }

    void ResolveAction()
    {
        if (inputActionAsset == null) return;

        var parts = actionPath.Split('/');
        if (parts.Length == 2)
            _action = inputActionAsset.FindActionMap(parts[0])?.FindAction(parts[1]);
        else
            _action = inputActionAsset.FindAction(actionPath);
    }

    public void RefreshDisplay()
    {
        if (_action == null || keybindText == null) return;
        keybindText.text = GetBindingDisplayString();
    }

    string GetBindingDisplayString()
    {
        for (int i = 0; i < _action.bindings.Count; i++)
        {
            var binding = _action.bindings[i];

            if (binding.isComposite) continue;

            if (string.IsNullOrEmpty(controlScheme) ||
                string.IsNullOrEmpty(binding.groups) ||
                binding.groups.Contains(controlScheme))
            {
                string display = _action.GetBindingDisplayString(i,
                    InputBinding.DisplayStringOptions.DontIncludeInteractions);

                if (!string.IsNullOrEmpty(display))
                    return display;
            }
        }

        return "Unbound";
    }
}
