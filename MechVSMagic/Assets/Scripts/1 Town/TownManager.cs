using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum TownState
{
    Town, Bed, Dungeon, Smith, Dictionary, Quest
}

public class TownManager : MonoBehaviour
{
    [SerializeField] GameObject[] uiPanels;

    [SerializeField] BedManager BM;
    [SerializeField] SmithManager SM;
    TownState state;

    private void Start()
    {
        GameManager.sound.PlayBGM(BGM.Town1);
        state = TownState.Town;
        PanelSet();
        ItemManager.ItemDrop(1, 84, 1);
    }

    #region Town
    public void Btn_Town_Quest()
    {
        state = TownState.Quest;
        PanelSet();
    }
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
        BM.ResetAllState();
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
        for (int i = 0; i < uiPanels.Length; i++)
            uiPanels[i].SetActive(i == (int)state);
    }
}
