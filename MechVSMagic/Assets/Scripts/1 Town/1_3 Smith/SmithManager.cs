using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SmithManager : MonoBehaviour
{
    [SerializeField] TownManager TM;

    [Header("Equip Buttons")]
    [SerializeField] Transform btnParent;
    [SerializeField] Transform poolParent;
    [SerializeField] GameObject equipBtnPrefab;

    [Header("Category Panel")]
    //0 : Equip, 1 : Skill, 2 : Resource
    [SerializeField] GameObject[] categoryPanels;

    [SerializeField] EquipInfoPanel[] selectedEquipPanel = new EquipInfoPanel[3];
    KeyValuePair<int, Equipment> selectedEquip;
    static KeyValuePair<int, Equipment> dummyEquip;

    EquipBluePrint selectedEBP = null;
    Skillbook selectedSkillbook = null;

    List<EquipBtnSet> btnList = new List<EquipBtnSet>();
    List<EquipBtnSet> btnPool = new List<EquipBtnSet>();

    #region Category
    ItemCategory currCategory = ItemCategory.Weapon;
    //장비 전용
    Rarity currRarity = Rarity.None;
    //스킬북 전용, 0 : active, 1 : passive, -1 : all
    int currUseType = -1;
    //장비, 스킬북 0 : all, 1,3,5,7,9
    int currLvl = 0;
    #endregion

    private void Start()
    {/*
        ItemManager.ItemDrop(1, 84, 2);
        ItemManager.ItemDrop(1, 85, 3);
        ItemManager.ItemDrop(1, 86, 3);
        ItemManager.ItemDrop(1, 148, 1);*/
        ResetAllState();
    }

    public void ResetAllState()
    {
        ResetCategory();
    }
    void ResetCategory()
    {
        currRarity = Rarity.None;
        currUseType = -1;
        currLvl = 0;

        Btn_SwitchCategory((int)ItemCategory.Weapon);

        selectedEquip = dummyEquip;
        selectedEBP = null;
        selectedSkillbook = null;
        SelectedPanelUpdate();
    }

    #region Btn Image Update
    void TokenBtnUpdate()
    {
        TokenBtnReset();
        if (currCategory <= ItemCategory.Accessory)
            BtnUpdate_Equip();
        else if (currCategory <= ItemCategory.Recipe)
            BtnUpdate_Recipe();
        else if (currCategory <= ItemCategory.Skillbook)
            BtnUpdate_Skillbook();
        else
            BtnUpdate_Resource();
        

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
                GameObject go = NewBtnSet();
                go.transform.parent = btnParent;
                btnList.Add(go.GetComponent<EquipBtnSet>());
                btnList[btnList.Count - 1].Init(this, idxs);
                go.SetActive(true);

                idxs.Clear();
            }
        }
        void BtnUpdate_Recipe()
        {
            List<EquipBluePrint> recipe = ItemManager.GetRecipeData(currRarity, currLvl);

            List<KeyValuePair<int, EquipBluePrint>> idxs = new List<KeyValuePair<int, EquipBluePrint>>();

            for (int i = 0; i < recipe.Count;)
            {
                while (idxs.Count < 4 && i < recipe.Count)
                {
                    idxs.Add(new KeyValuePair<int, EquipBluePrint>(i, recipe[i]));
                    i++;
                }

                GameObject go = NewBtnSet();
                go.transform.parent = btnParent;
                btnList.Add(go.GetComponent<EquipBtnSet>());
                btnList[btnList.Count - 1].Init(this, idxs);
                go.SetActive(true);

                idxs.Clear();
            }
        }
        void BtnUpdate_Skillbook()
        {
            List<Skillbook> now = ItemManager.GetSkillbookData(currUseType, currLvl);

            List<KeyValuePair<int, Skillbook>> idxs = new List<KeyValuePair<int, Skillbook>>();
            for (int i = 0; i < now.Count;)
            {
                while (idxs.Count < 4 && i < now.Count)
                {
                    idxs.Add(new KeyValuePair<int, Skillbook>(i, now[i]));
                    i++;
                }
                GameObject go = NewBtnSet();
                go.transform.parent = btnParent;
                btnList.Add(go.GetComponent<EquipBtnSet>());
                btnList[btnList.Count - 1].Init(this, idxs);
                go.SetActive(true);

                idxs.Clear();
            }
        }
        void BtnUpdate_Resource()
        {

        }
    }
    void TokenBtnReset()
    {
        for(int i =0;i<btnList.Count;i++)
        {
            btnList[i].gameObject.SetActive(false);
            btnList[i].transform.parent = poolParent;
            btnPool.Add(btnList[i]);
        }
        btnList.Clear();
    }
    GameObject NewBtnSet()
    {
        if (btnPool.Count > 0)
        {
            GameObject go = btnPool[0].gameObject;
            btnPool.RemoveAt(0);
            return go;
        }
        else
            return Instantiate(equipBtnPrefab);
    }
    #endregion

    #region Tag Btn
    public void Btn_SwitchCategory(int category)
    {
        currCategory = (ItemCategory)category;
        CategoryBtnUpdate();
        TokenBtnUpdate();

        selectedEquip = dummyEquip;
        selectedEBP = null;
        selectedSkillbook = null;
        SelectedPanelUpdate();

        void CategoryBtnUpdate()
        {
            int curr = 0;
            if (currCategory == ItemCategory.Skillbook)
                curr = 1;
            else if (currCategory == ItemCategory.Resource)
                curr = 2;

            for (int i = 0; i < 3; i++)
                categoryPanels[i].SetActive(i == curr);
        }
    }
    public void Btn_SwitchRarity(int rarity)
    {
        currRarity = (Rarity)rarity;
        TokenBtnUpdate();
    }
    public void Btn_SwitchSkillUseType(int type)
    {
        currUseType = type;
        TokenBtnUpdate();
    }
    public void Btn_SwitchLvl(int lvl)
    {
        currLvl = lvl;
        TokenBtnUpdate();
    }
    #endregion
    
    public void Btn_EquipToken(KeyValuePair<int, Equipment> token)
    {
        if (selectedEquip.Equals(token))
            selectedEquip = dummyEquip;
        else
            selectedEquip = token;

        SelectedPanelUpdate();
    }
    public void Btn_EBPToken(EquipBluePrint token)
    {
        if (selectedEBP == token) 
            selectedEBP = null;
        else
            selectedEBP = token;

        SelectedPanelUpdate();
    }
    public void Btn_SkillbookToken(Skillbook token)
    {
        if (selectedSkillbook == token) 
            selectedSkillbook = null;
        else
            selectedSkillbook = token;

        SelectedPanelUpdate();
    }

    public void Btn_EquipFusion()
    {
        if (ItemManager.CanFusion(selectedEquip.Value.ebp.part, selectedEquip.Key))
        {
            ItemManager.FusionEquipment(selectedEquip.Value.ebp.part, selectedEquip.Key);
            selectedEquip = dummyEquip;
            TokenBtnUpdate();
            SelectedPanelUpdate();
        }
        else
            Debug.Log("There is no same Equipment");
    }
    public void Btn_EquipOptionSwitch()
    {
        if (ItemManager.CanSwitchOption(selectedEquip.Value.ebp.part, selectedEquip.Key))
        {
            ItemManager.SwitchEquipOption(selectedEquip.Value.ebp.part, selectedEquip.Key);
            TokenBtnUpdate();
            SelectedPanelUpdate();
        }
        else
            Debug.Log("You Can't Switch this Equipment");
    }
    public void Btn_EquipDisassemble()
    {
        ItemManager.DisassembleEquipment(selectedEquip.Value.ebp.part, selectedEquip.Key);
        selectedEquip = dummyEquip;
        TokenBtnUpdate();
        SelectedPanelUpdate();
    }

    public void Btn_EquipSmith()
    {
        if (ItemManager.CanSmith(selectedEBP.idx))
            ItemManager.SmithEquipment(selectedEBP.idx);
        else
            Debug.Log("not enough resources");
    }

    public void Btn_SkillLearn()
    {
        ItemManager.SkillLearn(selectedSkillbook.idx);
    }
    public void Btn_SkillbookDisassemble()
    {

    }

    void SelectedPanelUpdate()
    {
        foreach (EquipInfoPanel e in selectedEquipPanel)
            e.gameObject.SetActive(false);

        if (currCategory <= ItemCategory.Accessory && !selectedEquip.Equals(dummyEquip))
        {
            selectedEquipPanel[0].InfoUpdate(selectedEquip.Value);
            selectedEquipPanel[0].gameObject.SetActive(true);
        }
        else if (currCategory <= ItemCategory.Recipe && selectedEBP != null)
        {
            selectedEquipPanel[1].InfoUpdate(selectedEBP);
            selectedEquipPanel[1].gameObject.SetActive(true);
        }
        else if (currCategory <= ItemCategory.Skillbook && selectedSkillbook != null)
        {
            selectedEquipPanel[2].InfoUpdate(selectedSkillbook);
            selectedEquipPanel[2].gameObject.SetActive(true);
        }
    }

    void Btn_Smith(int idx)
    {
        //재료 검사
        //제작
    }
}
