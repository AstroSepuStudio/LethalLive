using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EmoteWheelPiece : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI emoteName;
    public CanvasGroup canvasGroup;
    public RectTransform pivot;

    public void UpdatePiece(Emote emote)
    {
        if (emote == null)
        {
            emoteName.text = "";
            pivot.gameObject.SetActive(false);
            return;
        }

        pivot.gameObject.SetActive(true);
        emoteName.text = emote.emoteName;
    }
}
