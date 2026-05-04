using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EmoteWheelPiece : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI emoteName;
    [SerializeField] GameObject detailsGO;
    [SerializeField] Image loopImg;
    [SerializeField] Image dynamicImg;

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

        detailsGO.SetActive(emote.loop || emote.dynamic);

        loopImg.enabled = emote.loop;
        dynamicImg.enabled = emote.dynamic;
    }
}
