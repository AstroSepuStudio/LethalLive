using UnityEngine;
using UnityEngine.EventSystems;

public class MapDragHandlerOffline : MonoBehaviour, IDragHandler, IPointerDownHandler
{
    [SerializeField] RectTransform mapTarget;
    [SerializeField] DNG_MapModuleOffline mapModule;

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
