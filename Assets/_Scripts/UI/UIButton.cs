using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class UIButton : MonoBehaviour
{
    [SerializeField] bool useColorSelection = true;
    [SerializeField] Color selectedColor = new(0, 150, 0);
    [SerializeField] Color deselectedColor = new(0, 100, 0);

    public UnityEvent OnSelectedEvent;
    public UnityEvent OnDeselectedEvent;

    Image graphic;

    private void Start()
    {
        graphic = GetComponent<Image>();
    }

    public void OnButtonSelected()
    {
        OnSelectedEvent?.Invoke();

        if (useColorSelection && graphic != null)
            graphic.color = selectedColor;
    }

    public void OnButtonDeselected()
    {
        OnDeselectedEvent?.Invoke();

        if (useColorSelection && graphic != null)
            graphic.color = deselectedColor;
    }
}
