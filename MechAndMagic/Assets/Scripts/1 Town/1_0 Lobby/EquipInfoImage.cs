using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EquipInfoImage : MonoBehaviour
{
    [SerializeField] Image frameImage;
    [SerializeField] Image iconImage;
    [SerializeField] Text lvTxt;

    ///<summary> 장비 정보 이미지 세팅, 장착 장비 없으면 lv 0으로 전달 </summary>
    public void SetImage(Sprite frame, Sprite icon, int lv)
    {
        frameImage.sprite = frame;

        if(lv == 0)
        {
            iconImage.gameObject.SetActive(false);
            lvTxt.gameObject.SetActive(false);
        }
        else
        {
            iconImage.sprite = icon;
            iconImage.gameObject.SetActive(true);
            lvTxt.text = $"Lv.{lv}";
            lvTxt.gameObject.SetActive(true);
        }
    }
}