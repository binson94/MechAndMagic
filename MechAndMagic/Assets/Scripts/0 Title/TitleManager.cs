using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum TitleState
{
    Title, Dictionary, Option, Start
}

public class TitleManager : MonoBehaviour
{
    [SerializeField] GameObject[] uiPanels;
    [SerializeField] GameObject creditPanel;

    #region Option
    [SerializeField] Slider bgmSlider;
    [SerializeField] Slider sfxSlider;

    [SerializeField] Slider txtSizeSlider;
    [SerializeField] Slider txtSpdSlider;
    #endregion

    #region GameSlot
    [SerializeField] GameSlot[] slots;

    int currSlot = -1;
    [SerializeField] GameObject charSelectPanel;
    int currExplain = -1;
    [SerializeField] GameObject[] charExplainPanels;

    [SerializeField] GameObject slotDeletePanel;
    #endregion
    TitleState state;

    private void Start()
    {
        state = TitleState.Title;
        bgmSlider.value = PlayerPrefs.GetFloat("BGM", 1);
        sfxSlider.value = PlayerPrefs.GetFloat("SFX", 1);
        Slider_BGM();
        Slider_SFX();

        txtSizeSlider.value = PlayerPrefs.GetInt("TxtSize", 1) / 2f;
        txtSpdSlider.value = PlayerPrefs.GetInt("TxtSpd", 1) / 2f;

        PanelSet();

        Start_SlotSet();

        GameManager.sound.PlayBGM(BGM.Title);
    }

    #region Title
    public void Btn_Title_Exit() => Application.Quit();
    #endregion

    #region Dictionary
    #endregion

    #region Option
    public void Slider_BGM()
    {
        GameManager.sound.BGMSet(bgmSlider.value);
    }

    public void Slider_SFX()
    {
        GameManager.sound.SFXSet(sfxSlider.value);
    }
    
    public void Slider_TxtSize()
    {
        PlayerPrefs.SetInt("TxtSize", Mathf.RoundToInt(txtSizeSlider.value * 2));
        txtSizeSlider.value = Mathf.RoundToInt(txtSizeSlider.value * 2) / 2f;
    }

    public void Slider_TxtSpd()
    {
        PlayerPrefs.SetInt("TxtSpd", Mathf.RoundToInt(txtSpdSlider.value * 2));
        txtSpdSlider.value = Mathf.RoundToInt(txtSpdSlider.value * 2) / 2f;
    }

    public void Btn_Option_Credit()
    {
        uiPanels[(int)TitleState.Option].SetActive(false);
        creditPanel.SetActive(true);
    }
    #endregion

    #region Start
    public void Btn_Start_SlotLoad(int slot)
    {
        GameManager.LoadSlotData(slot);
        QuestManager.LoadData();
        ItemManager.LoadData();

        string name = "1 Town";
        switch(GameManager.slotData.nowScene)
        {
            case SceneKind.Dungeon:
                name = "2_0 Dungeon";
                break;
            case SceneKind.Battle:
                name = "2_1 Battle";
                break;
            case SceneKind.Event:
                name = "2_2 Event";
                break;
            case SceneKind.Outbreak:
                name = "2_3 Outbreak";
                break;
        }
        UnityEngine.SceneManagement.SceneManager.LoadScene(name);
    }

    #region start_New
    public void Btn_Start_SlotNew(int slot)
    {
        currSlot = slot;
        charSelectPanel.SetActive(true);
        uiPanels[(int)TitleState.Start].SetActive(false);
    }

    public void Btn_Start_SlotNewBack()
    {
        currSlot = -1;
        charSelectPanel.SetActive(false);
        uiPanels[(int)TitleState.Start].SetActive(true);
    }

    public void Btn_Start_SlotNewClass(int classIdx)
    {
        charSelectPanel.SetActive(false);
        charExplainPanels[currExplain = classIdx].SetActive(true);
    }

    public void Btn_Start_SlotNewClassYes()
    {
        GameManager.NewSlot(currSlot, currExplain);
        GameManager.SaveSlotData();
        QuestManager.LoadData();
        ItemManager.LoadData();

        UnityEngine.SceneManagement.SceneManager.LoadScene("1 Town");
    }

    public void Btn_Start_SlotNewClassNo()
    {
        charSelectPanel.SetActive(true);
        charExplainPanels[currExplain].SetActive(false);
        currExplain = -1;
    }
    #endregion start_New

    #region start_Delete
    public void Btn_Start_SlotDelete(int slot)
    {
        currSlot = slot;
        slotDeletePanel.SetActive(true);
    }

    public void Btn_Start_SlotDeleteYes()
    {
        GameManager.DeleteSlot(currSlot);
        currSlot = -1;

        slotDeletePanel.SetActive(false);
        Start_SlotSet();
    }

    public void Btn_Start_SlotDeleteNo()
    {
        currSlot = -1;
        slotDeletePanel.SetActive(false);
    }
    #endregion start_Delete

    public void Btn_Start_Prestige()
    {
        Debug.Log("Prestige");
    }

    private void Start_SlotSet()
    {
        for (int i = 0; i < GameManager.SLOTMAX; i++)
            slots[i].SlotUpdate(LitJson.JsonMapper.ToObject<SlotData>(PlayerPrefs.GetString(string.Concat("SlotData", i))));
    }
    #endregion Start

    public void Btn_SelectPanel(int idx)
    {
        state = (TitleState)idx;
        PanelSet();
    }

    private void PanelSet()
    {
        for (int i = 0; i < 4; i++)
            uiPanels[i].SetActive(i == (int)state);
        creditPanel.SetActive(false);
    }
}
