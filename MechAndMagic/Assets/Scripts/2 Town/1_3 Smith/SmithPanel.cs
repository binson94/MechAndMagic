using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class SmithPanel : MonoBehaviour, ITownPanel
{
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

    [Header("Equip List")]
    [SerializeField] RectTransform equipBtnParent;
    [SerializeField] RectTransform poolParent;
    [SerializeField] EquipBtnToken equipBtnPrefab;
    List<EquipBtnToken> btnList = new List<EquipBtnToken>();
    Queue<EquipBtnToken> equipBtnPool = new Queue<EquipBtnToken>();

    #region Category
    ///<summary> 0 : category, 1 : rarity, 2 : level, 3 : skillType </summary>
    [Header("Category")] [Tooltip("0 : category, 1 : rarity, 2 : level, 3 : skillType")]
    [SerializeField] GameObject[] categorySelectPanels;
    ///<summary> 0 : rarity Btn, 1 : skillType Btn </summary>
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
    ///<summary> 현재 선택한 장비 정보 보여주는 UI Set </summary>
    [SerializeField] EquipInfoPanel selectedEquipPanel;
    ///<summary> -1 닫기, 0 선택, 1 융합, 2 옵션 변경, 3 분해, 4 제작, 5 스킬북 </summary>
    int currWorkPanel;
    ///<summary> 0 선택, 1 융합, 2 옵션 변경, 3 분해, 4 제작, 5 스킬북 </summary>
    [Tooltip("0 선택, 1 융합, 2 옵션 변경, 3 분해, 4 제작, 5 스킬북")]
    [SerializeField] GameObject[] workPanels;


    ///<summary> 현재 선택한 장비 정보(리스트에서의 인덱스, 장비 페어) </summary>
    KeyValuePair<int, Equipment> selectedEquip;
    ///<summary> 선택한 장비 없을 시 상태
    static KeyValuePair<int, Equipment> dummyEquip = new KeyValuePair<int, Equipment>(-1, null);

    EquipBluePrint selectedEBP = null;
    Skillbook selectedSkillbook = null;
    #endregion Work Panel

    private void Start() {
        Debug_Drop();

        void Debug_Drop(){
        ItemManager.ItemDrop(84, 1);
        ItemManager.ItemDrop(85, 1);
        ItemManager.ItemDrop(86, 1);
        ItemManager.ItemDrop(87, 1);
        ItemManager.ItemDrop(23, 1);}
    }
    
    public void ResetAllState()
    {
        //스텟 업데이트
        StatTxtUpdate();
        //장착 장비 정보 업데이트
        EquipIconUpdate();

        //카테고리 초기화
        currRarity = Rarity.None;
        currUseType = -1;
        currLvl = 0;
        Btn_SwitchCategory((int)ItemCategory.Weapon);
        Btn_OpenCategorySelectPanel(-1);

        //장비 선택 상태 초기화
        selectedEquip = dummyEquip;
        selectedEBP = null;
        selectedSkillbook = null;
        SelectedPanelUpdate();
    }
    ///<summary> 플레이어 스텟 표시 텍스트 업데이트 </summary>
    void StatTxtUpdate()
    {
        classTxt.text = GameManager.instance.slotData.className;

        statTxts[0].text = GameManager.instance.slotData.lvl.ToString();
        statTxts[1].text = $"{GameManager.instance.slotData.exp} / {GameManager.reqExp[GameManager.instance.slotData.lvl]}";
        expSlider.value = GameManager.instance.slotData.exp / (float)GameManager.reqExp[GameManager.instance.slotData.lvl];
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
                equipSlotImages[i].sprite = SpriteGetter.instance.GetEquipIcon(GameManager.instance.slotData.itemData.equipmentSlots[i + 1]);
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
        equipSlotImages[7].gameObject.SetActive(GameManager.instance.slotData.potionSlot[0] > 0);
        equipSlotImages[8].sprite = SpriteGetter.instance.GetPotionIcon(GameManager.instance.slotData.potionSlot[1]);
        equipSlotImages[8].gameObject.SetActive(GameManager.instance.slotData.potionSlot[1] > 0);
    }

    #region Category
    ///<summary> 카테고리, 등급, 레벨, 스킬 타입 버튼 선택 시 세부 선택 판넬 보이기 </summary>
    ///<param name="panelIdx"> 0 category, 1 rarity, 2 lvl, 3 skillType </param>
    public void Btn_OpenCategorySelectPanel(int panelIdx)
    {
        for(int i = 0;i < categorySelectPanels.Length;i++)
            categorySelectPanels[i].SetActive(i == panelIdx);

        //선택 정보 초기화 
        selectedEquip = dummyEquip;
        selectedEBP = null;
        selectedSkillbook = null;
        SelectedPanelUpdate();
        Btn_OpenWorkPanel(-1);
    }
    ///<summary> 카테고리 세부 선택 판넬에서 카테고리 변경 </summary>
    ///<param name="category"> 1 무기, 2 방어구, 3 장신구, 4 제작법, 5 스킬북 </param>
    public void Btn_SwitchCategory(int category)
    {
        //카테고리 변경
        currCategory = (ItemCategory)category;
        //스킬북이면 스킬 타입 선택 버튼으로 변경
        for (int i = 0; i < categoryBtns.Length; i++)
            categoryBtns[i].SetActive(i == 0 ^ currCategory == ItemCategory.Skillbook);
        TokenBtnUpdate();

        //선택 정보 초기화
        selectedEquip = dummyEquip;
        selectedEBP = null;
        selectedSkillbook = null;
        SelectedPanelUpdate();
        Btn_OpenCategorySelectPanel(-1);
    }
    ///<summary> 등급 세부 선택 판넬에서 등급 변경 </summary>
    ///<param name="rarity"> 1 일반, 2 고급, 3 희귀, 3 고유, 4 전설 </param>
    public void Btn_SwitchRarity(int rarity)
    {
        currRarity = (Rarity)rarity;
        TokenBtnUpdate();
        Btn_OpenCategorySelectPanel(-1);
    }
    ///<summary> 레벨 세부 선택 판넬에서 레벨 변경 </summary>
    ///<param name="lvl"> 0 전체, 1, 3, 5, 7, 9 </param>
    public void Btn_SwitchLvl(int lvl)
    {
        currLvl = lvl;
        TokenBtnUpdate();
        Btn_OpenCategorySelectPanel(-1);
    }
    ///<summary> 스킬 타입 세부 선택 판넬에서 스킬 타입 변경 </summary>
    ///<param name="type"> 0 액티브, 1 패시브 </param>
    public void Btn_SwitchSkillUseType(int type)
    {
        currUseType = type;
        TokenBtnUpdate();
        Btn_OpenCategorySelectPanel(-1);
    }
    #endregion
    
    #region Btn Image Update
    ///<summary> 스크롤뷰에서 나오는 버튼들 정보 업데이트 </summary>
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
            //카테고리에 맞는 장비만 얻기
            List<Equipment> categorizedEquips = ItemManager.GetEquipData(currCategory, currRarity, currLvl);

            //버튼 인덱스(0 시작), 장비 정보 페어
            //0개에서 4개
            List<KeyValuePair<int, Equipment>> buttonInfos = new List<KeyValuePair<int, Equipment>>();

            //
            for (int i = 0; i < categorizedEquips.Count;)
            {
                while (buttonInfos.Count < 4 && i < categorizedEquips.Count)
                {
                    buttonInfos.Add(new KeyValuePair<int, Equipment>(i, categorizedEquips[i]));
                    i++;
                }
                //풀에서 버튼 토큰 가져오기
                EquipBtnToken token = GameManager.GetToken(equipBtnPool, equipBtnParent, equipBtnPrefab);
                token.Init(this, buttonInfos);
                btnList.Add(token);
                token.gameObject.SetActive(true);

                buttonInfos.Clear();
            }

            //버튼 토큰이 5개보다 모자를 시, 빈 토큰으로 채움(리스트 보여주기 용)
            for(int i = btnList.Count;i < 5;i++)
            {
                EquipBtnToken token = GameManager.GetToken(equipBtnPool, equipBtnParent, equipBtnPrefab);
                token.Init(this, buttonInfos);
                btnList.Add(token);
                token.gameObject.SetActive(true);
            }
        }
        void BtnUpdate_Recipe()
        {
            //현재 카테고리에 맞는 제작법 리스트
            List<EquipBluePrint> categorizedRecipes = ItemManager.GetRecipeData(currRarity, currLvl);

            //버튼 인덱스, 레시피 페어
            //0개에서 4개
            List<KeyValuePair<int, EquipBluePrint>> buttonInfos = new List<KeyValuePair<int, EquipBluePrint>>();

            for (int i = 0; i < categorizedRecipes.Count;)
            {
                while (buttonInfos.Count < 4 && i < categorizedRecipes.Count)
                {
                    buttonInfos.Add(new KeyValuePair<int, EquipBluePrint>(i, categorizedRecipes[i]));
                    i++;
                }

                EquipBtnToken token = GameManager.GetToken(equipBtnPool, equipBtnParent, equipBtnPrefab);
                token.Init(this, buttonInfos);
                btnList.Add(token);
                token.gameObject.SetActive(true);

                buttonInfos.Clear();
            }
            
            //5개보다 모자를 시, 빈 토큰으로 채움
            for(int i = btnList.Count;i < 5;i++)
            {
                EquipBtnToken token = GameManager.GetToken(equipBtnPool, equipBtnParent, equipBtnPrefab);
                token.Init(this, buttonInfos);
                btnList.Add(token);
                token.gameObject.SetActive(true);
            }
        }
        void BtnUpdate_Skillbook()
        {
            //현재 카테고리에 맞는 스킬북 반환
            List<Skillbook> categorizedSkillbooks = ItemManager.GetSkillbookData(currUseType, currLvl);

            //버튼 인덱스, 스킬북 페어
            //0개 ~ 4개
            List<KeyValuePair<int, Skillbook>> buttonInfos = new List<KeyValuePair<int, Skillbook>>();
            for (int i = 0; i < categorizedSkillbooks.Count;)
            {
                while (buttonInfos.Count < 4 && i < categorizedSkillbooks.Count)
                {
                    buttonInfos.Add(new KeyValuePair<int, Skillbook>(i, categorizedSkillbooks[i]));
                    i++;
                }
                EquipBtnToken token = GameManager.GetToken(equipBtnPool, equipBtnParent, equipBtnPrefab);
                token.Init(this, buttonInfos);
                btnList.Add(token);
                token.gameObject.SetActive(true);

                buttonInfos.Clear();
            }
            
            //버튼 토큰이 5개보다 모자를 시, 빈 토큰으로 채움
            for(int i = btnList.Count;i < 5;i++)
            {
                EquipBtnToken token = GameManager.GetToken(equipBtnPool, equipBtnParent, equipBtnPrefab);
                token.Init(this, buttonInfos);
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

    #region SelectWorkPanel
    ///<summary> 추가 선택 창 보이기 </summary>
    ///<param name="workPanelIdx"> 0 선택, 1 융합, 2 옵션 변경, 3 분해, 4 제작, 5 스킬북 </param>
    public void Btn_OpenWorkPanel(int workPanelIdx)
    {
        if(workPanelIdx == 1 && (selectedEquip.Equals(dummyEquip) || selectedEquip.Value.star >= 3))
        {
            Debug.Log("융합 불가");
            return;
        }
        if(workPanelIdx == 2 && !GameManager.instance.slotData.itemData.CanFusion(selectedEquip.Value.ebp.part, selectedEquip.Key))
        {
            Debug.Log("이 장비는 옵션 변경이 불가능합니다.");
            return;
        }

        for (int i = 0; i < workPanels.Length; i++)
            workPanels[i].SetActive(i == workPanelIdx);
    }
    #endregion SelectWorkPanel

    #region Fusion
    ///<summary> 융합 버튼
    ///<para> 융합 재료 장비 선택창 띄움 </para> </summary>
    public void Btn_Fusion()
    {

    }
    #endregion Fusion

    #region SwitchOption
    ///<summary> 옵션 변경 버튼 </summary>
    public void Btn_SwitchOption()
    {
        if (GameManager.instance.slotData.itemData.CanSwitchCommonStat(selectedEquip.Value.ebp.part, selectedEquip.Key))
        {
            ItemManager.SwitchEquipOption(selectedEquip.Value.ebp.part, selectedEquip.Key);
            TokenBtnUpdate();
            SelectedPanelUpdate();
        }
    }
    #endregion SwitchOption

    
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
        {
            ItemManager.SmithEquipment(selectedEBP.idx);

            selectedEBP = null;
            SelectedPanelUpdate();
        }
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
    public void Btn_SkillbookToken(Skillbook token)
    {
        if (selectedSkillbook == token)
            selectedSkillbook = null;
        else
            selectedSkillbook = token;

        SelectedPanelUpdate();
    }
    public void Btn_FusionToken(KeyValuePair<int, Equipment> token)
    {

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
    public void BedToSkillLearn(int skillIdx)
    {
        Btn_SwitchCategory(5);
        Btn_SkillbookToken(ItemManager.GetSkillbookData(-1, 0).FindAll(x => x.idx == skillIdx).First());
    }
}
