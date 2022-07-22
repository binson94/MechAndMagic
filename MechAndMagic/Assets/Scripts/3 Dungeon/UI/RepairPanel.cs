using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RepairPanel : MonoBehaviour 
{
    
    ///<summary> 클래스 이름 텍스트 </summary>
    [Header("Stat")]
    [SerializeField] Text nameTxt;
    ///<summary> 스텟 표시 텍스트
    ///<para> 0 lvl, 1 exp  req, 2 ~ 11 stat </para> </summary>
    [Tooltip(" 0 lvl, 1 exp / req, 2 ~ 11 stat")]
    [SerializeField] Text[] statTxts;
    ///<summary> 경험치 표시 슬라이더 </summary>
    [SerializeField] Slider expSlider;


    ///<summary> 장비 정보 표시 UI Set </summary>
    [Header("Equip")]
    [SerializeField] EquipInfoPanel equipPanel;
    ///<summary> 포션 정보 표시 UI Set </summary>
    [SerializeField] PotionInfoPanel potionPanel;
    ///<summary> 장비 장착칸 아이콘 이미지들 <para> 0 wep, 1-4 armor, 5-6 accessory, 7-8 potion </para> </summary>
    [SerializeField] Image[] equipSlotImages;
    ///<summary> 장비 장착칸 그리드 이미지들 <para> 0 wep, 1-4 armor, 5-6 accessory, 7-8 potion </para> </summary>
    [SerializeField] Image[] equipSlotGridImages;
    ///<summary> 장비 세트 아이콘 이미지들 <para> 0 wep, 1-4 armor, 5-6 accessory </para> </summary>
    [SerializeField] Image[] equipSetImages;
    [SerializeField] GameObject[] stars;
    ///<summary> 세트 옵션 이름 텍스트들 </summary>
    [SerializeField] Text[] setNameTxts;
    ///<summary> 세트 옵션 설명 텍스트들 </summary>
    [SerializeField] Text[] setScriptTxts;
    ///<summary> 세트 옵션 설명 텍스트 색상
    ///<para> 0 발동 이름, 1 발동 설명, 2 미발동 이름, 3 미발동 설명 </para> </summary>
    Color[] setColors = new Color[] { new Color(1, 1, 1, 1), new Color(0xd3 / 255f, 0xd3 / 255f, 0xd3 / 255f, 1), new Color(0x77 / 255, 0x77 / 255f, 0x77 / 255f, 1), new Color(0x5b / 255f, 0x5a / 255f, 0x5a / 255f, 1) };

    [Header("Skill")]
    ///<summary> 스킬 정보 표시 텍스트들
    ///<para> 0 script, 1 pos, 2 neg </para> </summary>
    [SerializeField] Text[] skillScriptTxts;
    ///<summary> 현재 장착 중인 스킬 정보 표시 </summary>
    [SerializeField] SkillBtnToken[] skillBtns;

    [Header("Drop")]
    [SerializeField] PopUpManager pm;
    [SerializeField] DropToken dropTokenPrefab;
    [SerializeField] RectTransform tokenParent;
    List<DropToken> tokenList = new List<DropToken>();
    Queue<DropToken> tokenPool = new Queue<DropToken>();
    [SerializeField] RectTransform poolParent;


    private void Start() {
        nameTxt.text = GameManager.instance.slotData.className;
        LoadEquipInfo();
        LoadSkillInfo();
    }
    public void ResetAllState()
    {
        foreach(Text t in skillScriptTxts) t.text = string.Empty;
        equipPanel.InfoUpdate(null as Equipment);
        potionPanel.InfoUpdate(0);
        
        LoadStatInfo();
        LoadDropInfo();
    }

    ///<summary> 장착 중인 장비 세부 정보 보이기 </summary>
    public void Btn_Equip(int part)
    {
        potionPanel.InfoUpdate(0);
        equipPanel.InfoUpdate(GameManager.instance.slotData.itemData.equipmentSlots[part]);
    }
    public void Btn_Potion(int slotIdx)
    {
        equipPanel.InfoUpdate(null as Equipment);
        potionPanel.InfoUpdate(GameManager.instance.slotData.potionSlot[slotIdx], GameManager.instance.slotData.dungeonData.potionUse[slotIdx]);
    }
    ///<summary> 장착 중인 스킬 세부 정보 보이기 </summary>
    public void Btn_Skill(int skillSlotIdx)
    {
        Skill s = SkillManager.GetSlotSkill(skillSlotIdx);
        skillScriptTxts[0].text = s.script;
        skillScriptTxts[1].text = s.posScript;
        skillScriptTxts[2].text = s.negScript;
    }

    ///<summary> 아이템 장착 정보 불러오기 </summary>
    void LoadEquipInfo()
    {
        for (int i = 0; i < 7; i++)
        {
            if(GameManager.instance.slotData.itemData.equipmentSlots[i + 1] != null)
            {
                equipSlotImages[i].sprite = SpriteGetter.instance.GetEquipIcon(GameManager.instance.slotData.itemData.equipmentSlots[i + 1]?.ebp);
                equipSlotGridImages[i].sprite = SpriteGetter.instance.GetGrid(GameManager.instance.slotData.itemData.equipmentSlots[i + 1].ebp.rarity);
                equipSetImages[i].sprite = SpriteGetter.instance.GetSetIcon(GameManager.instance.slotData.itemData.equipmentSlots[i + 1].ebp.set);
                equipSetImages[i].gameObject.SetActive(GameManager.instance.slotData.itemData.equipmentSlots[i + 1].ebp.set > 0);

                equipSlotImages[i].transform.parent.gameObject.SetActive(true);
                for(int j = 0;j < 3;j++)
                    stars[i * 3 + j].SetActive(j < GameManager.instance.slotData.itemData.equipmentSlots[i + 1].star);
            }
            else
            {
                equipSlotImages[i].transform.parent.gameObject.SetActive(false);
                equipSetImages[i].gameObject.SetActive(false);
                for(int j = 0;j < 3;j++)
                    stars[i * 3 + j].SetActive(false);
            }
        }

        equipSlotImages[7].sprite = SpriteGetter.instance.GetPotionIcon(GameManager.instance.slotData.potionSlot[0]);
        equipSlotImages[7].transform.parent.gameObject.SetActive(GameManager.instance.slotData.potionSlot[0] > 0);
        equipSlotImages[8].sprite = SpriteGetter.instance.GetPotionIcon(GameManager.instance.slotData.potionSlot[1]);
        equipSlotImages[8].transform.parent.gameObject.SetActive(GameManager.instance.slotData.potionSlot[1] > 0);

        LoadSetInfo();

        void LoadSetInfo()
        {
            foreach (Text text in setNameTxts) text.text = string.Empty;
            foreach (Text text in setScriptTxts) text.text = string.Empty;

            List<Pair<string, string>> currSetInfos = ItemManager.GetSetInfo();

            for (int i = 0; i < 3 && i < currSetInfos.Count; i++)
            {
                setNameTxts[i].text = currSetInfos[i].Key;
                setNameTxts[i].color = setColors[0];
                setScriptTxts[i].text = currSetInfos[i].Value;
                setScriptTxts[i].color = setColors[1];
            }
        }
    }
    ///<summary> 스킬 장착 정보 불러오기 </summary>
    void LoadSkillInfo()
    {
        for (int i = 0; i < skillBtns.Length; i++)
            skillBtns[i].Init(i);
    }
    ///<summary> 스텟 정보 불러오기 </summary>
    void LoadStatInfo()
    {
        int lvl = GameManager.instance.slotData.lvl;
        statTxts[0].text = $"{lvl}";

        if (lvl <= 9)
        {
            statTxts[1].text = $"{GameManager.instance.slotData.exp} / {GameManager.reqExp[lvl]}";
            expSlider.value = GameManager.instance.slotData.exp / (float)GameManager.reqExp[lvl];
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
    ///<summary> 드랍된 아이템 정보 불러오기 </summary>
    void LoadDropInfo()
    {
        ResetDropInfo();
        List<Triplet<DropType, int, int>> drops = GameManager.instance.slotData.dungeonData.dropList;

        DropToken token;
        List<Triplet<DropType, int, int>> idxs = new List<Triplet<DropType, int, int>>();

        for(int i = 0;i < drops.Count;i++)
        {
            token = GameManager.GetToken(tokenPool, tokenParent, dropTokenPrefab);

            for(int j = 0;j < 3 && i < drops.Count;i++, j++)
                idxs.Add(drops[i]);

            token.Initialize(pm, idxs);
            tokenList.Add(token);
            token.gameObject.SetActive(true);
            idxs.Clear();
        }

        void ResetDropInfo()
        {
            for(int i = 0;i < tokenList.Count;i++)
            {
                tokenList[i].gameObject.SetActive(false);
                tokenList[i].transform.SetParent(poolParent);
                tokenPool.Enqueue(tokenList[i]);
            }

            tokenList.Clear();
        }
    }
}