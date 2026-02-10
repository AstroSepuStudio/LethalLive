using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class LL_RebindActionUI : MonoBehaviour
{
    [SerializeField] PlayerInput pInput;
    [SerializeField] int bindingIndex;

    [SerializeField] InputActionReference targetAction;
    [SerializeField] TextMeshProUGUI actionNameTxt;
    [SerializeField] TextMeshProUGUI keybindTxt;

    [SerializeField] GameObject rebindOverlay;
    [SerializeField] TextMeshProUGUI rebindTxt;


    InputActionRebindingExtensions.RebindingOperation rebOp;

    private void Awake()
    {
        if (pInput == null)
            pInput = GetComponentInParent<PlayerInput>();

        UpdateUI();
    }

    public void StartRebind()
    {
        if (targetAction == null)
            return;

        if (targetAction.action.bindings[bindingIndex].isComposite)
            return;

        rebindOverlay.SetActive(true);
        rebindTxt.SetText(
            $"Rebinding {targetAction.action.name} \n" +
            $"Press a key... (ESC to cancel)");

        pInput.SwitchCurrentActionMap("W8");

        rebOp = targetAction.action
            .PerformInteractiveRebinding(bindingIndex)
            .WithCancelingThrough("<Keyboard>/escape")
            .OnMatchWaitForAnother(0.1f)
            .OnCancel(_ => RebindCanceled())
            .OnComplete(_ => RebindComplete())
            .Start();
    }

    public void ResetToDefault()
    {
        if (targetAction == null)
            return;

        if (rebOp != null)
        {
            rebOp.Cancel();
            rebOp.Dispose();
        }

        targetAction.action.RemoveBindingOverride(bindingIndex);

        rebindOverlay.SetActive(false);
        pInput.SwitchCurrentActionMap("Player");

        UpdateUI();
    }

    void RebindComplete()
    {
        rebOp.Dispose();

        rebindOverlay.SetActive(false);
        pInput.SwitchCurrentActionMap("Player");

        UpdateUI();
    }

    void RebindCanceled()
    {
        rebOp.Dispose();

        rebindOverlay.SetActive(false);
        pInput.SwitchCurrentActionMap("Player");

        UpdateUI();
    }

    void UpdateUI()
    {
        if (targetAction == null) return;

        actionNameTxt.text = targetAction.action.name;

        string bind = targetAction.action.GetBindingDisplayString(bindingIndex);
        keybindTxt.text = string.IsNullOrEmpty(bind) ? "Unbound" : bind;
    }
}
