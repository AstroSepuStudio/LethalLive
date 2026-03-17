using UnityEngine;
using UnityEngine.EventSystems;

public class MapDragHandler : MonoBehaviour, IDragHandler, IPointerDownHandler
{
    [SerializeField] RectTransform mapTarget;
    [SerializeField] DNG_MapModule mapModule;

    Vector2 _lastSentPos;
    const float SendThreshold = 2f;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (mapModule.IsFollowingPlayer)
            mapModule.ToggleFollowPlayer();
    }

    public void OnDrag(PointerEventData eventData)
    {
        mapTarget.anchoredPosition += eventData.delta;

        if (Vector2.Distance(mapTarget.anchoredPosition, _lastSentPos) > SendThreshold)
        {
            _lastSentPos = mapTarget.anchoredPosition;
            mapModule.CmdSetMapAnchor(mapTarget.anchoredPosition);
        }
    }
}
