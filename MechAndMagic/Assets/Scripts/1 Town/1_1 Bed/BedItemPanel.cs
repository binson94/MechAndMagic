using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BedItemPanel : MonoBehaviour, ITownPanel
{
    [SerializeField] BedPanel BP;

    [Header("Equip Panel")]
    #region Variable_Equip
    [SerializeField] Transform equipTokenParent;
    [SerializeField] Transform equipTokenPoolParent;
    [SerializeField] GameObject equipTokenPrefab;


    #region Variable_Category
    //1Weapon, 2Armor, 3Accessory, 7Potion
    ItemCategory currCategory = ItemCategory.Weapon;
    Rarity currRarity = Rarity.None;
    //0 : all, 1,3,5,7,9
    int currLvl = 0;
    [SerializeField] GameObject[] categorySelectPanels;
    #endregion Variable_Category

    EquipPart currPart = EquipPart.None;
    [SerializeField] EquipInfoPanel currEquipPanel;
   [SerializeField] KeyValuePair<int, Equipment> selectedEquip;
    [SerializeField] EquipInfoPanel selectedEquipPanel;
    [SerializeField] GameObject equipBtns;
    static KeyValuePair<int, Equipment> dummyEquip = new KeyValuePair<int, Equipment>(-1, null);

    List<EquipBtnToken> equipTokenList = new List<EquipBtnToken>();
    List<EquipBtnToken> equipTokenPool = new List<EquipBtnToken>();

    [SerializeField] UnityEngine.UI.Text[] statDelta;
    #endregion

    public void ResetAllState()
    {
        currCategory = ItemCategory.Weapon;
        currRarity = Rarity.None;
        currLvl = 0;
        ItemTokenUpdate();
        
        currPart = EquipPart.None;
        selectedEquip = dummyEquip;
        CurrInfoPanelUpdate();
        SelectedInfoPanelUpdate();
    }

    #region Category
    public void Btn_OpenSelectPanel(int kind)
    {
        currPart = EquipPart.None;
        selectedEquip = dummyEquip;
        CurrInfoPanelUpdate();
        SelectedInfoPanelUpdate();
        
        for(int i = 0;i < categorySelectPanels.Length;i++)
            categorySelectPanels[i].SetActive(i == kind);
    }
    public void Btn_SwitchCategory(int category)
    {
        currCategory = (ItemCategory)category;
        Btn_OpenSelectPanel(-1);
        ItemTokenUpdate();
    }
    public void Btn_SwitchRarity(int rarity)
    {
        currRarity = (Rarity)rarity;
        Btn_OpenSelectPanel(-1);
        ItemTokenUpdate();
    }
    public void Btn_SwitchLvl(int lvl)
    {
        currLvl = lvl;
        Btn_OpenSelectPanel(-1);
        ItemTokenUpdate();
    }
    #endregion Category
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
                equipTokenList.Add(go.GetComponent<EquipBtnToken>());
                equipTokenList[equipTokenList.Count - 1].Init(this, idxs);
                go.SetActive(true);

                idxs.Clear();
            }
            
            for(int i = equipTokenList.Count;i < 4;i++)
            {
                GameObject go = NewItemToken();
                go.transform.SetParent(equipTokenParent);
                equipTokenList.Add(go.GetComponent<EquipBtnToken>());
                equipTokenList[equipTokenList.Count - 1].Init(this, idxs);
                go.SetActive(true);
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
        BP.StatTxtUpdate();
    }
    public void Btn_ToSmith() => BP.BedToSmith(currCategory, currRarity, currLvl, selectedEquip);
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
        foreach(UnityEngine.UI.Text t in statDelta) t.text = string.Empty;

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

            int[] newDelta = ItemManager.GetStatDelta(selectedEquip.Value);
            for(int i = 0;i < 10;i++)
                if(newDelta[i] > 0)
                {
                    statDelta[i].text = string.Concat("+", newDelta[i]);
                    if(i == 6 || i == 7)
                        statDelta[i].text = string.Concat(statDelta[i].text, "%");

                    statDelta[i].text = string.Concat("<color=#82e67c>", statDelta[i].text, "</color>");
                }
                else if (newDelta[i] < 0)
                {
                    statDelta[i].text = string.Concat(newDelta[i]);
                    if(i == 6 || i == 7)
                        statDelta[i].text = string.Concat(statDelta[i].text, "%");
                        
                    statDelta[i].text = string.Concat("<color=#f93f3d>", statDelta[i].text, "</color>");
                }

        }
    }
}
