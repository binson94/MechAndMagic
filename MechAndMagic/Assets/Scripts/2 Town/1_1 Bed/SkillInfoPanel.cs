using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

///<summary> 숙소 스킬창에서 현재 선택한 스킬 표시하는 UI </Summary>
public class SkillInfoPanel : MonoBehaviour
{
    ///<summary> 스킬 아이콘 이미지 </summary>
    [SerializeField] Image iconImage;

    [Tooltip("0 name, 1 lvl, 2 ap, 3 cd, 4 script, 5 pos, 6 neg")]
    ///<summary> 스킬 정보 표기 텍스트들
    ///<para> 0 name, 1 lvl, 2 ap, 3 cd, 4 script, 5 pos, 6 neg </para>
    ///</summary>
    [SerializeField] Text[] skillInfoTxts;
    [SerializeField] GameObject apTxts;
    [SerializeField] GameObject cooldownTxts;
    
    public void InfoUpdate(Skill s)
    {
        //선택 취소한 경우
        if (s.idx == 0)
        {
            iconImage.gameObject.SetActive(false);
            foreach(Text t in skillInfoTxts)
                t.text = string.Empty;
            apTxts.SetActive(false);
            cooldownTxts.SetActive(false);
        }
        else
        {
            iconImage.sprite =  SpriteGetter.instance.GetSkillIcon(s.icon);
            iconImage.gameObject.SetActive(true);

            skillInfoTxts[0].text = s.name;
            skillInfoTxts[1].text = $"Lv.{s.reqLvl}";

            skillInfoTxts[2].text = $"{s.apCost}";
            apTxts.SetActive(s.useType == 0);
            skillInfoTxts[3].text = $"{s.cooldown}";
            cooldownTxts.SetActive(s.useType == 0);
            
            skillInfoTxts[4].text = s.script;

            skillInfoTxts[5].text = s.posScript;
            skillInfoTxts[6].text = s.negScript;
        }
    }
}
