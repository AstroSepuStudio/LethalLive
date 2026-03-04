using UnityEngine;
using UnityEngine.EventSystems;

public class MapDragHandler : MonoBehaviour, IDragHandler, IPointerDownHandler
{
    [SerializeField] RectTransform mapTarget;
    [SerializeField] DNG_MapModule mapModule;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (mapModule.IsFollowingPlayer)
            mapModule.ToggleFollowPlayer();
    }

    public void OnDrag(PointerEventData eventData)
    {
        mapTarget.anchoredPosition += eventData.delta;
    }
}
