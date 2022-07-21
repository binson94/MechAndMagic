using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class DropPopup : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    ///<summary> 왼쪽부터 0 ~ 4 </summary>
    [SerializeField] int tokenIdx;
    [SerializeField] DropToken tokenParent;
    [SerializeField] RectTransform rect;

    public void OnPointerDown(PointerEventData data) => tokenParent.ShowPopUp(tokenIdx, rect);
    public void OnPointerUp(PointerEventData data) => tokenParent.HidePopUp();
}
