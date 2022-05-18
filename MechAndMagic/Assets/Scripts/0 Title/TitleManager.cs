using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum TitleState
{
    Title, Start, Option
}

public class TitleManager : MonoBehaviour
{
    /// <summary> 0 Title, 1 Start, 2 Option </summary>
    [SerializeField] GameObject[] uiPanels;
    [SerializeField] GameObject creditPanel;

    #region Option
    [SerializeField] Slider bgmSlider;
    [SerializeField] Slider sfxSlider;
    [SerializeField] Slider txtSpdSlider;
    #endregion

    #region GameSlot
    [SerializeField] GameSlot[] slots;

    int currSlot = -1;
    [SerializeField] GameObject charSelectPanel;
    int currClass = -1;
    [SerializeField] GameObject[] charExplainPanels;

    [SerializeField] GameObject slotDeletePanel;
    #endregion
    TitleState state;

    private void Start()
    {
        state = TitleState.Title;
        bgmSlider.value = (float)GameManager.sound.option.bgm;
        sfxSlider.value = (float)GameManager.sound.option.sfx;
        txtSpdSlider.value = GameManager.sound.option.txtSpd / 2f;
        
        PanelSet();
        Start_SlotSet();
        GameManager.sound.PlayBGM(BGM.Title);
    }

    #region Title
    public void Btn_Title_Exit() => Application.Quit();
    #endregion

    #region Option
    public void Slider_BGM() => GameManager.sound.BGMSet(bgmSlider.value);
    public void Slider_SFX() => GameManager.sound.SFXSet(sfxSlider.value);
    public void Slider_TxtSpd()
    {
        txtSpdSlider.value = Mathf.RoundToInt(txtSpdSlider.value * 2) / 2f;
        GameManager.sound.TxtSet(txtSpdSlider.value * 2);
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
        charExplainPanels[currClass = classIdx].SetActive(true);
    }

    public void Btn_Start_SlotNewClassYes()
    {
        GameManager.NewSlot(currSlot, currClass);
        UnityEngine.SceneManagement.SceneManager.LoadScene("1 Town");
    }

    public void Btn_Start_SlotNewClassNo()
    {
        charSelectPanel.SetActive(true);
        charExplainPanels[currClass].SetActive(false);
        currClass = -1;
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

    private void Start_SlotSet()
    {
        for (int i = 0; i < GameManager.SLOTMAX; i++)
            slots[i].SlotUpdate(GameManager.HexToObj<SlotData>(PlayerPrefs.GetString(string.Concat("Slot", i))));
    }
    #endregion Start

    public void Btn_SelectPanel(int idx)
    {
        state = (TitleState)idx;
        PanelSet();
    }

    private void PanelSet()
    {
        for (int i = 0; i < uiPanels.Length; i++)
            uiPanels[i].SetActive(i == (int)state);
        creditPanel.SetActive(false);
    }
}
