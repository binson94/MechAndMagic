using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class DungeonSelectToken : MonoBehaviour
{
    ///<summary> 현재 버튼이 지정하는 던전의 인덱스 </summary>
    public int jsonIdx;
    ///<summary> 던전 입장 불가 여부 </summary>
    bool isLock;
    ///<summary> 잠금 시 이미지 </summary>
    [SerializeField] GameObject lockImage;

    ///<summary> 던전 아이콘 이미지 </summary>
    [SerializeField] Image dungeonIconImage;
    ///<summary> 던전 메인 여부 표시 </summary>
    [SerializeField] Image dungeonFrameImage;
    ///<summary> 던전 이름 텍스트 </summary>
    [SerializeField] Text nameTxt;
    ///<summary> 권장 레벨 텍스트 </summary>
    [SerializeField] Text recLvlTxt;
    ///<summary> 던전 입장 불가 이유 텍스트 </summary>
    [SerializeField] Text lockReasonTxt;
    ///<summary> 던전 시작 버튼 </summary>
    [SerializeField] GameObject startBtn;
    DungeonPanel mgr;

    public void SetData(int jsonIdx, LitJson.JsonData json, Sprite iconSprite, Sprite frameSprite, DungeonPanel m)
    {
        mgr = m;
        this.jsonIdx = jsonIdx;

        dungeonIconImage.sprite = iconSprite;
        dungeonFrameImage.sprite = frameSprite;

        int tmp;

        nameTxt.text = json[jsonIdx]["name"].ToString();
        recLvlTxt.text = $"권장 레벨 : Lv.{(int)json[jsonIdx]["reclvl"]}";

        if(GameManager.instance.slotData.lvl < (tmp = (int)json[jsonIdx]["reqlvl"]))
        {
            isLock = true; lockReasonTxt.text = $"레벨 {tmp} 달성 필요";
        }
        else if(!QuestManager.GetClearedQuest().Contains(tmp = (int)json[jsonIdx]["request"]))
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
    public void Btn_ToggleScript() => mgr.Btn_SelectDungeon(jsonIdx);
    ///<summary> 던전 시작 버튼 </summary>
    public void Btn_StartDungeon() => mgr.Btn_StartDungeon(jsonIdx);
}
