using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TitleState
{
    Title, Dictionary, Option, Start
}

public class TitleManager : MonoBehaviour
{
    [SerializeField] GameObject[] uiPanels;
    TitleState state;

    private void Start()
    {
        state = TitleState.Title;
        PanelSet();
    }

    #region Title
    public void Btn_Title_Start()
    {
        state = TitleState.Start;
        PanelSet();
    }

    public void Btn_Title_Dictionary()
    {
        state = TitleState.Dictionary;
        PanelSet();
        Debug.Log("Dictionary");
    }

    public void Btn_Title_Option()
    {
        state = TitleState.Option;
        PanelSet();
        Debug.Log("Option");
    }

    public void Btn_TItle_Exit()
    {
        Application.Quit();
    }
    #endregion

    #region Dictionary
    #endregion

    #region Option
    #endregion

    #region Start
    public void Btn_Start_SelectSlot(int idx)
    {
        GameManager.currSlot = idx;
        QuestDataManager.LoadData();

        Debug.Log(string.Concat("slot", idx));
        UnityEngine.SceneManagement.SceneManager.LoadScene("1 Town");
    }

    public void Btn_Start_Prestige()
    {
        Debug.Log("Prestige");
    }
    #endregion

    public void Btn_Common_BackToTitle()
    {
        state = TitleState.Title;
        PanelSet();
    }

    private void PanelSet()
    {
        for (int i = 0; i < 4; i++)
            uiPanels[i].SetActive(i == (int)state);
    }
}
