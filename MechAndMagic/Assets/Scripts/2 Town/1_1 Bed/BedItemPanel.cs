using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BedItemPanel : MonoBehaviour, ITownPanel
{
    ///<summary> 대장간으로 이동 처리를 위한 상위 매니저 </summary>
    [SerializeField] BedPanel BP;


    ///<summary> 아이템 버튼 토큰 부모 스크롤 뷰 오브젝트 </summary>
    [Header("Equip Token")]
    [SerializeField] RectTransform equipTokenParent;
    ///<summary> 풀에 넣은 토큰들 부모 오브젝트 </summary>
    [SerializeField] RectTransform equipTokenPoolParent;
    ///<summary> 아이템 버튼 토큰 프리팹, 한 오브젝트에 4개씩 처리 </summary>
    [SerializeField] EquipBtnToken equipTokenPrefab;

    ///<summary> 0 category, 1 rarity, 2 lvl </summary>
    [Header("Category")]
    [SerializeField] GameObject[] categorySelectPanels;
    ///<summary> 1Weapon, 2Armor, 3Accessory, 7Potion </summary>
    ItemCategory currCategory = ItemCategory.Weapon;
    Rarity currRarity = Rarity.None;
    ///<summary> 0 : all, 1,3,5,7,9 </summary>
    int currLvl = 0;


    ///<summary> 현재 선택한 슬롯 </summary>
    EquipPart slotPart = EquipPart.None;
    ///<summary> 현재 선택한 슬롯 장비 정보 표시 UI Set </summary>
    [SerializeField] EquipInfoPanel slotEquipPanel;
    ///<summary> 새로 선택한 장비 list에서의 idx와 장비 정보 pair </summary>
    [SerializeField] KeyValuePair<int, Equipment> selectedEquip;
    ///<summary> 새로 선택한 장비 정보 표시 UI Set </summary>
    [SerializeField] EquipInfoPanel selectedEquipPanel;
    ///<summary> 장비 및 대장간으로 버튼 표기/숨김을 위한 오브젝트 </summary>
    [SerializeField] GameObject equipBtns;
    ///<summary> 아무것도 선택하지 않은 상태를 위한 더미 오브젝트 </summary>
    static KeyValuePair<int, Equipment> dummyEquip = new KeyValuePair<int, Equipment>(-1, null);

    ///<summary> 표시 중인 아이템 버튼 토큰 </summary>
    List<EquipBtnToken> equipTokenList = new List<EquipBtnToken>();
    ///<summary> 아이템 버튼 토큰 풀 </summary>
    Queue<EquipBtnToken> equipTokenPool = new Queue<EquipBtnToken>();

    ///<summary> 선택한 장비 장착 시 변화하는 스텟 수치 표기 텍스트 </summary>
    [SerializeField] Text[] statDelta;

    ///<summary> 포션 종류 선택 버튼의 부모 오브젝트 </summary>
    [Header("Potion")]
    [SerializeField] GameObject potionSelectPanel;
    ///<summary> 현재 선택한 슬롯 포션 정보 표시 UI Set </summary>
    [SerializeField] PotionInfoPanel potionSlotPanel;
    ///<summary> 현재 선택한 포션 정보 표시 UI Set </summary>
    [SerializeField] PotionInfoPanel selectedPotionPanel;
    ///<summary> 현재 선택한 포션 Idx </summary>
    int selectedPotion;
    ///<summary> 포션 장착 버튼 </summary>
    [SerializeField] GameObject potionEquipBtn;

    ///<summary> 상태 초기화 - 카테고리 초기화, 아이템 선택 상태 초기화 </summary>
    public void ResetAllState()
    {
        currCategory = ItemCategory.None;
        currRarity = Rarity.None;
        currLvl = 0;
        ItemTokenUpdate();
        
        slotPart = EquipPart.None;
        selectedEquip = dummyEquip;
        SlotInfoPanelUpdate();
        SelectedInfoPanelUpdate();
        
        potionSelectPanel.SetActive(false); potionEquipBtn.SetActive(false);
        potionSlotPanel.gameObject.SetActive(false); selectedPotionPanel.gameObject.SetActive(false);
    }

    #region Category
    ///<summary> 카테고리 변경을 위한 UI Set 열기 </summary>
    ///<param name="kind"> categorySelectPanels의 idx(0 ~ 2) </param>
    public void Btn_OpenSelectPanel(int kind)
    {
        slotPart = EquipPart.None;
        selectedEquip = dummyEquip;
        SlotInfoPanelUpdate();
        SelectedInfoPanelUpdate();
        
        for(int i = 0;i < categorySelectPanels.Length;i++)
            categorySelectPanels[i].SetActive(i == kind);
    }
    ///<summary> 아이템 카테고리 변경 </summary>
    public void Btn_SwitchCategory(int category)
    {
        currCategory = (ItemCategory)category;
        Btn_OpenSelectPanel(-1);
        ItemTokenUpdate();

        potionSelectPanel.SetActive(false); potionEquipBtn.SetActive(false);
        potionSlotPanel.gameObject.SetActive(false); selectedPotionPanel.gameObject.SetActive(false);
    }
    ///<summary> 아이템 레어리티 변경 </summary>
    public void Btn_SwitchRarity(int rarity)
    {
        currRarity = (Rarity)rarity;
        Btn_OpenSelectPanel(-1);
        ItemTokenUpdate();
    }
    ///<summary> 아이템 레벨 변경 </summary>
    public void Btn_SwitchLvl(int lvl)
    {
        currLvl = lvl;
        Btn_OpenSelectPanel(-1);
        ItemTokenUpdate();
    }
    #endregion Category
    #region Token Update
    ///<summary> 현재 카테고리에 맞는 버튼 토큰 생성 </summary>
    void ItemTokenUpdate()
    {
        ItemTokenReset();

        //None 전체, Weapon, Armor, Accessory
        if (currCategory <= ItemCategory.Accessory)
        {
            List<KeyValuePair<int, Equipment>> categorizedEquips = ItemManager.GetEquipData(currCategory, currRarity, currLvl);

            for (int i = 0; i < categorizedEquips.Count; i += 4)
            {
                EquipBtnToken token = GameManager.GetToken(equipTokenPool, equipTokenParent, equipTokenPrefab);

                token.Initialize(this, i, categorizedEquips);
                equipTokenList.Add(token);
                token.gameObject.SetActive(true);
            }

            for (int i = equipTokenList.Count; i < 5; i++)
            {
                EquipBtnToken token = GameManager.GetToken(equipTokenPool, equipTokenParent, equipTokenPrefab);

                token.Initialize(this, 0, null);
                equipTokenList.Add(token);
                token.gameObject.SetActive(true);
            }
        }
    }
    ///<summary> 버튼 토큰 초기화, 전부 풀에 삽입 </summary>
    void ItemTokenReset()
    {
        for (int i = 0; i < equipTokenList.Count; i++)
        {
            equipTokenList[i].gameObject.SetActive(false);
            equipTokenList[i].transform.SetParent(equipTokenPoolParent);
            equipTokenPool.Enqueue(equipTokenList[i]);
        }
        equipTokenList.Clear();
    }
    #endregion Token Update
    #region Potion
    ///<summary> 아이템 카테고리 포션으로 변경 </summary>
    public void Btn_OpenPotion()
    {
        Btn_OpenSelectPanel(-1);
        ItemTokenUpdate();

        potionSelectPanel.SetActive(true);

        potionSlotPanel.InfoUpdate(0);
        potionSlotPanel.gameObject.SetActive(true);

        selectedPotion = 0;
        selectedPotionPanel.InfoUpdate(0);
        selectedPotionPanel.gameObject.SetActive(true);
    }
    ///<summary> 선택한 포션 슬롯 정보 표시 </summary>
    public void Btn_PotionSlot(int slotIdx)
    {
        foreach(GameObject panel in categorySelectPanels) panel.SetActive(false);
        potionSelectPanel.SetActive(true);
        potionSlotPanel.gameObject.SetActive(true);
        selectedPotionPanel.gameObject.SetActive(true);
        equipBtns.SetActive(false);

        potionSlotPanel.InfoUpdate(GameManager.instance.slotData.potionSlot[slotIdx]);
        selectedPotionPanel.InfoUpdate(0);
    }
    ///<summary> 포션 선택 버튼 </summary>
    ///<param name="potionIdx" 1 활력(AP 전체 회복), 2 정화(모든 디버프 해제), 3 회복(HP 전체 회복), 4 재활용(재사용 가능) </summary>
    public void Btn_SelectPotion(int potionIdx)
    {
        if(GameManager.instance.slotData.potionSlot[0] == potionIdx || GameManager.instance.slotData.potionSlot[1] == potionIdx)
        {    
            selectedPotion = 0;
            potionSlotPanel.InfoUpdate(potionIdx);
            selectedPotionPanel.InfoUpdate(0);
            potionEquipBtn.SetActive(false);
        }
        else
        {
            selectedPotion = potionIdx;
            potionSlotPanel.InfoUpdate(GameManager.instance.slotData.potionSlot[0]);
            selectedPotionPanel.InfoUpdate(potionIdx);
            potionEquipBtn.SetActive(true);
        }
    }
    ///<summary> 포션 장착 버튼 </summary>
    public void Btn_EquipPotion()
    {
        ItemManager.EquipPotion(selectedPotion);
        selectedPotion = 0;
        potionSlotPanel.InfoUpdate(0);
        selectedPotionPanel.InfoUpdate(0);
        potionEquipBtn.SetActive(false);

        BP.EquipIconUpdate();
    }  
    #endregion Potion
    
    public void Btn_UnEquip()
    {
        if(slotPart != EquipPart.None)
        {
            ItemManager.UnEquip(slotPart);
            ItemTokenUpdate();
        }
    }
    ///<summary> 장비 장착 버튼, 새로 선택한 장비 장착 </summary>
    public void Btn_Equip()
    {
        ItemManager.Equip(selectedEquip.Value.ebp.part, selectedEquip.Key);
        selectedEquip = dummyEquip;
        slotPart = EquipPart.None;

        SlotInfoPanelUpdate();
        SelectedInfoPanelUpdate();

        ItemTokenUpdate();
        BP.StatTxtUpdate();
        BP.SetTxtUpdate();
        BP.EquipIconUpdate();
    }
    ///<summary> 대장간으로 넘어가기, 현재 선택 카테고리 및 장비 유지 </summary>
    public void Btn_ToSmith() => BP.BedToSmith(currCategory, currRarity, currLvl, selectedEquip);
    ///<summary> 슬롯에 장착 중인 장비 정보 표시 </summary>
    public void Btn_EquipSlot(int part)
    {
        slotPart = (EquipPart)part;
        SlotInfoPanelUpdate();

        potionSelectPanel.SetActive(false); potionEquipBtn.SetActive(false);
        potionSlotPanel.gameObject.SetActive(false); selectedPotionPanel.gameObject.SetActive(false);

        if (selectedEquip.Key != -1 && (selectedEquip.Value == null || selectedEquip.Value.ebp.part != slotPart))
        {
            selectedEquip = dummyEquip;
            SelectedInfoPanelUpdate();
        }
    }
    //<summary> 새로운 장비 선택 </summary>
    public void Btn_EquipToken(KeyValuePair<int, Equipment> p)
    {
        if(selectedEquip.Equals(p))
        {
            selectedEquip = dummyEquip;
        }
        else
        {
            selectedEquip = p;
            if (slotPart != p.Value.ebp.part)
                Btn_EquipSlot((int)p.Value.ebp.part);
            SlotInfoPanelUpdate();
        }
        SelectedInfoPanelUpdate();
    }

    ///<summary> 선택한 장비 슬롯 정보 UI 업데이트 </summary>
    void SlotInfoPanelUpdate() => slotEquipPanel.InfoUpdate(ItemManager.GetEquipment(slotPart));
    ///<summary> 새로운 장비 선택 정보 UI 업데이트 </summary>
    void SelectedInfoPanelUpdate()
    {
        foreach(UnityEngine.UI.Text t in statDelta) t.text = string.Empty;

        if (selectedEquip.Equals(dummyEquip))
        {
            selectedEquipPanel.InfoUpdate(null as Equipment);
            equipBtns.SetActive(false);
        }
        else
        {
            selectedEquipPanel.InfoUpdate(selectedEquip.Value);
            equipBtns.SetActive(true);

            int[] newDelta = ItemManager.GetStatDelta(selectedEquip.Value);
            for(int i = 0;i < 10;i++)
                if(newDelta[i] > 0)
                {
                    statDelta[i].text = $"+{newDelta[i]}";
                    if(i == 6 || i == 7)
                        statDelta[i].text = $"{statDelta[i].text}%";

                    statDelta[i].text = $"<color=#82e67c>{statDelta[i].text}</color>";
                }
                else if (newDelta[i] < 0)
                {
                    statDelta[i].text = newDelta[i].ToString();
                    if(i == 6 || i == 7)
                        statDelta[i].text = $"{statDelta[i].text}%";
                        
                    statDelta[i].text = $"<color=#f93f3d>{statDelta[i].text}</color>";
                }

        }
    }
}
