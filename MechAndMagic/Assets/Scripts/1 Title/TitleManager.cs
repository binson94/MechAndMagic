using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TitleManager : MonoBehaviour
{
    enum TitleState { Title, SlotSelect, ClassSelect, ClassInfo, Option }
    
    [Header("UI")]
    ///<summary> 0 Title, 1 SlotSelect, 2ClassSelect, 3 ClassInfo, 4 Option </summary>
    [SerializeField] GameObject[] uiPanels;
    ///<summary> option - credit 시 표시할 판넬 </summary>
    [SerializeField] GameObject creditPanel;

    #region GameSlot
    [Header("Slot")]
    ///<summary> 슬롯 정보 판넬 </summary>
    [SerializeField] GameSlot[] slots;
    ///<summary> 슬롯 클래스 아이콘 </summary>
    [SerializeField] Sprite[] slotClassIcons;
    ///<summary> 슬롯 신영 프레임 </summary>
    [SerializeField] Sprite[] slotFrames;

    ///<summary> 현재 선택 중인 슬롯 </summary>
    int currSlot = -1;
    ///<summary> 현재 선택 중인 캐릭터 </summary>
    int currClass = -1;
    ///<summary> 캐릭터 선택 시 보여줄 설명창 </summary>
    [SerializeField] GameObject[] charExplainPanels;
    ///<summary> 슬롯 삭제 시 재확인창 </summary>
    [SerializeField] GameObject slotDeletePanel;
    #endregion GameSlot

    TitleState state;

    private void Start()
    { 
        state = TitleState.Title;
        
        PanelSet();
        SlotUpdate();
        SoundManager.instance.PlayBGM(BGMList.Title);
    }

    #region Start
    ///<summary> 진행 중이던 슬롯 불러옴 </summary>
    public void Btn_LoadSlot(int slot)
    {
        GameManager.instance.LoadSlotData(slot);
        ItemManager.LoadSetData();
        GameManager.instance.LoadScene(GameManager.instance.slotData.nowScene);
    }

    #region start_New
    ///<summary> 빈 슬롯 선택 - 캐릭터 선택 창 보여줌 </summary>
    public void Btn_CreateNewSlot(int slot)
    {
        currSlot = slot;
        Btn_SelectPanel((int)TitleState.ClassSelect);
    }
    ///<summary> 새로운 슬롯 생성 중, 취소 버튼 - 슬롯 선택 창으로 넘어감 </summary>
    public void Btn_CancelSlotSelect()
    {
        currSlot = -1;
        Btn_SelectPanel((int)TitleState.SlotSelect);
    }
    ///<summary> 캐릭터 선택 - 캐릭터 설명창 보여줌 </summary>
    public void Btn_SelectClass(int classIdx)
    {
        currClass = classIdx;
        for(int i = 1;i<=8;i++)
            if(i == currClass)
                charExplainPanels[i].SetActive(true); 
            else
                charExplainPanels[i].SetActive(false);

        Btn_SelectPanel((int)TitleState.ClassInfo);
    }
    ///<summary> 캐릭터 선택 확정 - 게임 시작 </summary>
    public void Btn_ConfirmClassSelect()
    {
        GameManager.instance.CreateNewSlot(currSlot, currClass);
        ItemManager.LoadSetData();
        GameManager.instance.LoadScene(SceneKind.Story);
    }
    ///<summary> 캐릭터 선택 취소 - 캐릭터 선택 창 보여줌 </summary>
    public void Btn_CancelClassSelect()
    {
        currClass = -1;
        Btn_SelectPanel((int)TitleState.ClassSelect);
    }
    #endregion start_New

    #region start_Delete
    ///<summary> 슬롯 삭제 버튼 </summary>
    public void Btn_DeleteSlot(int slot)
    {
        currSlot = slot;
        slotDeletePanel.SetActive(true);
    }
    ///<summary> 슬롯 삭제 확인 </summary>
    public void Btn_ConfirmDeleteSlot()
    {
        GameManager.instance.DeleteSlot(currSlot);
        currSlot = -1;

        slotDeletePanel.SetActive(false);
        SlotUpdate();
    }
    ///<summary> 슬롯 삭제 취소 </summary>
    public void Btn_CancelDeleteSlot()
    {
        currSlot = -1;
        slotDeletePanel.SetActive(false);
    }
    #endregion start_Delete

    ///<summary> 슬롯 정보 업데이트 </summary>
    private void SlotUpdate()
    {
        for (int i = 0; i < GameManager.SLOTMAX; i++)
        {
            SlotData slotData = GameManager.HexToObj<SlotData>(PlayerPrefs.GetString($"Slot{i}"));
            if(slotData != null)
                slots[i].SlotUpdate(slotData, slotClassIcons[slotData.slotClass], slotFrames[slotData.region - 10]);
            else
                slots[i].SlotUpdate();
        }
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
    
    public void Btn_Sound() => SoundManager.instance.PlaySFX(22);
    public void Btn_Title_Exit() => Application.Quit();
}
