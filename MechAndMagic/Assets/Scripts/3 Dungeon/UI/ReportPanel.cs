using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ReportPanel : MonoBehaviour
{
    ///<summary> 돌발퀘스트 정보 표시 UI Set, 돌발 퀘스트 없으면 비활성화 </summary>
    [SerializeField] GameObject outbreakPanel;
    ///<summary> 돌발 퀘스트 표기 텍스트
    ///<para> 0 name, 1 success(기본값 성공), 2 reward </para> </summary>
    [Tooltip("0 name, 1 success, 2 reward")]
    [SerializeField] Text[] outbreakTxts;

    ///<summary> 경험치 표시 슬라이더 </summary>
    [Header("EXP")]
    [SerializeField] Slider expSlider;
    ///<summary> 경험치 획득량 표시 텍스트 </summary>
    [SerializeField] Text expTxt;
    ///<summary> 레벨업 시 표기 텍스트 </summary>
    [SerializeField] GameObject lvlUpTxt;

    ///<summary> 드롭 세부 정보, 버튼 터치 지속 시 활성화 </summary>
    [Header("Drop")]
    [SerializeField] RectTransform dropPopup;
    ///<summary> 팝업에 보여줄 아이템 이름 </summary>
    [SerializeField] Text popUpTxt;

    ///<summary> 드롭 아이콘, 5개 한세트 </summary>
    [SerializeField] DropToken dropTokenPrefab;
    [SerializeField] RectTransform tokenParent;
    [SerializeField] RectTransform viewPoint;

    ///<summary> 보고서 정보 불러오기 </summary>
    public void LoadData()
    {
        LoadOutbreakData();
        LoadExpData();
        LoadDropData();
    }
    ///<summary> 돌발퀘스트 클리어 정보 불러오기 </summary>
    void LoadOutbreakData()
    {
        KeyValuePair<QuestBlueprint, int> outbreak = QuestManager.GetProceedingQuestData()[3];
        QuestProceed outbreakProceed = GameManager.instance.slotData.questData.outbreakProceed;

        //돌발 퀘스트 없음 -> 돌발 퀘스트 정보 제거
        if(outbreakProceed.state == QuestState.NotReceive || outbreakProceed.idx <= 0)
            outbreakPanel.SetActive(false);
        //돌발 퀘스트 성공
        else if (outbreakProceed.state == QuestState.CanClear)
        {
            outbreakTxts[0].text = outbreak.Key.name;
            outbreakTxts[2].text = $"{outbreak.Key.rewardIdx[0]} : {outbreak.Key.rewardAmt[0]}";
            QuestManager.ClearOutbreak();
        }
        //돌발 퀘스트 실패
        else
        {
            outbreakTxts[0].text = outbreak.Key.name;
            outbreakTxts[1].text = "실패";
            outbreakTxts[1].color = new Color(0xed / 255f, 0x29 / 255f, 0x29 / 255f, 1);
            outbreakTxts[2].text = string.Empty;
        }
    }
    ///<summary> 경험치 획득 정보 불러오기 </summary>
    void LoadExpData()
    {
        expSlider.value = (float)GameManager.instance.slotData.exp / GameManager.reqExp[GameManager.instance.slotData.lvl];
        expTxt.text =$"+ {GameManager.instance.slotData.dungeonData.dropExp} exp";
        lvlUpTxt.SetActive(GameManager.instance.slotData.dungeonData.isLvlUp);
    }
    ///<summary> 아이템 획득 정보 불러오기 </summary>
    void LoadDropData()
    {
        List<Triplet<DropType, int, int>> drops = GameManager.instance.slotData.dungeonData.dropList;

        DropToken token;
        List<Triplet<DropType, int, int>> idxs = new List<Triplet<DropType, int, int>>();

        for(int i = 0;i < drops.Count;i++)
        {
            token = GameManager.GetToken(null, tokenParent, dropTokenPrefab);

            for(int j = 0;j < 5 && i < drops.Count;i++, j++)
                idxs.Add(drops[i]);

            token.Init(this, idxs);
            idxs.Clear();
        }
    }

    public void ShowPopUp(string script, Color txtColor, RectTransform btnRect)
    {
        dropPopup.SetParent(btnRect);
        dropPopup.anchoredPosition = new Vector2(Mathf.Min(0, 540 - dropPopup.rect.width - btnRect.anchoredPosition.x), +95);
        dropPopup.SetParent(viewPoint);
        popUpTxt.color = txtColor;
        popUpTxt.text = script;
        dropPopup.gameObject.SetActive(true);
    }
    public void HidePopUp() => dropPopup.gameObject.SetActive(false);

    ///<summary> 마을로 돌아가기 버튼 </summary>
    public void Btn_GoToTown() => GameManager.instance.LoadScene(SceneKind.Town);
    
}
