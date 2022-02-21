using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class BedPanel : MonoBehaviour, ITownPanel
{
    [SerializeField] TownManager TM;

    [Header("Panels")]
    #region Variable_Panel
    [SerializeField] GameObject statPanel;
    [SerializeField] GameObject[] subPanels;
    int currPanel = 0;
    #endregion

    [Header("Main Panel")]
    #region Variable_Main
    [SerializeField] Text[] statTxts;
    #endregion

    [Header("Equip Panel")]
    #region Variable_Equip
    [SerializeField] Transform equipTokenParent;
    [SerializeField] Transform equipTokenPoolParent;
    [SerializeField] GameObject equipTokenPrefab;

    //1Weapon, 2Armor, 3Accessory, 7Potion
    ItemCategory currCategory = ItemCategory.Weapon;
    Rarity currRarity = Rarity.None;
    //0 : all, 1,3,5,7,9
    int currLvl = 0;
    //0 : Equip, 1 : Potion
    [SerializeField] GameObject[] categoryPanels;

    EquipPart currPart = EquipPart.None;
    [SerializeField] EquipInfoPanel currEquipPanel;
   [SerializeField] KeyValuePair<int, Equipment> selectedEquip;
    [SerializeField] EquipInfoPanel selectedEquipPanel;
    [SerializeField] GameObject equipBtns;
    static KeyValuePair<int, Equipment> dummyEquip = new KeyValuePair<int, Equipment>(-1, null);

    List<EquipBtnSet> equipTokenList = new List<EquipBtnSet>();
    List<EquipBtnSet> equipTokenPool = new List<EquipBtnSet>();
    #endregion

    [Header("Skill Panel")]
    #region Variable_Skill
    [SerializeField] Transform skillTokenParent;
    [SerializeField] Transform skillTokenPoolParent;
    [SerializeField] GameObject skillTokenPrefab;

    int currType = 0;       //0 active, 1 passive, 2 all
    int currLearned = 0;    //0 didn`t Learned, 1 Learned, 2 All
    //lvl은 공유

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
        StatTxtUpdate();
        ResetCategory();

        currPanel = 0;
        PanelShow();
    }
    void ResetCategory()
    {
        currCategory = ItemCategory.Weapon;
        currRarity = Rarity.None;
        currLvl = 0;
        ItemTokenUpdate();

        currType = 2;
        currLearned = 2;
        SkillTokenUpdate();
        
        currPart = EquipPart.None;
        selectedEquip = dummyEquip;
        CurrInfoPanelUpdate();
        SelectedInfoPanelUpdate();

        currSkillIdx = -1;
        selectedSkillIdx = -1;
        selectedSkillState = SkillState.CantLearn;
        CurrSkillPanelUpdate();
        SelectedSkillPanelUpdate();
    }

    #region Function_Main
    public void Btn_PanelSelect(int idx)
    {
        currPanel = idx;
        ResetCategory();
        PanelShow();
    }
    void PanelShow()
    {
        statPanel.SetActive(currPanel <= 1);
        for (int i = 0; i < subPanels.Length; i++) subPanels[i].SetActive(i == currPanel);
    }
    void StatTxtUpdate()
    {
        int i, j;
        for (i = 2, j = 0; i < 13; i++, j++)
        {
            if (i == 3) i++;
            statTxts[j].text = GameManager.slotData.itemStats[i].ToString();
        }
        statTxts[10].text = GameManager.slotData.lvl.ToString();
    }
    #endregion Function_Main

    #region Function_Item
    #region Category
    public void Btn_SwitchCategory(int category)
    {
        currCategory = (ItemCategory)category;
        CategoryBtnUpdate();
        ItemTokenUpdate();

        void CategoryBtnUpdate()
        {
            int curr = (currCategory <= ItemCategory.Accessory) ? 0 : 1;

            for (int i = 0; i < 2; i++)
                categoryPanels[i].SetActive(i == curr);
        }
    }
    public void Btn_SwitchRarity(int rarity)
    {
        currRarity = (Rarity)rarity;
        ItemTokenUpdate();
    }
    public void Btn_SwitchLvl(int lvl)
    {
        currLvl = lvl;
        ItemTokenUpdate();
        SkillTokenUpdate();
    }
    #endregion
    #region Token Update
    void ItemTokenUpdate()
    {
        ItemTokenReset();
        if (currCategory <= ItemCategory.Accessory)
            BtnUpdate_Equip();
        else
            BtnUpdate_Potion();
        
        void BtnUpdate_Equip()
        {
            List<Equipment> now = ItemManager.GetEquipData(currCategory, currRarity, currLvl);

            List<KeyValuePair<int, Equipment>> idxs = new List<KeyValuePair<int, Equipment>>();
            for (int i = 0; i < now.Count;)
            {
                while (idxs.Count < 4 && i < now.Count)
                {
                    idxs.Add(new KeyValuePair<int, Equipment>(i, now[i]));
                    i++;
                }
                GameObject go = NewItemToken();
                go.transform.SetParent(equipTokenParent);
                equipTokenList.Add(go.GetComponent<EquipBtnSet>());
                equipTokenList[equipTokenList.Count - 1].Init(this, idxs);
                go.SetActive(true);

                idxs.Clear();
            }
        }
        void BtnUpdate_Potion()
        {

        }
        GameObject NewItemToken()
        {
            if (equipTokenPool.Count > 0)
            {
                GameObject go = equipTokenPool[0].gameObject;
                equipTokenPool.RemoveAt(0);
                return go;
            }
            else
                return Instantiate(equipTokenPrefab);
        }
    }
    void ItemTokenReset()
    {
        for (int i = 0; i < equipTokenList.Count; i++)
        {
            equipTokenList[i].gameObject.SetActive(false);
            equipTokenList[i].transform.SetParent(equipTokenPoolParent);
            equipTokenPool.Add(equipTokenList[i]);
        }
        equipTokenList.Clear();
    }
    #endregion Token Update
    public void Btn_UnEquip()
    {
        if(currPart != EquipPart.None)
        {
            ItemManager.UnEquip(currPart);
            ItemTokenUpdate();
        }
    }
    public void Btn_Equip()
    {
        ItemManager.Equip(selectedEquip.Value.ebp.part, selectedEquip.Key);
        selectedEquip = dummyEquip;
        currPart = EquipPart.None;

        CurrInfoPanelUpdate();
        SelectedInfoPanelUpdate();

        ItemTokenUpdate();
        StatTxtUpdate();
    }
    public void Btn_CurrEquip(int part)
    {
        if (currPart == (EquipPart)part)
            currPart = EquipPart.None;
        else
            currPart = (EquipPart)part;
        CurrInfoPanelUpdate();

        if (selectedEquip.Key != -1 && selectedEquip.Value.ebp.part != currPart)
        {
            selectedEquip = dummyEquip;
            SelectedInfoPanelUpdate();
        }
    }
    public void Btn_EquipToken(KeyValuePair<int, Equipment> p)
    {
        if(selectedEquip.Equals(p))
        {
            selectedEquip = dummyEquip;
        }
        else
        {
            selectedEquip = p;
            if (currPart != p.Value.ebp.part)
                Btn_CurrEquip((int)p.Value.ebp.part);
            CurrInfoPanelUpdate();
        }
        SelectedInfoPanelUpdate();
    }

    void CurrInfoPanelUpdate()
    {
        if(currPart == EquipPart.None)
        {
            currEquipPanel.gameObject.SetActive(false);
        }
        else
        {
            Equipment e = ItemManager.GetEquipment(currPart);
            if (e == null)
                currEquipPanel.gameObject.SetActive(false);
            else
            {
                currEquipPanel.InfoUpdate(e);
                currEquipPanel.gameObject.SetActive(true);
            }
        }
    }
    void SelectedInfoPanelUpdate()
    {
        if (selectedEquip.Equals(dummyEquip))
        {
            selectedEquipPanel.gameObject.SetActive(false);
            equipBtns.SetActive(false);
        }
        else
        {
            selectedEquipPanel.InfoUpdate(selectedEquip.Value);
            selectedEquipPanel.gameObject.SetActive(true);
            equipBtns.SetActive(true);
        }

    }
    #endregion Function_Item

    #region Function_Skill
    #region Category
    public void Btn_SwitchSkillType(int type)
    {
        currType = type;
        SkillTokenUpdate();
    }
    public void Btn_SwitchSkillLearned(int learned)
    {
        currLearned = learned;
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


        var showSkills = (from token in skills
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

            for (int i = 0; i < 3; i++)
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
    #endregion Function_Skill


}
