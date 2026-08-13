using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;


public class EventTest : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler
{
public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log($"OnPointerDown : {eventData.position}", gameObject);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Debug.Log($"OnPointerEnter : {eventData.position}", gameObject);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Debug.Log($"OnPointerExit : {eventData.position}", gameObject);
    }

}
