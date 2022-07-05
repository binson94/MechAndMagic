using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

///<summary> 스킬 장착 슬롯 표시를 위한 클래스 </summary>
public class SkillSlot : MonoBehaviour
{
    ///<summary> 스킬 아이콘 </summary>
    [SerializeField] Image iconImage;
    ///<summary> 스킬 레벨 </summary>
    [SerializeField] Text lvlTxt;
    ///<summary> 스킬 슬롯 테두리 </summary>
    [SerializeField] GameObject baseFrame;
    ///<summay> 스킬 슬롯 선택 강조 테두리 </summary>
    [SerializeField] GameObject selectFrame;

    ///<summary> 슬롯 이미지 업데이트 </summary>
    ///<param name="icon"> 스킬 아이콘, null인 경우, 장착되지 않은 슬롯 </param>
    ///<param name="select"> 슬롯 선택 여부 </param>
    public void ImageUpdate(int skillIcon, int lvl, bool select)
    {
        if(lvl == 0)
        {
            iconImage.gameObject.SetActive(false);
            lvlTxt.text = string.Empty;
            baseFrame.SetActive(false);
            selectFrame.SetActive(select);
        }
        else
        {
            iconImage.sprite = Resources.Load<Sprite>($"Sprites/SkillIcon/icon_{skillIcon}");
            iconImage.gameObject.SetActive(true);
            lvlTxt.text = $"Lv.{lvl}";
            baseFrame.SetActive(true);
            selectFrame.SetActive(select);
        }
    }
}
