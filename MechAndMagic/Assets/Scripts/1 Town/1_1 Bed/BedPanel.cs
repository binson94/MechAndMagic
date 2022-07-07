using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class BedPanel : MonoBehaviour, ITownPanel
{
    ///<summary> 숙소 -> 대장간 중개를 위해 </summary>
    [SerializeField] TownManager TM;

    ///<summary> 0 : main, 1 : item, 2 : skill </summary>
    [Header("Panels")] [Tooltip("0 main, 1 item, 2 skill")]
    [SerializeField] GameObject[] subPanels;
    ///<summary> 0 : main, 1 : item, 2 : skill </summary>
    ITownPanel[] panels = null;
    ///<summary> 캐릭터 정보 표시 UI Set, 스킬에서만 비활성화 </summary>
    [SerializeField] GameObject mainItemPanel;
    ///<summary> 0 main, 1 item, 2 skill </summary>
    int currPanel = 0;

    ///<summary> 클래스 이름 </summary>
    [Header("Main-Item Panel")]
    [SerializeField] Text classTxt;
    ///<summary> 0 lvl, 1 exp, 2 hp, 3 ap, 4 atk, 5 def, 6 acc, 7 dog, 8 crc, 9 crb, 10 pen, 11 spd </summary>
    [SerializeField] Text[] statTxts;
    [SerializeField] Text[] statDelta;
    ///<summary> 경험치 슬라이더 </summary>
    [SerializeField] Slider expSlider;
    ///<summary> 장비 장착칸 아이콘 이미지들 <para> 0 wep, 1-4 armor, 5-6 accessory, 7-8 potion </para> </summary>
    [SerializeField] Image[] equipSlotImages;
    ///<summary> 세트 정보 표시 텍스트 </summary>
    [SerializeField] Text setTxt;

    public void ResetAllState()
    {
        if(panels == null)
        {
            panels = new ITownPanel[subPanels.Length];
            for(int i = 0;i < panels.Length;i++)
                panels[i] = subPanels[i].GetComponent<ITownPanel>();
        }
        currPanel = 0;
        StatTxtUpdate();
        SetTxtUpdate();
        PanelShow();
    }

    public void Btn_PanelSelect(int idx)
    {
        currPanel = idx;
        PanelShow();
    }
    void PanelShow()
    {
        if(currPanel <= 1)
            EquipIconUpdate();
        mainItemPanel.SetActive(currPanel <= 1);
        
        for (int i = 0; i < subPanels.Length; i++)
            if (i == currPanel)
            {
                panels[i].ResetAllState();
                subPanels[i].SetActive(true);
            }
            else
                subPanels[i].SetActive(false);
    }

    public void StatTxtUpdate()
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

        foreach(Text t in statDelta)
            t.text = string.Empty;
    }
    public void SetTxtUpdate()
    {
        setTxt.text = string.Empty;

        List<KeyValuePair<string, int>> setList = ItemManager.GetSetList();

        foreach(KeyValuePair<string, int> token in setList)
            setTxt.text += $"{token.Key} {token.Value}세트\n";
    }

    public void EquipIconUpdate()
    {
        equipSlotImages[7].sprite = SpriteGetter.instance.GetPotionIcon(GameManager.instance.slotData.potionSlot[0]);
        equipSlotImages[7].gameObject.SetActive(GameManager.instance.slotData.potionSlot[0] > 0);
        equipSlotImages[8].sprite = SpriteGetter.instance.GetPotionIcon(GameManager.instance.slotData.potionSlot[1]);
        equipSlotImages[8].gameObject.SetActive(GameManager.instance.slotData.potionSlot[1] > 0);
    }

    public void BedToSmith(ItemCategory currC, Rarity currR, int currL, KeyValuePair<int, Equipment> selected) => TM.BedToSmith(currC, currR, currL, selected);
}
