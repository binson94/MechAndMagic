using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OutbreakPanel : MonoBehaviour
{
    ///<summary> 돌발 퀘스트 설명 텍스트 </summary>
    [SerializeField] Text outbreakTxt;

    [Header("Reroll")]
    [SerializeField] Image rerollBtn;
    [SerializeField] Text rerollTxt;        
    bool isReroll = false;

    ///<summary> 돌발 퀘스트 방 도달 시 호출 </summary>
    public void OnOutbreakRoom()
    {
        QuestManager.AcceptQuest(true, GameManager.instance.slotData.dungeonData.currRoomEvent);
        outbreakTxt.text = QuestManager.GetProceedingQuestData()[3].Key.script;
    }

    ///<summary> 퀘스트 다시 받기 버튼 </summary>
    public void Btn_RerollOutbreak()
    {
        if(isReroll) return;
        isReroll = true;
        Debug.Log("show ad");
        //광고 재생

        int newQuest = GameManager.instance.slotData.dungeonData.currDungeon.GetNewOutbreak(GameManager.instance.slotData.dungeonData.currRoomEvent);
        QuestManager.AcceptQuest(true, newQuest);

        rerollBtn.color = new Color(1, 1, 1, 100f / 255);
        rerollTxt.color = new Color(1, 1, 1, 100f / 255);
        outbreakTxt.text = QuestManager.GetProceedingQuestData()[3].Key.script;

    }
}
