using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SkillBtnToken : MonoBehaviour
{
    BedSkillPanel BM;

    ///<summary> 액티브 패시브 프레임 </summary>
    [SerializeField] Image frameImage;
    ///<summary> 스킬 아이콘 </summary>
    [SerializeField] Image skillIconImage;

    ///<summary> 0 skillName, 1 skillLV, 2 skillAP </summary>
    [Tooltip("0 skillName, 1 skillLV, 2 skillAP")]
    [SerializeField] Text[] skillTxts;

    ///<summary> 스킬 AP 표시, 액티브 스킬이고 학습했을 때만 표시 </summary>
    [SerializeField] GameObject skillAP;
    ///<summary> 스킬 잠금 아이콘, 미학습 때만 표시 </summary>
    [SerializeField] GameObject lockImage;
    ///<summary> 스킬 학습 버튼, 학습 가능일 때만 표시 </summary>
    [SerializeField] GameObject learnBtn;
    ///<summary> 스킬 학습 불가능 이유 표시 텍스트 </summary>
    [SerializeField] Text cantLearnExplainTxt;

    int skillIdx;
    SkillState state;

    ///<summary> 숙소 스킬 창에서 토큰 활성화 </summary>
    ///<param name="state"> 스킬 학습, 장착 정보와 학습 불가 시 이유 </param>
    public void Init(BedSkillPanel b, Skill skill, KeyValuePair<SkillState, string> state, Sprite frame)
    {
        BM = b;

        this.state = state.Key;
        //미학습, 학습 가능 -> 잠금 이미지, 학습 버튼 보이기,    학습 불가 이유, 레벨, AP 소모량 숨김
        if (this.state == SkillState.CanLearn)
        {
            lockImage.SetActive(true);
            learnBtn.SetActive(true);
            cantLearnExplainTxt.gameObject.SetActive(false);
            skillTxts[1].gameObject.SetActive(false);
            skillAP.SetActive(false);
        }
        //학습 불가 -> 잠금 이미지, 학습 불가 이유 보이기,    학습 버튼, 레벨, AP 소모량 숨김
        else if (this.state == SkillState.CantLearn)
        {
            lockImage.SetActive(true);
            learnBtn.SetActive(false);
            cantLearnExplainTxt.text = state.Value;
            cantLearnExplainTxt.gameObject.SetActive(true);
            skillTxts[1].gameObject.SetActive(false);
            skillAP.SetActive(false);
        }
        //학습함 -> 요구 레벨, 액티브인 경우 ap 소모량 표시,    잠금 이미지 숨김
        else
        {
            lockImage.SetActive(false);
            skillAP.SetActive(skill.useType == 0);
            skillTxts[1].gameObject.SetActive(true);
        }

        frameImage.sprite = frame;
        skillIconImage.sprite = SpriteGetter.instance.GetSkillIcon(skill.icon);

        skillIdx = skill.idx;
        skillTxts[0].text = skill.name;
        skillTxts[1].text = $"Lv.{skill.reqLvl}";
        skillTxts[2].text = $"{skill.apCost}";
    }

    ///<summary> 던전 정비 창에서 버튼 활성화 </summary>
    public void Init(int skillSlotIdx)
    {
        BM = null;
        Skill skill = SkillManager.GetSlotSkill(skillSlotIdx);

        if(skill.idx == 0)
        {
            skillIconImage.gameObject.SetActive(false);
            skillAP.SetActive(false);
            foreach(Text t in skillTxts) t.text = string.Empty;
        }
        else
        {
            Skill s = SkillManager.GetSkill(GameManager.SlotClass, skillSlotIdx < 6 ? GameManager.Instance.slotData.activeSkills[skillSlotIdx] : GameManager.Instance.slotData.passiveSkills[skillSlotIdx - 6]);
            skillIconImage.sprite = SpriteGetter.instance.GetSkillIcon(skill.icon);
            skillIconImage.gameObject.SetActive(true);

            skillTxts[0].text = skill.name;
            skillTxts[1].text = $"Lv.{skill.reqLvl}";
            skillTxts[2].text = skill.apCost.ToString();
            skillAP.SetActive(s.useType == 0);
        }
    }

    public void Btn_Select()
    {
        SoundManager.Instance.PlaySFX(22);
        if (BM != null)
            BM.Btn_SkillToken(skillIdx, state);
    }
    public void Btn_Learn()
    {
        SoundManager.Instance.PlaySFX(22);
        if(BM != null) BM.BedToSkillLearn(skillIdx);
    }

}
