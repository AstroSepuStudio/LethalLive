using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EmoteWheelPiece : MonoBehaviour
{
    [SerializeField] Image icon;
    public CanvasGroup canvasGroup;
    public RectTransform pivot;

    public void UpdatePiece(Emote emote)
    {
        if (emote == null)
        {
            icon.sprite = null;
            pivot.gameObject.SetActive(false);
            return;
        }

        pivot.gameObject.SetActive(true);
        icon.sprite = emote.icon;
    }
}
