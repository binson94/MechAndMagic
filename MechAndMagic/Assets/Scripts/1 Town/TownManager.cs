using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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
    //기계, 마법 캔버스 구분
    [SerializeField] GameObject[] canvases;
    [SerializeField] GameObject[] mechUiPanels;
    [SerializeField] GameObject[] magicUiPanels;
    Image bgImage;
    [SerializeField] Sprite[] bgSprites;

    GameObject[] uiPanels;
    ITownPanel[] townPanels;
    TownState state;

    [SerializeField] GameObject optionPanel;
    [SerializeField] GameObject creditPanel;
    [SerializeField] Slider bgmSlider;
    [SerializeField] Slider sfxSlider;
    [SerializeField] Slider txtSpdSlider;

    private void Start()
    {
        for (int i = 0; i < 2; i++)
            canvases[i].SetActive(GameManager.slotData.slotClass < 5 ^ i == 1);
        uiPanels = GameManager.slotData.slotClass < 5 ? mechUiPanels : magicUiPanels;
        bgImage = (GameManager.slotData.slotClass < 5 ? canvases[0].transform.GetChild(0) : canvases[1].transform.GetChild(0)).GetComponent<Image>();
        bgImage.sprite = bgSprites[2 * (GameManager.slotData.slotClass / 5)];
        
        state = TownState.Town;
        townPanels = new ITownPanel[uiPanels.Length];
        for (int i = 1; i < uiPanels.Length; i++)
            townPanels[i] = uiPanels[i].GetComponent<ITownPanel>();

        PanelSet();

        SoundManager.instance.PlayBGM(BGM.Town1);

        bgmSlider.value = (float)SoundManager.instance.option.bgm;
        sfxSlider.value = (float)SoundManager.instance.option.sfx;
        txtSpdSlider.value = SoundManager.instance.option.txtSpd / 2f;
        Btn_CloseOption();
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            Btn_OpenOption();
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

    public void BedToSmith(ItemCategory currC, Rarity currR, int currL, KeyValuePair<int, Equipment> selected)
    {
        state = TownState.Smith;
        townPanels[(int)state].ResetAllState();
        uiPanels[(int)state].GetComponent<SmithPanel>().BedToSmith(currC, currR, currL, selected);
        PanelSet();
    }

    private void PanelSet()
    {
        for (int i = 0; i < uiPanels.Length; i++)
            uiPanels[i].SetActive(i == (int)state);
    }

    #region Option
    public void Btn_OpenOption() => optionPanel.SetActive(true);
    public void Btn_CloseOption()
    {
        Btn_CloseCredit();
        optionPanel.SetActive(false);
    }
    public void Slider_BGM() => SoundManager.instance.BGMSet(bgmSlider.value);
    public void Slider_SFX() => SoundManager.instance.SFXSet(sfxSlider.value);
    public void Slider_TxtSpd()
    {
        PlayerPrefs.SetInt("TxtSpd", Mathf.RoundToInt(txtSpdSlider.value * 2));
        txtSpdSlider.value = Mathf.RoundToInt(txtSpdSlider.value * 2) / 2f;
    }

    public void Btn_OpenCredit()
    {
        Btn_CloseOption();
        creditPanel.SetActive(true);
    }
    public void Btn_CloseCredit()
    {
        creditPanel.SetActive(false);
        Btn_OpenOption();
    }
    #endregion Option
}
