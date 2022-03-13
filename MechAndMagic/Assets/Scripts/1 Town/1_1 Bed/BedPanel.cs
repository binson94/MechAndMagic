using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class BedPanel : MonoBehaviour, ITownPanel
{
    [SerializeField] TownManager TM;

    [Header("Panels")]
    //0 : main, 1 : item, 2 : skill
    [SerializeField] GameObject[] subPanels;
    ITownPanel[] panels = null;
    [SerializeField] GameObject mainItemPanel;
    int currPanel = 0;

    [Header("Main-Item Panel")]
    [SerializeField] Text[] statTxts;
    [SerializeField] Text[] statDelta;
    [SerializeField] Slider expSlider;

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
        PanelShow();
    }

    public void Btn_PanelSelect(int idx)
    {
        currPanel = idx;
        PanelShow();
    }
    void PanelShow()
    {
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
        statTxts[0].text = GameManager.slotData.lvl.ToString();
        statTxts[1].text = string.Concat(GameManager.slotData.exp, " / ", SlotData.reqExp[GameManager.slotData.lvl]);
        expSlider.value = GameManager.slotData.exp / (float)SlotData.reqExp[GameManager.slotData.lvl];

        int i, j;
        for (i = j = 2; i < 13; i++, j++)
        {
            if (i == 3) i++;
            statTxts[j].text = GameManager.slotData.itemStats[i].ToString();
        }

        statTxts[8].text = string.Concat(statTxts[8].text, "%");
        statTxts[9].text = string.Concat(statTxts[9].text, "%");

        foreach(Text t in statDelta)
            t.text = string.Empty;
    }

    public void BedToSmith(ItemCategory currC, Rarity currR, int currL, KeyValuePair<int, Equipment> selected) => TM.BedToSmith(currC, currR, currL, selected);
}
