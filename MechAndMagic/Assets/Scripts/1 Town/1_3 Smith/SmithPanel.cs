using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SmithPanel : MonoBehaviour, ITownPanel
{
    [Header("Stat Show")]
    [SerializeField] UnityEngine.UI.Text[] statTxts;
    [SerializeField] UnityEngine.UI.Slider expSlider;

    [Header("Equip List")]
    [SerializeField] Transform btnParent;
    [SerializeField] Transform poolParent;
    [SerializeField] GameObject equipBtnPrefab;
    List<EquipBtnSet> btnList = new List<EquipBtnSet>();
    List<EquipBtnSet> btnPool = new List<EquipBtnSet>();

    #region Category
    [Header("Category")]
    //0 : category, 1 : rarity, 2 : level, 3 : skillType
    [SerializeField] GameObject[] categorySelectPanels;
    //0 : rarity Btn, 1 : skillType Btn
    [SerializeField] GameObject[] categoryBtns;
    ItemCategory currCategory = ItemCategory.Weapon;
    //장비 전용
    Rarity currRarity = Rarity.None;
    //스킬북 전용, 0 : active, 1 : passive, -1 : all
    int currUseType = -1;
    //장비, 스킬북 0 : all, 1,3,5,7,9
    int currLvl = 0;
    #endregion Category

    #region Work Panel
    [SerializeField] EquipInfoPanel selectedEquipPanel;
    //-1 : close, 0 : equipCommon, 1 : equipMerge, 2 : equipOptionSwitch, 3 : EquipDisassemble, 4 : EBPCreate, 5 : Skillbook
    int currWorkPanel;
    [SerializeField] GameObject[] workPanels;
    KeyValuePair<int, Equipment> selectedEquip;
    static KeyValuePair<int, Equipment> dummyEquip;

    EquipBluePrint selectedEBP = null;
    Skillbook selectedSkillbook = null;
    #endregion Work Panel

    private void Start() {
        ItemManager.ItemDrop(84, 1);
        ItemManager.ItemDrop(85, 1);
        ItemManager.ItemDrop(86, 1);
        ItemManager.ItemDrop(87, 1);
        ItemManager.ItemDrop(23, 1);
    }
    public void ResetAllState()
    {
        currRarity = Rarity.None;
        currUseType = -1;
        currLvl = 0;

        StatTxtUpdate();
        Btn_SwitchCategory((int)ItemCategory.Weapon);
        Btn_OpenCategorySelectPanel(-1);

        selectedEquip = dummyEquip;
        selectedEBP = null;
        selectedSkillbook = null;
        SelectedPanelUpdate();
    }

    #region Category
    public void Btn_OpenCategorySelectPanel(int panelIdx)
    {
        for(int i = 0;i < categorySelectPanels.Length;i++)
            categorySelectPanels[i].SetActive(i == panelIdx);

        selectedEquip = dummyEquip;
        selectedEBP = null;
        selectedSkillbook = null;
        SelectedPanelUpdate();
        Btn_OpenWorkPanel(-1);
    }
    public void Btn_SwitchCategory(int category)
    {
        currCategory = (ItemCategory)category;
        CategoryBtnUpdate();
        TokenBtnUpdate();

        selectedEquip = dummyEquip;
        selectedEBP = null;
        selectedSkillbook = null;
        SelectedPanelUpdate();
        Btn_OpenCategorySelectPanel(-1);

        void CategoryBtnUpdate()
        {
            int curr = 0;
            if (currCategory == ItemCategory.Skillbook)
                curr = 1;

            for(int i =0;i<categoryBtns.Length;i++)
                categoryBtns[i].SetActive(i == curr);
        }
    }
    public void Btn_SwitchRarity(int rarity)
    {
        currRarity = (Rarity)rarity;
        TokenBtnUpdate();
        Btn_OpenCategorySelectPanel(-1);
    }
    public void Btn_SwitchSkillUseType(int type)
    {
        currUseType = type;
        TokenBtnUpdate();
        Btn_OpenCategorySelectPanel(-1);
    }
    public void Btn_SwitchLvl(int lvl)
    {
        currLvl = lvl;
        TokenBtnUpdate();
        Btn_OpenCategorySelectPanel(-1);
    }
    #endregion
    
    #region Btn Image Update
    void TokenBtnUpdate()
    {
        TokenBtnReset();
        if (currCategory <= ItemCategory.Accessory)
            BtnUpdate_Equip();
        else if (currCategory <= ItemCategory.Recipe)
            BtnUpdate_Recipe();
        else
            BtnUpdate_Skillbook();
        

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
                go.transform.SetParent(btnParent);
                btnList.Add(go.GetComponent<EquipBtnSet>());
                btnList[btnList.Count - 1].Init(this, idxs);
                go.SetActive(true);

                idxs.Clear();
            }

            for(int i = btnList.Count;i < 4;i++)
            {
                GameObject go = NewBtnSet();
                go.transform.SetParent(btnParent);
                btnList.Add(go.GetComponent<EquipBtnSet>());
                btnList[btnList.Count - 1].Init(this, idxs);
                go.SetActive(true);
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
                go.transform.SetParent(btnParent);
                btnList.Add(go.GetComponent<EquipBtnSet>());
                btnList[btnList.Count - 1].Init(this, idxs);
                go.SetActive(true);

                idxs.Clear();
            }
            
            for(int i = btnList.Count;i < 4;i++)
            {
                GameObject go = NewBtnSet();
                go.transform.SetParent(btnParent);
                btnList.Add(go.GetComponent<EquipBtnSet>());
                btnList[btnList.Count - 1].Init(this, idxs);
                go.SetActive(true);
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
                go.transform.SetParent(btnParent);
                btnList.Add(go.GetComponent<EquipBtnSet>());
                btnList[btnList.Count - 1].Init(this, idxs);
                go.SetActive(true);

                idxs.Clear();
            }
            
            for(int i = btnList.Count;i < 4;i++)
            {
                GameObject go = NewBtnSet();
                go.transform.SetParent(btnParent);
                btnList.Add(go.GetComponent<EquipBtnSet>());
                btnList[btnList.Count - 1].Init(this, idxs);
                go.SetActive(true);
            }
        }
    }
    void TokenBtnReset()
    {
        for(int i =0;i<btnList.Count;i++)
        {
            btnList[i].gameObject.SetActive(false);
            btnList[i].transform.SetParent(poolParent);
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

    #region Work Panels
    public void Btn_OpenWorkPanel(int workPanelIdx)
    {
        if(workPanelIdx == 2 && !GameManager.instance.slotData.itemData.CanFusion(selectedEquip.Value.ebp.part, selectedEquip.Key))
        {
            Debug.Log("이 장비는 옵션 변경이 불가능합니다.");
            return;
        }

        for(int i = 0;i < workPanels.Length;i++)
            workPanels[i].SetActive(i == workPanelIdx);
    }

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

    public void Btn_Fusion()
    {
        if (GameManager.instance.slotData.itemData.CanFusion(selectedEquip.Value.ebp.part, selectedEquip.Key))
        {
            ItemManager.FusionEquipment(selectedEquip.Value.ebp.part, selectedEquip.Key);
            selectedEquip = dummyEquip;
            TokenBtnUpdate();
            SelectedPanelUpdate();
        }
        else
            Debug.Log("There is no same Equipment");
    }
    public void Btn_SwitchOption()
    {
        if (GameManager.instance.slotData.itemData.CanSwitchCommonStat(selectedEquip.Value.ebp.part, selectedEquip.Key))
        {
            ItemManager.SwitchEquipOption(selectedEquip.Value.ebp.part, selectedEquip.Key);
            TokenBtnUpdate();
            SelectedPanelUpdate();
        }
    }
    public void Btn_Disassemble()
    {
        ItemManager.DisassembleEquipment(selectedEquip.Value.ebp.part, selectedEquip.Key);
        selectedEquip = dummyEquip;
        TokenBtnUpdate();
        SelectedPanelUpdate();
    }
    public void Btn_Create()
    {
        if (ItemManager.CanSmith(selectedEBP.idx))
            ItemManager.SmithEquipment(selectedEBP.idx);
        else
            Debug.Log("not enough resources");
    }
    public void Btn_Cancel()
    {
        selectedEquip = dummyEquip;
        selectedEBP = null;
        selectedSkillbook = null;
        SelectedPanelUpdate();
    }
   
    public void Btn_SkillLearn()
    {
        if(GameManager.instance.slotData.itemData.IsLearned(selectedSkillbook.idx))
        {
            Debug.Log("이미 학습했습니다.");
            return;
        }

        ItemManager.SkillLearn(selectedSkillbook.idx);
        ItemManager.DisassembleSkillBook(selectedSkillbook.idx);
        selectedSkillbook = null;
        TokenBtnUpdate();
        SelectedPanelUpdate();
    }
    public void Btn_SkillbookDisassemble()
    {
        ItemManager.DisassembleSkillBook(selectedSkillbook.idx);
        selectedSkillbook = null;
        TokenBtnUpdate();
        SelectedPanelUpdate();
    }
    #endregion Work Panels

    void StatTxtUpdate()
    {
        statTxts[0].text = GameManager.instance.slotData.lvl.ToString();
        statTxts[1].text = string.Concat(GameManager.instance.slotData.exp, " / ", SlotData.reqExp[GameManager.instance.slotData.lvl]);
        expSlider.value = GameManager.instance.slotData.exp / (float)SlotData.reqExp[GameManager.instance.slotData.lvl];
        int i, j;
        for (i = j = 2; i < 13; i++, j++)
        {
            if (i == 3) i++;
            statTxts[j].text = GameManager.instance.slotData.itemStats[i].ToString();
        }

        statTxts[8].text = string.Concat(statTxts[8].text, "%");
        statTxts[9].text = string.Concat(statTxts[9].text, "%");
    }

    void SelectedPanelUpdate()
    {
        if (currCategory <= ItemCategory.Accessory && !selectedEquip.Equals(dummyEquip))
        {
            selectedEquipPanel.InfoUpdate(selectedEquip.Value);
            selectedEquipPanel.gameObject.SetActive(true);
            Btn_OpenWorkPanel(0);
        }
        else if (currCategory <= ItemCategory.Recipe && selectedEBP != null)
        {
            selectedEquipPanel.InfoUpdate(selectedEBP);
            selectedEquipPanel.gameObject.SetActive(true);
            Btn_OpenWorkPanel(4);
        }
        else if (currCategory <= ItemCategory.Skillbook && selectedSkillbook != null)
        {
            selectedEquipPanel.InfoUpdate(selectedSkillbook);
            selectedEquipPanel.gameObject.SetActive(true);
            Btn_OpenWorkPanel(5);
        }
        else
        {
            selectedEquipPanel.gameObject.SetActive(false);
            Btn_OpenWorkPanel(-1);
        }
    }
    public void BedToSmith(ItemCategory currC, Rarity currR, int currL, KeyValuePair<int, Equipment> selected)
    {
        currCategory = currC;
        currRarity = currR;
        currLvl = currL;
        selectedEquip = selected;
        TokenBtnUpdate();
        SelectedPanelUpdate();
        Btn_OpenWorkPanel(0);
    }
}
