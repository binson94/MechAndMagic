using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum TownState
{
    Town, Bed, Dungeon, Smith, Script
}

public interface ITownPanel
{
    public void ResetAllState();
}

public class TownManager : MonoBehaviour
{
    [SerializeField] GameObject[] uiPanels;
    ITownPanel[] townPanels;
    TownState state;

    private void Start()
    {
        GameManager.sound.PlayBGM(BGM.Town1);
        state = TownState.Town;
        townPanels = new ITownPanel[uiPanels.Length];
        for(int i = 1;i < uiPanels.Length;i++)
            townPanels[i] = uiPanels[i].GetComponent<ITownPanel>();

        PanelSet();
        ItemManager.ItemDrop(GameManager.slotData.slotClass, 84, 1);
    }

    public void Btn_SelectPanel(int idx)
    {
        state = (TownState)idx;
        townPanels[idx].ResetAllState();
        PanelSet();
    }
    public void Btn_BackToTown()
    {
        state = TownState.Town;
        PanelSet();
    }
    
    private void PanelSet()
    {
        for (int i = 0; i < uiPanels.Length; i++)
            uiPanels[i].SetActive(i == (int)state);
    }
}
