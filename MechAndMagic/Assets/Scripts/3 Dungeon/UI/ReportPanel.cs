using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class ReportPanel : MonoBehaviour
{
    [SerializeField] Text successTxt;
    ///<summary> 돌발퀘스트 정보 표시 UI Set, 돌발 퀘스트 없으면 비활성화 </summary>
    [SerializeField] GameObject outbreakPanel;
    ///<summary> 돌발 퀘스트 표기 텍스트
    ///<para> 0 name, 1 success(기본값 성공), 2 script, 3 reward </para> </summary>
    [Tooltip("0 name, 1 success, 2 script, 3 reward")]
    [SerializeField] Text[] outbreakTxts;

    ///<summary> 경험치 표시 슬라이더 </summary>
    [Header("EXP")]
    [SerializeField] Slider expSlider;
    ///<summary> 경험치 획득량 표시 텍스트 </summary>
    [SerializeField] Text expTxt;
    ///<summary> 레벨업 시 표기 텍스트 </summary>
    [SerializeField] GameObject lvlUpTxt;

    ///<summary> 드롭 아이콘, 5개 한세트 </summary>
    [Header("Drop")]
    [SerializeField] DropToken dropTokenPrefab;
    ///<summary> 드랍 토큰 부모 오브젝트 </summary>
    [SerializeField] RectTransform tokenParent;
    [SerializeField] PopUpManager pm;

    ///<summary> 보고서 정보 불러오기 </summary>
    public void LoadData(bool isClear)
    {
        successTxt.text = isClear ? "- <color=#86ff64>성공</color>" : "- <color=#F43021>실패</color>";

        LoadOutbreakData();
        LoadExpData();
        LoadDropData();
    }
    ///<summary> 돌발퀘스트 클리어 정보 불러오기 </summary>
    void LoadOutbreakData()
    {
        KeyValuePair<QuestBlueprint, int> outbreak = QuestManager.GetProceedingQuestData()[3];
        QuestProceed outbreakProceed = GameManager.Instance.slotData.questData.outbreakProceed;

        //돌발 퀘스트 없음 -> 돌발 퀘스트 정보 제거
        if(outbreakProceed.state == QuestState.NotReceive || outbreakProceed.idx <= 0)
            outbreakPanel.SetActive(false);
        //돌발 퀘스트 성공
        else if (outbreakProceed.state == QuestState.CanClear)
        {
            outbreakTxts[0].text = outbreak.Key.name;
            outbreakTxts[2].text = outbreak.Key.doneScript;

            if(outbreak.Key.rewardIdx[0] == 150)
                outbreakTxts[3].text = $"경험치 {outbreak.Key.rewardAmt[0]} 획득";
            else

            outbreakTxts[3].text = $"{ItemManager.GetResourceName(outbreak.Key.rewardIdx[0])} {outbreak.Key.rewardAmt[0]}개 획득";
            QuestManager.ClearOutbreak();
        }
        //돌발 퀘스트 실패
        else
        {
            outbreakTxts[0].text = outbreak.Key?.name;
            outbreakTxts[1].text = "실패";
            outbreakTxts[1].color = new Color(0xed / 255f, 0x29 / 255f, 0x29 / 255f, 1);
            outbreakTxts[2].text = outbreakTxts[3].text = string.Empty;
        }
    }
    ///<summary> 경험치 획득 정보 불러오기 </summary>
    void LoadExpData()
    {
        if (GameManager.SlotLvl <= 9)
            expSlider.value = (float)GameManager.Instance.slotData.exp / GameManager.GetReqExp();
        else
            expSlider.value = 1;
        expTxt.text =$"+ {GameManager.Instance.slotData.dungeonData.dropExp} exp";
        lvlUpTxt.SetActive(GameManager.Instance.slotData.dungeonData.isLvlUp);
    }
    ///<summary> 아이템 획득 정보 불러오기 </summary>
    void LoadDropData()
    {
        List<Triplet<DropType, int, int>> drops = GameManager.Instance.slotData.dungeonData.dropList;

        DropToken token;
        List<Triplet<DropType, int, int>> idxs = new List<Triplet<DropType, int, int>>();

        for(int i = 0;i < drops.Count;)
        {
            token = GameManager.GetToken(null, tokenParent, dropTokenPrefab);

            for(int j = 0;j < 5 && i < drops.Count;i++, j++)
                idxs.Add(drops[i]);

            token.Initialize(pm, idxs);
            idxs.Clear();
        }
    }

    ///<summary> 마을로 돌아가기 버튼 </summary>
    public void Btn_GoToTown() => GameManager.Instance.LoadScene(SceneKind.Town);
    
}
