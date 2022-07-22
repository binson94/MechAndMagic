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
    ///<summary> 장비 장착칸 그리드 이미지들 <para> 0 wep, 1-4 armor, 5-6 accessory, 7-8 potion </para> </summary>
    [SerializeField] Image[] equipSlotGridImages;
    ///<summary> 장비 세트 아이콘 이미지들 <para> 0 wep, 1-4 armor, 5-6 accessory </para> </summary>
    [SerializeField] Image[] equipSetImages;
    [SerializeField] GameObject[] stars;
    ///<summary> 세트 정보 표시 텍스트 </summary>
    [SerializeField] Text[] setNameTxts;
    ///<summary> 세트 옵션 설명 표시 텍스트들 </summary>
    [SerializeField] Text[] setScriptTxts;
    ///<summary> 세트 옵션 설명 텍스트 색상
    ///<para> 0 발동 이름, 1 발동 설명, 2 미발동 이름, 3 미발동 설명 </para> </summary>
    Color[] setColors = new Color[] { new Color(1, 1, 1, 1), new Color(0xd3 / 255f, 0xd3 / 255f, 0xd3 / 255f, 1), new Color(0x77 / 255, 0x77 / 255f, 0x77 / 255f, 1), new Color(0x5b / 255f, 0x5a / 255f, 0x5a / 255f, 1) };

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

        foreach(Text t in statDelta)
            t.text = string.Empty;
    }
    ///<summary> 세트 옵션 정보 업데이트 </summary>
    public void SetTxtUpdate()
    {
        foreach(Text text in setNameTxts) text.text = string.Empty;
        foreach (Text text in setScriptTxts) text.text = string.Empty;

        List<Pair<string, string>> currSetInfos = ItemManager.GetSetInfo();

        for(int i = 0;i < 3 && i < currSetInfos.Count;i++)
        {
            setNameTxts[i].text = currSetInfos[i].Key;
            setNameTxts[i].color = setColors[0];
            setScriptTxts[i].text = currSetInfos[i].Value;
            setScriptTxts[i].color = setColors[1];
        }
    }
    public void SetTxtUpdate(int setIdx)
    {
        foreach(Text text in setNameTxts) text.text = string.Empty;
        foreach (Text text in setScriptTxts) text.text = string.Empty;

        Pair<string, List<Triplet<bool, int ,string>>> currSetInfos = ItemManager.GetSetInfo(setIdx);

        for(int i = 0;i < 3 && i < currSetInfos.Value.Count;i++)
        {
            setNameTxts[i].text = $"{currSetInfos.Key} {currSetInfos.Value[i].second}세트";
            setNameTxts[i].color = currSetInfos.Value[i].first ? setColors[0] : setColors[2];
            setScriptTxts[i].text = currSetInfos.Value[i].third;
            setScriptTxts[i].color = currSetInfos.Value[i].first ? setColors[1] : setColors[3];
        }
    }

    public void EquipIconUpdate()
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
    }

    public void BedToSmith(SmithCategory currC, Rarity currR, int currL, KeyValuePair<int, Equipment> selected) => TM.BedToSmith(currC, currR, currL, selected);
}
