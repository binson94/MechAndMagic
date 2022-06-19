using System.Collections;
using System.Collections.Generic;
using UnityEngine;

///<summary> 퀘스트 진행 상태 표시 나열형 </summary>
public enum QuestState
{
    NotReceive, Proceeding, CanClear
}
///<summary> 개별 퀘스트 진행 상황 저장을 위한 클래스
///<para> QuestSlot 클래스에서 진행 중인 퀘스트 정보 리스트 형태로 관리 </para>
///</summary>
public class QuestProceed
{
    ///<summary> 퀘스트 인덱스 </summary>
    public int idx;
    ///<summary> 퀘스트 타입 </summary>
    public QuestType type;
    ///<summary> 퀘스트 목표 종류(ex : 몬스터 인덱스) </summary>
    public int objectIdx;
    ///<summary> 퀘스트 현재 진행 상황 </summary>
    public int objectCurr;
    ///<summary> 퀘스트 목표 요구량 </summary>
    public int objectReq;
    ///<summary> 퀘스트 진행 상태 </summary>
    public QuestState state;

    ///<summary> 기본 생성자 </summary>
    public QuestProceed()
    {
        idx = 0;
        objectCurr = 0;
        state = QuestState.NotReceive;
    }
    ///<summary> 퀘스트 수락 시 사용하는 생성자 </summary>
    public QuestProceed(QuestBlueprint qbp)
    {
        idx = qbp.idx;
        type = qbp.type;
        objectIdx = qbp.objectIdx;
        objectCurr = 0; objectReq = qbp.objectAmt;
        state = QuestState.Proceeding;
    }
    ///<summary> 돌발 퀘스트 수락 시 사용하는 함수 </summary>
    public void AcceptOutbreak(QuestBlueprint qbp)
    {
        idx = qbp.idx;
        type = qbp.type;
        objectIdx = qbp.objectIdx;
        objectCurr = 0; objectReq = qbp.objectAmt;
        state = QuestState.Proceeding;
    }
}

//퀘스트 정보 관리
public class QuestManager : MonoBehaviour
{
    const int QUEST_AMT = 10;
    const int OUTBREAK_AMT = 23;
    static QuestBlueprint[] questData = new QuestBlueprint[QUEST_AMT];
    static QuestBlueprint[] outbreakData = new QuestBlueprint[OUTBREAK_AMT];

    public static void LoadData()
    {
        for (int i = 0; i < questData.Length; i++)
            questData[i] = new QuestBlueprint(false, i);
        for (int i = 0; i < outbreakData.Length; i++)
            outbreakData[i] = new QuestBlueprint(true, i);
    }

    ///<summary> 현재 수행 중인 퀘스트 정보 반환 </summary>
    public static List<QuestProceed> GetCurrQuest() => GameManager.instance.slotData.questData.GetCurrQuest();
    public static KeyValuePair<QuestBlueprint, int>[] GetProceedingQuestData()
    {
        QuestProceed qp;
        KeyValuePair<QuestBlueprint, int>[] list = new KeyValuePair<QuestBlueprint, int>[4];
        int i;
        for(i = 0;i < GameManager.instance.slotData.questData.proceedingQuestList.Count;i++)
        {
            qp = GameManager.instance.slotData.questData.proceedingQuestList[i];
            list[i] = new KeyValuePair<QuestBlueprint, int>(questData[qp.idx], qp.objectCurr);
        }
        for(;i < 3;i++)
            list[i] = new KeyValuePair<QuestBlueprint, int>(null, 0);

        qp = GameManager.instance.slotData.questData.outbreakProceed;
        if(qp.state == QuestState.Proceeding || qp.state == QuestState.CanClear)
            list[3] = new KeyValuePair<QuestBlueprint, int>(outbreakData[qp.idx], qp.objectCurr);
        else
            list[3] = new KeyValuePair<QuestBlueprint, int>(null, 0);

        return list;
    }
    public static List<int> GetClearedQuest() => GameManager.instance.slotData.questData.GetClearedQuest();
    public static string GetQuestName(bool isOutbreak, int questIdx)
    {
        if(isOutbreak)
            return outbreakData[questIdx].name;
        else
            return questData[questIdx].name;
    }
    public static string GetQuestScript(bool isOutbreak, int questIdx)
    {
        if(isOutbreak)
            return outbreakData[questIdx].script;
        else
            return questData[questIdx].script;
    }

    #region QuestProgress
    ///<summary> 퀘스트 진행 업데이트 </summary>
    public static void QuestUpdate(QuestType type, int idx, int amt)
    {
        GameManager.instance.slotData.questData.QuestUpdate(type, idx, amt);
        SaveData();
    }
    ///<summary> 체력유지 돌발 퀘스트 업데이트 </summary>
    public static void DiehardUpdate(float rate) => GameManager.instance.slotData.questData.DiehardUpdate(rate);

    ///<summary> 새 퀘스트 받기, 마을 메뉴 또는 던전 돌발퀘 방에서 호출 </summary>
    public static void AcceptQuest(bool isOutbreak, int idx)
    {
        if (isOutbreak)
            GameManager.instance.slotData.questData.AcceptOutbreak(outbreakData[idx]);
        else
            GameManager.instance.slotData.questData.AcceptQuest(questData[idx]);

        SaveData();
    }

    ///<summary> 퀘스트 클리어 </summary>
    public static void ClearQuest(int idx)
    {
        GameManager.instance.slotData.questData.ClearQuest(idx);
        GetReward(false, questData[idx]);
        SaveData();
    }
    ///<summary> 돌발 퀘스트 클리어 </summary>
    public static void ClearOutbreak()
    {
        int idx = GameManager.instance.slotData.questData.GetOutbreakIdx();
        if (idx > 0)
        {
            GetReward(true, outbreakData[idx]);
            GameManager.instance.slotData.questData.ClearOutbreak();
        }
        else
            GameManager.instance.slotData.questData.RemoveOutbreak();

        SaveData();
    }
    ///<summary> 돌발 퀘스트 수행 정보 제거(중도 포기) </summary>
    public static void RemoveOutbreak()
    {
        GameManager.instance.slotData.questData.RemoveOutbreak();
        SaveData();
    }
    ///<summary> 퀘스트 클리어에 따른 보상 획득
    ///<para> ClearQuest에서 호출</summary>
    static void GetReward(bool isOutbreak, QuestBlueprint qbp)
    {
        for (int i = 0; i < qbp.rewardCount; i++)
        {
            switch ((int)qbp.rewardIdx[i])
            {
                //경험치
                case 150:
                    GameManager.instance.GetExp(qbp.rewardAmt[i]);
                    break;
            }
        }
    }
    #endregion QuestProgress

    static void SaveData() => GameManager.instance.SaveSlotData();
}
