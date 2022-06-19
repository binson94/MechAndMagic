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

    ///<summary> 0 active, 1 passive, 2 all </summary>
    int currType = 0;
    ///<summary> 0 didn't Learned, 1 Learned, 2 All </summary>
    int currLearned = 0;
    int currLvl = 0;        //0 All

    int currSkillIdx = -1;
    [SerializeField] SkillInfoPanel currSkillPanel;
    int selectedSkillIdx = -1;
    SkillState selectedSkillState = SkillState.CantLearn;
    [SerializeField] SkillInfoPanel selectedSkillPanel;

    [SerializeField] Sprite[] skillFrameSprites;
    [SerializeField] Sprite[] skillIconSprites;
    List<SkillBtnToken> skillTokenList = new List<SkillBtnToken>();
    List<SkillBtnToken> skillTokenPool = new List<SkillBtnToken>();
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

        Skill[] s = SkillManager.GetSkillData(GameManager.instance.slotData.slotClass);
        int[] skillBooks = ItemManager.GetSkillbookData();

        List<KeyValuePair<Skill, int>> skills = new List<KeyValuePair<Skill, int>>();
        for (int i = 0; i < s.Length; i++)
            if (GameManager.instance.slotData.slotClass == 7 && s[i].category == 1024)
                continue;
            else
                skills.Add(new KeyValuePair<Skill, int>(s[i], GameManager.instance.slotData.itemData.IsLearned(s[i].idx) ? 1 : 0));


        var showSkills = 
            (from token in skills
             where (token.Key.useType == currType || currType == 2) && (token.Value == currLearned || currLearned == 2) && (token.Key.reqLvl == currLvl || currLvl == 0)
             select token);

        foreach(KeyValuePair<Skill, int> skillLearnPair in showSkills)
        {
            SkillBtnToken go = NewSkillToken();
            go.transform.SetParent(skillTokenParent);
            skillTokenList.Add(go);
            go.Init(this, skillLearnPair, GetSkillState(skillLearnPair), skillFrameSprites[skillLearnPair.Key.useType], skillIconSprites[0]);
            go.gameObject.SetActive(true);
        }

        SkillBtnToken NewSkillToken()
        {
            if (skillTokenPool.Count > 0)
            {
                SkillBtnToken go = skillTokenPool[0];
                skillTokenPool.RemoveAt(0);
                return go;
            }
            else
                return Instantiate(skillTokenPrefab).GetComponent<SkillBtnToken>();
        }
        KeyValuePair<SkillState, string> GetSkillState(KeyValuePair<Skill, int> skillLearnPair)
        {
            SkillState state;
            string expression = string.Empty;
            if (skillLearnPair.Value == 0)
            {
                state = SkillState.CanLearn;

                if (skillLearnPair.Key.reqLvl > GameManager.instance.slotData.lvl)
                {
                    state = SkillState.CantLearn;
                    expression = string.Concat("Lv.", skillLearnPair.Key.reqLvl, " 필요");
                }
                else
                {
                    foreach(int skillIdx in skillLearnPair.Key.reqskills)
                    {
                        Skill s = SkillManager.GetSkill(GameManager.instance.slotData.slotClass, skillIdx);
                        if(s != null && !GameManager.instance.slotData.itemData.IsLearned(s.idx))
                        {
                            state = SkillState.CantLearn;
                            expression = string.Concat(s.name, " 학습 필요");
                            break;
                        }
                    }

                    if(!GameManager.instance.slotData.itemData.HasSkillBook(skillLearnPair.Key.idx))
                    {
                        state = SkillState.CantLearn;
                        expression = "스킬북 없음";
                    }
                }
            }
            else if (GameManager.instance.slotData.activeSkills.Contains(skillLearnPair.Key.idx) || GameManager.instance.slotData.passiveSkills.Contains(skillLearnPair.Key.idx))
                state = SkillState.Equip;
            else
                state = SkillState.Learned;

            return new KeyValuePair<SkillState, string>(state, expression);
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
            int skillIdx = currSkillIdx < 6 ? GameManager.instance.slotData.activeSkills[currSkillIdx] : GameManager.instance.slotData.passiveSkills[currSkillIdx - 6];
            if (skillIdx > 0)
            {
                currSkillPanel.InfoUpdate(SkillManager.GetSkill(GameManager.instance.slotData.slotClass, skillIdx));
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
            selectedSkillPanel.InfoUpdate(SkillManager.GetSkill(GameManager.instance.slotData.slotClass, selectedSkillIdx));
            selectedSkillPanel.gameObject.SetActive(true);
        }
        else
            selectedSkillPanel.gameObject.SetActive(false);
    }

    public void Btn_SkillEquip()
    {
        if (SkillManager.GetSkill(GameManager.instance.slotData.slotClass, selectedSkillIdx).useType == 0)
        {
            for (int i = 0; i < 6; i++)
                if (GameManager.instance.slotData.activeSkills[i] == 0)
                {
                    GameManager.instance.slotData.activeSkills[i] = selectedSkillIdx;
                    GameManager.instance.SaveSlotData();
                    break;
                }
        }
        else
        {
            for (int i = 0; i < 4; i++)
                if (GameManager.instance.slotData.passiveSkills[i] == 0)
                {
                    GameManager.instance.slotData.passiveSkills[i] = selectedSkillIdx;
                    GameManager.instance.SaveSlotData();
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
            if (GameManager.instance.slotData.activeSkills[i] == selectedSkillIdx)
            {
                GameManager.instance.slotData.activeSkills[i] = 0;
                GameManager.instance.SaveSlotData();
                break;
            }
        for (int i = 0; i < 4; i++)
        {
            if (GameManager.instance.slotData.passiveSkills[i] == selectedSkillIdx)
            {
                GameManager.instance.slotData.passiveSkills[i] = 0;
                GameManager.instance.SaveSlotData();
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
