using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class BedSkillPanel : MonoBehaviour, ITownPanel
{
    [Header("Skill Panel")]
    #region Variable_Skill
    [SerializeField] Transform skillTokenParent;
    [SerializeField] Transform skillTokenPoolParent;
    [SerializeField] GameObject skillTokenPrefab;

    int currType = 0;       //0 active, 1 passive, 2 all
    int currLearned = 0;    //0 didn't Learned, 1 Learned, 2 All
    int currLvl = 0;        //0 All

    int currSkillIdx = -1;
    [SerializeField] SkillInfoPanel currSkillPanel;
    int selectedSkillIdx = -1;
    SkillState selectedSkillState = SkillState.CantLearn;
    [SerializeField] SkillInfoPanel selectedSkillPanel;

    //0 : 학습, 1 : 장착, 2 : 해제
    [SerializeField] GameObject[] skillBtns;
    List<SkillBtnSet> skillTokenList = new List<SkillBtnSet>();
    List<SkillBtnSet> skillTokenPool = new List<SkillBtnSet>();
    #endregion

    public void ResetAllState()
    {
        currType = 2;
        currLearned = 2;
        SkillTokenUpdate();

        currSkillIdx = -1;
        selectedSkillIdx = -1;
        selectedSkillState = SkillState.CantLearn;
        CurrSkillPanelUpdate();
        SelectedSkillPanelUpdate();
    }

    #region Category
    public void Btn_SwitchType(int type)
    {
        currType = type;
        SkillTokenUpdate();
    }
    public void Btn_SwitchLearned(int learned)
    {
        currLearned = learned;
        SkillTokenUpdate();
    }
    public void Btn_SwitchLvl(int lv)
    {
        currLvl = lv;
        SkillTokenUpdate();
    }
    #endregion Category
    #region Token Update
    void SkillTokenUpdate()
    {
        SkillTokenReset();

        Skill[] s = SkillManager.GetSkillData(GameManager.slotData.slotClass);
        int[] skillBooks = ItemManager.GetSkillbookData();

        List<KeyValuePair<Skill, int>> skills = new List<KeyValuePair<Skill, int>>();
        for (int i = 0; i < s.Length; i++)
            if (GameManager.slotData.slotClass == 7 && s[i].category == 1024)
                continue;
            else
                skills.Add(new KeyValuePair<Skill, int>(s[i], ItemManager.IsLearned(s[i].idx) ? 1 : 0));


        List<KeyValuePair<Skill, int>> showSkills = (from token in skills
                         where (token.Key.useType == currType || currType == 2) && (token.Value == currLearned || currLearned == 2) && (token.Key.reqLvl == currLvl || currLvl == 0)
                         select token).ToList();

        for (int i = 0; i < showSkills.Count; i++)
        {
            GameObject go = NewSkillToken();
            go.transform.SetParent(skillTokenParent);
            skillTokenList.Add(go.GetComponent<SkillBtnSet>());
            skillTokenList[skillTokenList.Count - 1].Init(this, showSkills[i], GetSkillState(showSkills[i]));
            go.SetActive(true);
        }
        
        GameObject NewSkillToken()
        {
            if (skillTokenPool.Count > 0)
            {
                GameObject go = skillTokenPool[0].gameObject;
                skillTokenPool.RemoveAt(0);
                return go;
            }
            else
                return Instantiate(skillTokenPrefab);
        }
        SkillState GetSkillState(KeyValuePair<Skill, int> a)
        {
            SkillState state;
            if(a.Value == 0)
            {
                state = SkillState.CanLearn;
                for (int i = 0; i < a.Key.reqskills.Length; i++)
                    if (!ItemManager.IsLearned(a.Key.reqskills[i]))
                    {
                        state = SkillState.CantLearn;
                        break;
                    }
            }
            else
            {
                state = SkillState.Learned;
                foreach (int idx in GameManager.slotData.activeSkills)
                    if (a.Key.idx == idx)
                        state = SkillState.Equip;
                foreach (int idx in GameManager.slotData.passiveSkills)
                    if (a.Key.idx == idx)
                        state = SkillState.Equip;
            }

            return state;
        }
    }
    void SkillTokenReset()
    {
        for (int i = 0; i < skillTokenList.Count; i++)
        {
            skillTokenList[i].gameObject.SetActive(false);
            skillTokenList[i].transform.SetParent(skillTokenPoolParent);
            skillTokenPool.Add(skillTokenList[i]);
        }
        skillTokenList.Clear();
    }
    #endregion Token Update
    public void Btn_SkillSlot(int idx)
    {
        if (currSkillIdx == idx)
            currSkillIdx = -1;
        else
            currSkillIdx = idx;

        CurrSkillPanelUpdate();
    }
    public void Btn_SkillToken(int idx, SkillState state)
    {
        if (selectedSkillIdx == idx)
        {
            selectedSkillIdx = -1;
            selectedSkillState = SkillState.CantLearn;
        }
        else
        {
            selectedSkillIdx = idx;
            selectedSkillState = state;
        }

        SelectedSkillPanelUpdate();
    }

    void CurrSkillPanelUpdate()
    {
        if (currSkillIdx >= 0)
        {
            int skillIdx = currSkillIdx < 6 ? GameManager.slotData.activeSkills[currSkillIdx] : GameManager.slotData.passiveSkills[currSkillIdx - 6];
            if (skillIdx > 0)
            {
                currSkillPanel.InfoUpdate(SkillManager.GetSkill(GameManager.slotData.slotClass, skillIdx));
                currSkillPanel.gameObject.SetActive(true);
            }
            else
            {
                currSkillIdx = -1;
                currSkillPanel.gameObject.SetActive(false);
            }
        }
        else
            currSkillPanel.gameObject.SetActive(false);
    }
    void SelectedSkillPanelUpdate()
    {
        if (selectedSkillIdx > 0)
        {
            selectedSkillPanel.InfoUpdate(SkillManager.GetSkill(GameManager.slotData.slotClass, selectedSkillIdx));
            selectedSkillPanel.gameObject.SetActive(true);

            for (int i = 0; i < skillBtns.Length; i++)
                skillBtns[i].SetActive(i + 1 == (int)selectedSkillState);
        }
        else
        {
            selectedSkillPanel.gameObject.SetActive(false);
            foreach (GameObject g in skillBtns)
                g.SetActive(false);
        }
    }

    public void Btn_SkillEquip()
    {
        if (SkillManager.GetSkill(GameManager.slotData.slotClass, selectedSkillIdx).useType == 0)
        {
            for (int i = 0; i < 6; i++)
                if (GameManager.slotData.activeSkills[i] == 0)
                {
                    GameManager.slotData.activeSkills[i] = selectedSkillIdx;
                    GameManager.SaveSlotData();
                    break;
                }
        }
        else
        {
            for(int i =0;i<4;i++)
                if(GameManager.slotData.passiveSkills[i] == 0)
                {
                    GameManager.slotData.passiveSkills[i] = selectedSkillIdx;
                    GameManager.SaveSlotData();
                    break;
                }
        }

        selectedSkillIdx = -1;
        selectedSkillState = SkillState.CantLearn;

        SkillTokenUpdate();
        SelectedSkillPanelUpdate();
    }
    public void Btn_SkillUnEquip()
    {
        for (int i = 0; i < 6; i++)
            if (GameManager.slotData.activeSkills[i] == selectedSkillIdx)
            {
                GameManager.slotData.activeSkills[i] = 0;
                GameManager.SaveSlotData();
                break;
            }
        for (int i = 0; i < 4; i++)
        {
            if (GameManager.slotData.passiveSkills[i] == selectedSkillIdx)
            {
                GameManager.slotData.passiveSkills[i] = 0;
                GameManager.SaveSlotData();
                break;
            }
        }

        selectedSkillIdx = -1;
        selectedSkillState = SkillState.CantLearn;
        SkillTokenUpdate();
        SelectedSkillPanelUpdate();
    }
    public void Btn_SkillLearn()
    {
        ItemManager.SkillLearn(selectedSkillIdx);
        selectedSkillIdx = -1;
        selectedSkillState = SkillState.CantLearn;
        SkillTokenUpdate();
        SelectedSkillPanelUpdate();
    }
}
