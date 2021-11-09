using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TownState
{
    Town, Bed, Dungeon, Smith, Dictionary
}

public class TownManager : MonoBehaviour
{
    [SerializeField] GameObject[] uiPanels;
    TownState state;

    private void Start()
    {
        state = TownState.Town;
        PanelSet();
    }

    #region Town

    #endregion

    #region Bed

    #endregion

    #region Dungeon

    #endregion

    #region Smith

    #endregion

    #region Dictionary

    #endregion

    #region Common
    public void Btn_Common_Bed()
    {
        state = TownState.Bed;
        PanelSet();
    }

    public void Btn_Common_Dungeon()
    {
        state = TownState.Dungeon;
        PanelSet();
    }

    public void Btn_Common_Smith()
    {
        state = TownState.Smith;
        PanelSet();
    }

    public void Btn_Common_Dictionary()
    {
        state = TownState.Dictionary;
        PanelSet();
    }

    public void Btn_Common_BackToTown()
    {
        state = TownState.Town;
        PanelSet();
    }
    #endregion
    
    private void PanelSet()
    {
        for (int i = 0; i < 5; i++)
            uiPanels[i].SetActive(i == (int)state);
    }
}
