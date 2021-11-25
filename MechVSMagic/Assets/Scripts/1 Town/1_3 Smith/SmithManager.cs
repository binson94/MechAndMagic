using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SmithManager : MonoBehaviour
{
    [Header("Equip Buttons")]
    [SerializeField] Transform btnParent;
    [SerializeField] Transform poolParent;
    [SerializeField] GameObject equipBtnPrefab;

    [Header("Category Panel")]
    //0 : Equip, 1 : Skill, 2 : Resource
    [SerializeField] GameObject[] categoryPanels;

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
    {
        Btn_SwitchCategory((int)ItemCategory.Weapon);
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

            List<int> idxs = new List<int>();
            for (int i = 0; i < now.Count;)
            {
                while (idxs.Count < 4 && i < now.Count)
                {
                    idxs.Add(i);
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
            int[] recipe = ItemManager.GetResourceData(ItemCategory.Resource);

            List<int> idxs = new List<int>();
            for (int i = 0; i < recipe.Length;)
            {
                for (int j = 0; j < 4 && i < recipe.Length;i++)
                {
                    if(recipe[i]  > 0)
                    {
                        idxs.Add(i);
                        j++;
                    }
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

            List<int> idxs = new List<int>();
            for (int i = 0; i < now.Count;)
            {
                while (idxs.Count < 4 && i < now.Count)
                {
                    idxs.Add(i);
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

    public void Btn_UnEquip(int part)
    {
        ItemManager.UnEquip((EquipPart)part);
        TokenBtnUpdate();
    }
    public void Btn_Equip(int idx)
    {
        ItemManager.Equip(currCategory, idx);
        TokenBtnUpdate();
    }
    public void Btn_Smith(int idx)
    {

    }
}
