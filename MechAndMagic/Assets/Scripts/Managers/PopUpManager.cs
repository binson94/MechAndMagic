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
        popup.anchoredPosition = new Vector2(0, 0);
        popup.position = new Vector3(Mathf.Min(popup.position.x, 1020 - popup.rect.width), popup.position.y, popup.position.z);
        popup.SetParent(viewPoint);
        popupTxt.color = txtColor;
        popupTxt.text = script;
        popup.gameObject.SetActive(true);
    }
    public void HidePopUp() => popup.gameObject.SetActive(false);
}
