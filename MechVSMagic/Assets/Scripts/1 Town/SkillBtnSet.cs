using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum SkillState
{
    CantLearn, CanLearn, Learned, Equip
}

public class SkillBtnSet : MonoBehaviour
{
    SmithManager SM;
    BedManager BM;

    [SerializeField] Text skillName;

    SkillState state;
    int skillIdx;

    public void Init(SmithManager s, int idx)
    {
        SM = s;
        BM = null;

        skillIdx = idx;
    }

    public void Init(BedManager b, KeyValuePair<Skill, int> s, SkillState state)
    {
        BM = b;
        SM = null;

        this.state = state;
        skillIdx = s.Key.idx;
        skillName.text = string.Concat(s.Key.name, state);
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
