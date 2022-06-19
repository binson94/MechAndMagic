using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum SkillState
{
    CantLearn, CanLearn, Learned, Equip
}

public class SkillBtnToken : MonoBehaviour
{
    SmithPanel SM;
    BedSkillPanel BM;

    [SerializeField] Image frameImage;
    [SerializeField] Image skillIconImage;

    [Tooltip("0 skillName, 1 skillLV, 2 skillAP")]
    ///<summary> 0 skillName, 1 skillLV, 2 skillAP </summary>
    [SerializeField] Text[] skillTxts;


    [SerializeField] GameObject skillAP;
    [SerializeField] GameObject lockImage;
    [SerializeField] GameObject learnBtn;
    [SerializeField] Text skillExplain;

    SkillState state;
    int skillIdx;

    public void Init(SmithPanel s, int idx)
    {
        SM = s;
        BM = null;

        skillIdx = idx;
    }

    public void Init(BedSkillPanel b, KeyValuePair<Skill, int> s, KeyValuePair<SkillState, string> state, Sprite frame, Sprite icon)
    {
        BM = b;
        SM = null;

        this.state = state.Key;
        if (this.state == SkillState.CanLearn)
        {
            lockImage.SetActive(true);
            learnBtn.SetActive(true);
            skillExplain.gameObject.SetActive(false);
            skillTxts[1].gameObject.SetActive(false);
            skillAP.SetActive(false);
        }
        else if (this.state == SkillState.CantLearn)
        {
            lockImage.SetActive(true);
            learnBtn.SetActive(false);
            skillExplain.text = state.Value;
            skillExplain.gameObject.SetActive(true);
            skillTxts[1].gameObject.SetActive(false);
            skillAP.SetActive(false);
        }
        else
        {
            lockImage.SetActive(false);
            skillAP.SetActive(s.Key.useType == 0);
            skillTxts[1].gameObject.SetActive(true);
        }

        frameImage.sprite = frame;
        skillIconImage.sprite = icon;

        skillIdx = s.Key.idx;
        skillTxts[0].text = s.Key.name;
        skillTxts[1].text = string.Concat("Lv.", s.Key.reqLvl);
        skillTxts[2].text = s.Key.reqLvl.ToString();
    }

    public void Btn_Select()
    {
        if (SM != null)
            Debug.Log(1);
        else
        {
            BM.Btn_SkillToken(skillIdx, state);
        }
    }
}
