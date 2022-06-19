using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class DungeonSelectToken : MonoBehaviour
{
    ///<summary> 현재 버튼이 지정하는 던전의 인덱스 </summary>
    public int dungeonIdx;
    ///<summary> 던전 입장 불가 여부 </summary>
    bool isLock;
    ///<summary> 잠금 시 이미지 </summary>
    [SerializeField] GameObject lockImage;

    ///<summary> 던전 이름 텍스트 </summary>
    [SerializeField] Text nameTxt;
    ///<summary> 권장 레벨 텍스트 </summary>
    [SerializeField] Text recLvlTxt;
    ///<summary> 던전 입장 불가 이유 텍스트 </summary>
    [SerializeField] Text lockReasonTxt;
    ///<summary> 던전 시작 버튼 </summary>
    [SerializeField] GameObject startBtn;
    DungeonPanel mgr;

    public void SetData(int dungeonIdx, LitJson.JsonData json, DungeonPanel m)
    {
        mgr = m;
        this.dungeonIdx = dungeonIdx;
        int tmp;

        nameTxt.text = json[dungeonIdx]["name"].ToString();
        recLvlTxt.text = string.Concat("권장 레벨 : Lv.", (int)json[dungeonIdx]["reclvl"]);

        if(GameManager.instance.slotData.lvl < (tmp = (int)json[dungeonIdx]["reqlvl"]))
        {
            isLock = true; lockReasonTxt.text = $"레벨 {tmp} 달성 필요";
        }
        else if(!QuestManager.GetClearedQuest().Contains(tmp = (int)json[dungeonIdx]["request"]))
        {
            isLock = true; lockReasonTxt.text = $"{QuestManager.GetQuestName(false, tmp)} 미완료";
        }
        else
            isLock = false;

        lockImage.SetActive(isLock);
        startBtn.SetActive(false);
    }
    
    public void ToggleStartBtn(bool show) => startBtn.gameObject.SetActive(show);

    ///<summary> 던전 이름 누를 시, 설명 보여주기 토글 </summary>
    public void Btn_ToggleScript() => mgr.Btn_SelectDungeon(dungeonIdx);
    ///<summary> 던전 시작 버튼 </summary>
    public void Btn_StartDungeon() => mgr.Btn_StartDungeon(dungeonIdx);
}
