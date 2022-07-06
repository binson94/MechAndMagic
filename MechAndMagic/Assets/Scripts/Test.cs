using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Test : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    bool isOn = false;

    ///가운데 부모, 왼쪽 아래 기준점(음수값)
    [SerializeField] RectTransform pivot;
    //왼쪽 아래 기준점
    [SerializeField] RectTransform popUp;
    [SerializeField] RectTransform panel;

    public void OnPointerDown(PointerEventData data) {
        popUp.SetParent(transform);
        popUp.anchoredPosition = new Vector2(Mathf.Min(0, 512 - popUp.rect.width - pivot.anchoredPosition.x), -95);
        popUp.SetParent(panel);
        popUp.gameObject.SetActive(true);
    }

    public void OnPointerUp(PointerEventData data)
    {
        popUp.gameObject.SetActive(false);
    }
}
