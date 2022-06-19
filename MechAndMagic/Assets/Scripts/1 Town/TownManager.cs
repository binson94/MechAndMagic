using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum TownState
{
    Lobby, Bed, Dungeon, Smith, Script
}

public interface ITownPanel
{
    public void ResetAllState();
}

public class TownManager : MonoBehaviour
{
    ///<summary> 기계, 마법 캔버스 구분, 인스펙터 할당 </summary>
    [SerializeField] GameObject[] canvases;
    ///<summary> 기계 진영 자식 캔버스들(Lobby, Bed, Dungeon, Smith, Script), 인스펙터 할당 </summary>
    [SerializeField] GameObject[] mechUiPanels;
    ///<summary> 마법 진영 자식 캔버스들(Lobby, Bed, Dungeon, Smith, Script), 인스펙터 할당 </summary>
    [SerializeField] GameObject[] magicUiPanels;
    ///<summary> 기계, 마법 진영에 따라 mechUiPanels, magicUiPanels 할당 </summary>
    GameObject[] uiPanels;
    ///<summary> uiPanels에서 GetComponent로 얻음 </summary>
    ITownPanel[] townPanels;

    ///<summary> 배경 이미지 </summary>
    Image bgImage;
    ///<summary> 배경 이미지 스프라이트들, 인스펙터에서 할당
    ///<para> 1to2 기계, 3to4 기계, 1to2 마법, 3to4 마법 순 </para>
    ///</summary>
    [SerializeField] Sprite[] bgSprites;

    ///<summary> 현재 열린 판넬 정보 </summary>
    TownState state;

    ///<summary> 옵션 판넬 </summary>
    [SerializeField] GameObject optionPanel;
    ///<summary> 크레딧 판넬 </summary>
    [SerializeField] GameObject creditPanel;
    [SerializeField] Slider bgmSlider;
    [SerializeField] Slider sfxSlider;
    [SerializeField] Slider txtSpdSlider;

    private void Start()
    {
        //기계, 마법 진영에 따라 캔버스 선택
        for (int i = 0; i < 2; i++)
            canvases[i].SetActive(GameManager.instance.slotData.slotClass < 5 ^ i == 1);
        uiPanels = GameManager.instance.slotData.slotClass < 5 ? mechUiPanels : magicUiPanels;
        
        //진영 및 챕터에 따라 배경 이미지 설정
        bgImage = (GameManager.instance.slotData.slotClass < 5 ? canvases[0].transform.GetChild(0) : canvases[1].transform.GetChild(0)).GetComponent<Image>();
        bgImage.sprite = bgSprites[2 * (GameManager.instance.slotData.slotClass / 5)];
        
        //ITownPanel GetComponent로 얻음
        townPanels = new ITownPanel[uiPanels.Length];
        for (int i = 0; i < uiPanels.Length; i++)
            townPanels[i] = uiPanels[i].GetComponent<ITownPanel>();

        //Lobby 판넬에서 시작
        Btn_SelectPanel(0);

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

    ///<summary> 판넬 선택 버튼 </summary>
    ///<param name="idx"> 판넬 인덱스, TownState 나열형과 대응 </param>
    public void Btn_SelectPanel(int idx)
    {
        state = (TownState)idx;
        townPanels[idx].ResetAllState();
        PanelSet();
    }
    
    ///<summary> 숙소에서 아이템 제작을 위해 대장간으로 넘어가기 위한 중계 함수 </summary>
    public void BedToSmith(ItemCategory currC, Rarity currR, int currL, KeyValuePair<int, Equipment> selected)
    {
        state = TownState.Smith;
        townPanels[(int)state].ResetAllState();
        uiPanels[(int)state].GetComponent<SmithPanel>().BedToSmith(currC, currR, currL, selected);
        PanelSet();
    }

    ///<summary> 선택만 판넬만 보이게 설정 </summary>
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
        txtSpdSlider.value = Mathf.RoundToInt(txtSpdSlider.value * 2) / 2f;
        SoundManager.instance.TxtSet(txtSpdSlider.value);
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
