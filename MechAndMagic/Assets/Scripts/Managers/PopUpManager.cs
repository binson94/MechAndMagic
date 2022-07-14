using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PopUpManager : MonoBehaviour
{
    [SerializeField] RectTransform viewPoint;
    [SerializeField] Text popupTxt;
    [SerializeField] RectTransform popup;

    public void ShowPopUp(string script, RectTransform btnRect, Color txtColor)
    {
        popup.SetParent(btnRect);
        popup.anchoredPosition = new Vector2(Mathf.Min(0, 540 - popup.rect.width - btnRect.anchoredPosition.x), +95);
        popup.SetParent(viewPoint);
        popupTxt.color = txtColor;
        popupTxt.text = script;
        popup.gameObject.SetActive(true);
    }
    public void HidePopUp() => popup.gameObject.SetActive(false);
}
