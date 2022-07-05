using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameSlot : MonoBehaviour
{
    ///<summary> 슬롯 클래스 아이콘 </summary>
    [SerializeField] Image classIconImage;
    ///<summary> 슬롯 소드 버튼 </summary>
    [SerializeField] Image loadBtn;
    ///<summary> 슬롯 삭제 버튼 </summary>
    [SerializeField] GameObject deleteBtn;
    ///<summary> 클래스 이름 표시 텍스트 </sumary>
    [SerializeField] Text classTxt;
    //<summary> 슬롯 챕터 표시 텍스트 </summary>
    [SerializeField] Text chapterTxt;

    ///<summary> 빈 슬롯 새로 시작 버튼 </summary>
    [SerializeField] GameObject newBtn;

    public void SlotUpdate(SlotData slot = null, Sprite icon = null, Sprite frame = null)
    {
        if(slot != null)
        {
            newBtn.SetActive(false);

            classIconImage.sprite = icon;
            classIconImage.gameObject.SetActive(true);
            loadBtn.sprite = frame;
            loadBtn.gameObject.SetActive(true);
            deleteBtn.SetActive(true);

            classTxt.text = slot.className;
            chapterTxt.text = $"Lv.{slot.lvl} 챕터 {slot.chapter}";
        }
        else
        {
            newBtn.SetActive(true);

            classIconImage.gameObject.SetActive(false);
            loadBtn.gameObject.SetActive(false);
            deleteBtn.SetActive(false);
        }
    }
}
