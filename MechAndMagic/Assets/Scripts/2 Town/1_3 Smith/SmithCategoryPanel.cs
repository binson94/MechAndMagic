using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum SmithCategory
{
    EquipTotal, Weapon, Armor, Accessory, RecipeTotal, WeaponRecipe, ArmorRecipe, AccessoryRecipe, Skillbook, Resource
}

public class SmithCategoryPanel : MonoBehaviour, ITownPanel
{
    [SerializeField] SmithPanel SP;

    ///<summary> 카테고리 선택 버튼들 UI Set
    ///<para> 0 : category, 1 : rarity, 2 : level, 3 : skillType </para></summary>
    [Header("Category")] [Tooltip("0 : category, 1 : rarity, 2 : level, 3 : skillType")]
    [SerializeField] GameObject[] categorySelectPanels;
    ///<summary> 스킬북 선택 시, 레어도 -> 스킬 타입
    ///<para> 0 : rarity Btn, 1 : skillType Btn </para></summary>
    [SerializeField] GameObject[] categoryBtns;

    ///<summary> 카테고리 </summary>
    SmithCategory currCategory = SmithCategory.EquipTotal;
    public SmithCategory CurrCategory { get => currCategory; }
    ///<summary> 카테고리-장비 레어도 </summary>
    Rarity currRarity = Rarity.None;
    public Rarity CurrRarity { get => currRarity; }
    ///<summary> 카테고리-스킬 타입
    ///<para> 0 : active, 1 : passive, -1 : all </para></summary>
    int currUseType = -1;
    public int CurrUseType { get => currUseType; }
    ///<summary> 카테고리-레벨
    ///<para> 0 : all, 1, 3, 5, 7, 9 </para></summary>
    int currLvl = 0;
    public int CurrLvl { get => currLvl; }

    ///<summary> 재료 보여주는 UI Set </summary>
    [SerializeField] ResourcePanel resourcePanel;

    public void ResetAllState()
    {
        currRarity = Rarity.None;
        currUseType = -1;
        currLvl = 0;
        //기본 상태 - 장비 전체 보여주기
        Btn_SwitchCategory(0);
        foreach(GameObject panel in categorySelectPanels) panel.SetActive(false);
    }

    
    ///<summary> 카테고리, 등급, 레벨, 스킬 타입 버튼 선택 시 세부 선택 판넬 보이기 </summary>
    ///<param name="panelIdx"> 0 category, 1 rarity, 2 lvl, 3 skillType </param>
    public void Btn_OpenCategorySelectPanel(int panelIdx)
    {
        if(panelIdx > 0 && currCategory == SmithCategory.Resource) return;

        for(int i = 0;i < categorySelectPanels.Length;i++)
            categorySelectPanels[i].SetActive(i == panelIdx);

        resourcePanel.gameObject.SetActive(false);
    }
    ///<summary> 카테고리 세부 선택 판넬에서 카테고리 변경 </summary>
    ///<param name="category"> 0 장비 전체, 1 무기, 2 방어구, 3 장신구, 4 제작법 전체, 5 무기제작, 6 방어구 제작, 7 악세 제작, 8 스킬북, 9 재료 </param>
    public void Btn_SwitchCategory(int category)
    {
        //카테고리 변경
        currCategory = (SmithCategory)category;

        //스킬북이면 스킬 타입 선택 버튼으로 변경
        for (int i = 0; i < categoryBtns.Length; i++)
            categoryBtns[i].SetActive(i == 0 ^ currCategory == SmithCategory.Skillbook);
        Btn_OpenCategorySelectPanel(-1);

        SP.TokenBtnUpdate();
        SP.ResetSelectInfo();

        if(currCategory == SmithCategory.Resource)
        {
            resourcePanel.ResetAllState();
            resourcePanel.gameObject.SetActive(true);
        }
    }
    ///<summary> 등급 세부 선택 판넬에서 등급 변경 </summary>
    ///<param name="rarity"> 1 일반, 2 고급, 3 희귀, 3 고유, 4 전설 </param>
    public void Btn_SwitchRarity(int rarity)
    {
        currRarity = (Rarity)rarity;
        SP.TokenBtnUpdate();
        Btn_OpenCategorySelectPanel(-1);
    }
    ///<summary> 레벨 세부 선택 판넬에서 레벨 변경 </summary>
    ///<param name="lvl"> 0 전체, 1, 3, 5, 7, 9 </param>
    public void Btn_SwitchLvl(int lvl)
    {
        currLvl = lvl;
        SP.TokenBtnUpdate();
        Btn_OpenCategorySelectPanel(-1);
    }
    ///<summary> 스킬 타입 세부 선택 판넬에서 스킬 타입 변경 </summary>
    ///<param name="type"> 0 액티브, 1 패시브 </param>
    public void Btn_SwitchSkillUseType(int type)
    {
        currUseType = type;
        SP.TokenBtnUpdate();
        Btn_OpenCategorySelectPanel(-1);
    }
}
