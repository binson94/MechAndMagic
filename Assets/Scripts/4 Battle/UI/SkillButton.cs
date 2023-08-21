using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SkillButton : MonoBehaviour
{
    [SerializeField] Image btnImage;
    ///<summary> 스킬 종류 표시하는 아이콘 이미지 </summary>
    [SerializeField] Image skillIcon;
    ///<summary> 0 스킬 이름, 1 스킬 ap, 2 스킬 쿨다운 값, 3 남은 쿨다운 </summary>
    [Tooltip("0 스킬 이름, 1 스킬 ap, 2 스킬 쿨다운 값, 3 남은 쿨다운")]
    [SerializeField] Text[] skillTxts;
    ///<summary> 스킬 선택 시 하이라이트 표시 </summary>
    [SerializeField] GameObject highlight;

    Color[] colors = new Color[] { new Color(1, 1, 1, 1), new Color(1, 1, 1, 0.4f), new Color(1, 1, 1, 0.8f) };

    public void Init(Unit unit, int skillSlotIdx)
    {
        Skill skill = SkillManager.GetSkill(unit.classIdx, unit.activeIdxs[skillSlotIdx]);
        if(skill == null) return;

        skillTxts[0].text = skill.name;
        skillIcon.sprite = SpriteGetter.instance.GetSkillIcon(skill.icon);
        ValueUpdate(unit, skillSlotIdx);
    }

    public void ValueUpdate(Unit unit, int skillSlotIdx)
    {
        Skill skill = SkillManager.GetSkill(unit.classIdx, unit.activeIdxs[skillSlotIdx]);
        if(skill == null) return;

        if (unit.cooldowns[skillSlotIdx] <= 0)
        {
            if (unit is VisionMaster)
            {
                Skill minus = SkillManager.GetSkill(unit.classIdx, unit.activeIdxs[skillSlotIdx] + 1);
                skillTxts[1].text = $"<color=#ed2929>{Mathf.Min(unit.GetSkillCost(minus), unit.GetSkillCost(skill))}</color> AP";
            }
            else
                skillTxts[1].text = $"<color=#ed2929>{unit.GetSkillCost(skill)}</color> AP";
            skillTxts[2].text = $"<color=#ed2929>{skill.cooldown}</color> CD";
            skillTxts[3].text = string.Empty;

            btnImage.color = skillIcon.color = colors[0];
            skillTxts[0].color = colors[0];
        }
        else
        {
            skillTxts[1].text = skillTxts[2].text = string.Empty;
            skillTxts[3].text = $"쿨타임 <color=#ed2929>{unit.cooldowns[skillSlotIdx]}</color> 턴 남음";

            btnImage.color = skillIcon.color = colors[1];
            skillTxts[0].color = colors[2];
        }
    }

    ///<summary> 비전 마스터 선택 버튼 업데이트 </summary>
    public void ValueUpdate(VisionMaster vision, int skillSlotIdx, int pivot)
    {
        Skill skill = SkillManager.GetSkill(vision.classIdx, vision.activeIdxs[skillSlotIdx] + pivot);
        skillIcon.sprite = SpriteGetter.instance.GetSkillIcon(skill.icon);

        skillTxts[0].text = pivot <= 0 ? $"{skill.name} : 양" : skill.name;
        skillTxts[1].text = $"<color=#ed2929>{vision.GetSkillCost(skill)}</color> AP";
        skillTxts[2].text = $"<color=#ed2929>{skill.cooldown}</color> CD";
        skillTxts[3].text = string.Empty;
        
        btnImage.color = skillIcon.color = colors[0];
        skillTxts[0].color = colors[0];
    }

    public void Highlight(bool isHigh) => highlight.SetActive(isHigh);
    
}
