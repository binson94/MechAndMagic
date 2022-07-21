using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using LitJson;

public interface ITownPanel
{
    void ResetAllState();
}

public class TownManager : MonoBehaviour
{
    ///<summary> NPC 정보 </summary>
    NPC[] npcDatas = null;

    enum TownState { Lobby, Bed, Dungeon, Smith, Script }

    ///<summary> 자식 캔버스들(Lobby, Bed, Dungeon, Smith, Script), 인스펙터 할당 </summary>
    [SerializeField] GameObject[] uiPanels;
    ///<summary> uiPanels에서 GetComponent로 얻음 </summary>
    [SerializeField] ITownPanel[] townPanels;

    ///<summary> 배경 이미지 </summary>
    [SerializeField] Image bgImage;
    ///<summary> 마을 이름 텍스트 </summary>
    [SerializeField] Text townNameTxt;
    ///<summary> 배경 이미지 스프라이트들, 인스펙터에서 할당
    ///<para> 1to2 기계, 3to4 기계, 1to2 마법, 3to4 마법 순 </para>
    ///</summary>
    [SerializeField] Sprite[] bgSprites;

    ///<summary> 현재 열린 판넬 정보 </summary>
    TownState state;

    #region PlayerInfoPanel
    [Header("Player Info")]
    [SerializeField] GameObject playerInfoPanel;
    ///<summary> 클래스 텍스트 </summary>
    [SerializeField] Text classTxt;
    [SerializeField] Text lvlTxt;

    [Header("Equipment Info")]
    [Tooltip("0 lv1 ~ 4 lv9")]
    ///<summary> 장비 정보 프레임 스프라이트들
    ///<para> 0 lv1, 1 lv3, 2 lv5, 3 lv7, 4 lv9</para>
    ///</summary>
    [SerializeField] Sprite[] equipFrameSprites;
    [Tooltip("장비 아이콘")]
    ///<summary> 장착 중인 장비 정보 보여줄 이미지 </summary>
    [SerializeField] EquipInfoImage[] equipInfos;
    #endregion PlayerInfoPanel

    [SerializeField] GameObject optionPanel;

    private void Start()
    {
        bgImage.sprite = bgSprites[2 * (GameManager.instance.slotData.slotClass / 5) + ((GameManager.instance.slotData.chapter - 1) / 2)];

        //ITownPanel GetComponent로 얻음
        townPanels = new ITownPanel[uiPanels.Length];
        for (int i = 0; i < uiPanels.Length; i++)
            townPanels[i] = uiPanels[i].GetComponent<ITownPanel>();

        //Lobby 판넬에서 시작
        Btn_SelectPanel(0);

        SoundManager.instance.PlayBGM((BGMList)System.Enum.Parse(typeof(BGMList), $"Town{(GameManager.instance.slotData.chapter + 1) / 2}"));
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
    public void BedToSmith(SmithCategory currC, Rarity currR, int currL, KeyValuePair<int, Equipment> selected)
    {
        state = TownState.Smith;
        townPanels[(int)state].ResetAllState();
        uiPanels[(int)state].GetComponent<SmithPanel>().BedToSmith(currC, currR, currL, selected);
        PanelSet();
    }
    public void BedToSkillLearn(int skillIdx)
    {
        state = TownState.Smith;
        townPanels[(int)state].ResetAllState();
        uiPanels[(int)state].GetComponent<SmithPanel>().BedToSkillLearn(skillIdx);
        PanelSet();
    }
    ///<summary> 로비에서 NPC 선택 시 대화로 넘어가기 위한 중계 함수 </summary>
    public void LoadDialog(int npcIdx)
    {
        state = TownState.Script;
        townPanels[(int)state].ResetAllState();
        uiPanels[(int)state].GetComponent<ScriptPanel>().SelectNPC(npcIdx);
        PanelSet();
    }
    public NPC GetNPCData(int npcIdx)
    {
        if (npcDatas == null)
        {
            npcDatas = new NPC[8];
            for (int i = 0; i < 8; i++)
                npcDatas[i] = new NPC($"NPC{i}");
        }

        return npcDatas[npcIdx];
    }
    
    void ShowItemInfo()
    {
        townNameTxt.gameObject.SetActive(state != TownState.Dungeon);

        LoadPlayerInfo();
        LoadItemInfo();
        playerInfoPanel.SetActive(true);
        ///<summary> 플레이어 레벨 및 직업 표시 </summary>
        void LoadPlayerInfo()
        {
            classTxt.text = GameManager.instance.slotData.className;
            lvlTxt.text = $"Lv.{GameManager.instance.slotData.lvl}";
        }
        ///<summary> 현재 장착한 장비 정보 불러오기, 장비 정보 창에 적용 </summary>
        void LoadItemInfo()
        {
            for (int i = 0; i < 7; i++)
            {
                Equipment e = GameManager.instance.slotData.itemData.equipmentSlots[i + 1];
                if (e == null)
                    equipInfos[i].SetImage(equipFrameSprites[0], e);
                else
                    equipInfos[i].SetImage(equipFrameSprites[(int)e.ebp.rarity - 1], e);

            }
        }

    }

    ///<summary> 선택만 판넬만 보이게 설정 </summary>
    private void PanelSet()
    {
        for (int i = 0; i < uiPanels.Length; i++)
            uiPanels[i].SetActive(i == (int)state);

        if (state == TownState.Lobby || state == TownState.Dungeon || state == TownState.Script)
            ShowItemInfo();
        else
        {
            townNameTxt.gameObject.SetActive(false);
            playerInfoPanel.SetActive(false);
        }
    }

    public void Btn_OpenOption() => optionPanel.SetActive(true);
    public void Btn_GoToTitle()
    {
        GameManager.instance.slotData = null;
        GameManager.instance.LoadScene(SceneKind.Title);
    }

    public void Btn_SFX() => SoundManager.instance.PlaySFX(22);
}
