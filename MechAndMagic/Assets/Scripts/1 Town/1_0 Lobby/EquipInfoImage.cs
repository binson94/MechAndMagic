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
    public void SetImage(Sprite frame, Equipment e)
    {
        frameImage.sprite = frame;

        if(e == null)
        {
            iconImage.gameObject.SetActive(false);
            lvTxt.gameObject.SetActive(false);
        }
        else
        {
            iconImage.sprite = SpriteGetter.instance.GetEquipIcon(e);
            iconImage.gameObject.SetActive(true);
            lvTxt.text = $"Lv.{e.ebp.reqlvl}";
            lvTxt.gameObject.SetActive(true);
        }
    }
}