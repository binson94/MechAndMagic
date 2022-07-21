using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class SmithPanel : MonoBehaviour, ITownPanel
{
    #region Variables
    #region PlayerInfo
    ///<summary> 클래스 이름 표시 </summary>
    [Header("Player Info")]
    [SerializeField] Text classTxt;
    ///<summary> 플레이어 스탯 표시 </summary>
    [SerializeField] Text[] statTxts;
    ///<summary> 경험치 표시 </summary>
    [SerializeField] Slider expSlider;
    ///<summary> 장착 중인 장비 표시 </summary>
    [SerializeField] Image[] equipSlotImages;
    ///<summary> 장착 중인 장비 그리드 표시 </summary>
    [SerializeField] Image[] equipSlotGridImages;
    ///<summary> 장착 중인 장비 성급 </summary>
    [SerializeField] GameObject[] stars;
    #endregion PlayerInfo

    #region EquipList
    ///<summary> 장비 리스트 버튼들 부모 오브젝트 </summary>
    [Header("Equip List")]
    [SerializeField] RectTransform equipBtnParent;
    ///<summary> 풀에 있는 버튼들 부모 오브젝트 </summary>
    public RectTransform poolParent;
    ///<summary> 장비 리스트 버튼 프리팹 </summary>
    public EquipBtnToken equipBtnPrefab;
    ///<summary> 현재 활성화된 장비 리스트 버튼들 </summary>
    List<EquipBtnToken> btnList = new List<EquipBtnToken>();
    ///<summary> 장비 리스트 버튼 풀 </summary>
    [HideInInspector] public Queue<EquipBtnToken> equipBtnPool = new Queue<EquipBtnToken>();
    #endregion EquipList

    #region SubPanels
    ///<summary> 카테고리 선택 UI Set </summary>
    [Header("Sub Panels")]
    [SerializeField] SmithCategoryPanel categoryPanel;
    ///<summary> 0 선택, 1 융합, 2 옵션 변경, 3 분해, 4 제작, 5 스킬북 </summary>
    [Tooltip("0 선택, 1 융합, 2 옵션 변경, 3 분해, 4 제작, 5 스킬북")]
    [SerializeField] GameObject[] workPanelObjects;
    ITownPanel[] workPanels = null;
    #endregion SubPanels

    #region SelectInfo
    ///<summary> 현재 선택한 장비 정보 보여주는 UI Set </summary>
    [Header("Select Info")] 
    [SerializeField] EquipInfoPanel selectedEquipPanel;
    ///<summary> 현재 선택한 장비 정보(리스트에서의 인덱스, 장비 페어) </summary>
    KeyValuePair<int, Equipment> selectedEquip;
    ///<summary> 현재 선택한 장비 정보(리스트에서의 인덱스, 장비 페어) </summary>
    public KeyValuePair<int, Equipment> SelectedEquip { get => selectedEquip; }
    ///<summary> 선택한 장비 없을 시 상태 </summary>
    static KeyValuePair<int, Equipment> dummyEquip = new KeyValuePair<int, Equipment>(-1, null);

    ///<summary> 선택한 제작법 정보 </summary>
    EquipBluePrint selectedEBP = null;
    ///<summary> 선택한 제작법 정보 </summary>
    public EquipBluePrint SelectedEBP { get => selectedEBP; }
    ///<summary> 선택한 스킬북 정보(리스트에서의 인덱스, 스킬북 페어) </summary>
    KeyValuePair<int, Skillbook> selectedSkillbook = dummySkillbook;
    ///<summary> 선택한 스킬북 정보(리스트에서의 인덱스, 스킬북 페어) </summary>
    public KeyValuePair<int, Skillbook> SelectedSkillbook { get => selectedSkillbook; }
    ///<summary> 선택한 스킬북 없을 시 상태 </summary>
    static KeyValuePair<int, Skillbook> dummySkillbook = new KeyValuePair<int, Skillbook>(-1, null);
    #endregion SelectInfo  
    #endregion Variables

    #region ResetInfo
    public void ResetAllState()
    {
        if(workPanels == null)
        {
            workPanels = new ITownPanel[workPanelObjects.Length];
            for(int i = 0;i < workPanels.Length;i++) workPanels[i] = workPanelObjects[i].GetComponent<ITownPanel>();
        }
        //스텟 업데이트
        StatTxtUpdate();
        //장착 장비 정보 업데이트
        EquipIconUpdate();

        //카테고리 초기화
        categoryPanel.ResetAllState();
    }
    ///<summary> 선택한 장비, 제작법, 스킬북 정보 초기화 </summary>
    public void ResetSelectInfo()
    {
        //장비 선택 상태 초기화
        selectedEquip = dummyEquip;
        selectedEBP = null;
        selectedSkillbook = dummySkillbook;
        TokenBtnUpdate();
        SelectedPanelUpdate();
    }
    #endregion ResetInfo

    #region LoadPlayerInfo
    ///<summary> 플레이어 스텟 표시 텍스트 업데이트 </summary>
    void StatTxtUpdate()
    {
        classTxt.text = GameManager.instance.slotData.className;

        int lvl = GameManager.instance.slotData.lvl;
        statTxts[0].text = $"{lvl}";
        if(lvl <= 9)
        {
            statTxts[1].text = $"{GameManager.instance.slotData.exp} / {GameManager.reqExp[GameManager.instance.slotData.lvl]}";
            expSlider.value = GameManager.instance.slotData.exp / (float)GameManager.reqExp[GameManager.instance.slotData.lvl];
        }
        else
        {
            statTxts[1].text = "최대";
            expSlider.value = 1;
        }
        
        int i, j;
        for (i = j = 2; i < 13; i++, j++)
        {
            if (i == 3) i++;
            statTxts[j].text = GameManager.instance.slotData.itemStats[i].ToString();
        }

        statTxts[8].text = $"{statTxts[8].text}%";
        statTxts[9].text = $"{statTxts[9].text}%";
    }
    ///<summary> 장착 중인 장비 정보 이미지 업데이트 </summary>
    void EquipIconUpdate()
    {
        for (int i = 0; i < 7; i++)
        {
            if(GameManager.instance.slotData.itemData.equipmentSlots[i + 1] != null)
            {
                equipSlotImages[i].sprite = SpriteGetter.instance.GetEquipIcon(GameManager.instance.slotData.itemData.equipmentSlots[i + 1].ebp);
                equipSlotGridImages[i].sprite = SpriteGetter.instance.GetGrid(GameManager.instance.slotData.itemData.equipmentSlots[i + 1].ebp.rarity);
            
                equipSlotImages[i].transform.parent.gameObject.SetActive(true);
                for(int j = 0;j < 3;j++)
                    stars[i * 3 + j].SetActive(j < GameManager.instance.slotData.itemData.equipmentSlots[i + 1].star);
            }
            else
            {
                equipSlotImages[i].transform.parent.gameObject.SetActive(false);
                for(int j = 0;j < 3;j++)
                    stars[i * 3 + j].SetActive(false);
            }
        }

        equipSlotImages[7].sprite = SpriteGetter.instance.GetPotionIcon(GameManager.instance.slotData.potionSlot[0]);
        equipSlotImages[7].transform.parent.gameObject.SetActive(GameManager.instance.slotData.potionSlot[0] > 0);
        equipSlotImages[8].sprite = SpriteGetter.instance.GetPotionIcon(GameManager.instance.slotData.potionSlot[1]);
        equipSlotImages[8].transform.parent.gameObject.SetActive(GameManager.instance.slotData.potionSlot[1] > 0);
    }
    #endregion LoadPlayerInfo

    #region Btn Image Update
    ///<summary> 스크롤뷰에서 나오는 버튼들 정보 업데이트 </summary>
    public void TokenBtnUpdate()
    {
        TokenBtnReset();
        if (categoryPanel.CurrCategory <= SmithCategory.Accessory)
            BtnUpdate_Equip();
        else if (categoryPanel.CurrCategory <= SmithCategory.AccessoryRecipe)
            BtnUpdate_Recipe();
        else
            BtnUpdate_Skillbook();
        

        void BtnUpdate_Equip()
        {
            //카테고리에 맞는 장비만 얻기
            List<KeyValuePair<int, Equipment>> categorizedEquips = ItemManager.GetEquipData(categoryPanel.CurrCategory, categoryPanel.CurrRarity, categoryPanel.CurrLvl);

            for (int i = 0; i < categorizedEquips.Count; i += 4)
            {
                //풀에서 버튼 토큰 가져오기
                EquipBtnToken token = GameManager.GetToken(equipBtnPool, equipBtnParent, equipBtnPrefab);
                token.Initialize(this, i, categorizedEquips);
                btnList.Add(token);
                token.gameObject.SetActive(true);
            }

            //버튼 토큰이 5개보다 모자를 시, 빈 토큰으로 채움(리스트 보여주기 용)
            for(int i = btnList.Count;i < 5;i++)
            {
                EquipBtnToken token = GameManager.GetToken(equipBtnPool, equipBtnParent, equipBtnPrefab);
                token.Initialize(this, 0, (List<KeyValuePair<int, Equipment>>)null);
                btnList.Add(token);
                token.gameObject.SetActive(true);
            }
        }
        void BtnUpdate_Recipe()
        {
            //현재 카테고리에 맞는 제작법 리스트
            List<KeyValuePair<int, EquipBluePrint>> categorizedRecipes = ItemManager.GetRecipeData(categoryPanel.CurrCategory, categoryPanel.CurrRarity, categoryPanel.CurrLvl);

            for (int i = 0; i < categorizedRecipes.Count; i += 4)
            {
                EquipBtnToken token = GameManager.GetToken(equipBtnPool, equipBtnParent, equipBtnPrefab);
                token.Initialize(this, i, categorizedRecipes);
                btnList.Add(token);
                token.gameObject.SetActive(true);
            }
            
            //5개보다 모자를 시, 빈 토큰으로 채움
            for(int i = btnList.Count;i < 5;i++)
            {
                EquipBtnToken token = GameManager.GetToken(equipBtnPool, equipBtnParent, equipBtnPrefab);
                token.Initialize(this, 0, (List<KeyValuePair<int, EquipBluePrint>>)null);
                btnList.Add(token);
                token.gameObject.SetActive(true);
            }
        }
        void BtnUpdate_Skillbook()
        {
            //현재 카테고리에 맞는 스킬북 반환
            List<KeyValuePair<int, Skillbook>> categorizedSkillbooks = ItemManager.GetSkillbookData(categoryPanel.CurrUseType, categoryPanel.CurrLvl);

            for (int i = 0; i < categorizedSkillbooks.Count; i += 4)
            {
                EquipBtnToken token = GameManager.GetToken(equipBtnPool, equipBtnParent, equipBtnPrefab);
                token.Initialize(this, i, categorizedSkillbooks);
                btnList.Add(token);
                token.gameObject.SetActive(true);
            }

            //버튼 토큰이 5개보다 모자를 시, 빈 토큰으로 채움
            for (int i = btnList.Count; i < 5; i++)
            {
                EquipBtnToken token = GameManager.GetToken(equipBtnPool, equipBtnParent, equipBtnPrefab);
                token.Initialize(this, 0, (List<KeyValuePair<int, Skillbook>>)null);
                btnList.Add(token);
                token.gameObject.SetActive(true);
            }
        }
    }
    ///<summary> 스크롤뷰에 있는 버튼들 초기화 </summary>
    void TokenBtnReset()
    {
        for(int i = 0; i < btnList.Count; i++)
        {
            btnList[i].gameObject.SetActive(false);
            btnList[i].transform.SetParent(poolParent);
            equipBtnPool.Enqueue(btnList[i]);
        }
        btnList.Clear();
    }
    #endregion

    ///<summary> 추가 선택 창 보이기 </summary>
    ///<param name="workPanelIdx"> 0 선택, 1 융합, 2 옵션 변경, 3 분해, 4 제작, 5 스킬북 </param>
    public void Btn_OpenWorkPanel(int workPanelIdx)
    {
        if(workPanelIdx >= 0)
            workPanels[workPanelIdx].ResetAllState();

        for (int i = 0; i < workPanels.Length; i++)
            workPanelObjects[i].SetActive(i == workPanelIdx);
    }
    public void OnEquipReroll()
    {
        if(selectedEquip.Value != null)
            selectedEquipPanel.InfoUpdate(selectedEquip.Value);
    }
    public void OnDisassemble()
    {
        selectedEquip = dummyEquip;
        TokenBtnUpdate();
        SelectedPanelUpdate();
    }
    
    ///<summary> 토큰 버튼 장비 선택 </summary>
    public void Btn_EquipToken(KeyValuePair<int, Equipment> token)
    {
        if (selectedEquip.Equals(token))
            selectedEquip = dummyEquip;
        else
            selectedEquip = token;

        SelectedPanelUpdate();
    }
    ///<summary> 토큰 버튼 제작법 선택 </summary>
    public void Btn_RecipeToken(EquipBluePrint token)
    {
        if (selectedEBP == token)
            selectedEBP = null;
        else
            selectedEBP = token;

        SelectedPanelUpdate();
    }
    ///<summary> 토큰 버튼 스킬북 선택 </summary>
    public void Btn_SkillbookToken(KeyValuePair<int, Skillbook> token)
    {
        if(selectedSkillbook.Equals(token))
            selectedSkillbook = dummySkillbook;
        else
            selectedSkillbook = token;

        SelectedPanelUpdate();
    }

    void SelectedPanelUpdate()
    {
        if (categoryPanel.CurrCategory <= SmithCategory.Accessory && !selectedEquip.Equals(dummyEquip))
        {
            selectedEquipPanel.InfoUpdate(selectedEquip.Value);
            selectedEquipPanel.transform.parent.gameObject.SetActive(true);
            selectedEquipPanel.gameObject.SetActive(true);
            Btn_OpenWorkPanel(0);
        }
        else if (categoryPanel.CurrCategory <= SmithCategory.AccessoryRecipe && selectedEBP != null)
        {
            selectedEquipPanel.InfoUpdate(selectedEBP);
            selectedEquipPanel.transform.parent.gameObject.SetActive(true);
            selectedEquipPanel.gameObject.SetActive(true);
            Btn_OpenWorkPanel(4);
        }
        else if (categoryPanel.CurrCategory <= SmithCategory.Skillbook && selectedSkillbook.Key >= 0)
        {
            selectedEquipPanel.InfoUpdate(selectedSkillbook.Value);
            selectedEquipPanel.transform.parent.gameObject.SetActive(true);
            selectedEquipPanel.gameObject.SetActive(true);
            Btn_OpenWorkPanel(5);
        }
        else if(categoryPanel.CurrCategory == SmithCategory.Resource)
        {
            selectedEquipPanel.transform.parent.gameObject.SetActive(false);
            Btn_OpenWorkPanel(-1);
        }
        else
        {
            selectedEquipPanel.transform.parent.gameObject.SetActive(true);
            selectedEquipPanel.gameObject.SetActive(false);
            Btn_OpenWorkPanel(-1);
        }
    }
    public void BedToSmith(SmithCategory currC, Rarity currR, int currL, KeyValuePair<int, Equipment> selected)
    {
        categoryPanel.Btn_SwitchCategory((int)currC);
        categoryPanel.Btn_SwitchRarity((int)currR);
        categoryPanel.Btn_SwitchLvl(currL);
        selectedEquip = selected;
        TokenBtnUpdate();
        SelectedPanelUpdate();
        Btn_OpenWorkPanel(0);
    }
    public void BedToSkillLearn(int skillIdx)
    {
        categoryPanel.Btn_SwitchCategory((int)SmithCategory.Skillbook);
        Btn_SkillbookToken(ItemManager.GetSkillbookData(-1, 0).FindAll(x => x.Value.idx == skillIdx).First());
    }
}
