using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EquipInfoImage : MonoBehaviour
{
    ///<summary> 장비 테두리 이미지 </summary>
    [SerializeField] Image frameImage;
    ///<summary> 장비 아이콘 </summary>
    [SerializeField] Image iconImage;
    ///<summary> 장비 레벨 텍스트 </summary>
    [SerializeField] Text lvTxt;
    ///<summary> 장비 성급 표시 이미지 </summary>
    [SerializeField] GameObject[] stars;

    ///<summary> 장비 정보 이미지 세팅, 장착 장비 없으면 lv 0으로 전달 </summary>
    public void SetImage(Sprite frame, Equipment e)
    {
        frameImage.sprite = frame;

        if(e == null)
        {
            iconImage.gameObject.SetActive(false);
            lvTxt.gameObject.SetActive(false);
            foreach(GameObject go in stars) go.SetActive(false);
        }
        else
        {
            iconImage.sprite = SpriteGetter.instance.GetEquipIcon(e.ebp);
            iconImage.gameObject.SetActive(true);
            lvTxt.text = $"Lv.{e.ebp.reqlvl}";
            lvTxt.gameObject.SetActive(true);
            for(int i = 0;i < 3;i++) stars[i].SetActive(i < e.star);
        }
    }
}