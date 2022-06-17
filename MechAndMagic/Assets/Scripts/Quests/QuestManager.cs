using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public enum QuestState
{
    NotReceive, Proceeding, CanClear, Clear
}

public class QuestProceed
{
    public int objectCurr;
    public QuestState state;

    public QuestProceed()
    {
        objectCurr = 0;
        state = QuestState.NotReceive;
    }
}

public class QuestSlot
{
    //Json Data
    static QuestDB questDB = null;

    //User Data
    public QuestProceed[] questProceeds;
    public int outbreakIdx;
    public QuestProceed outbreakProceed;
    

    static QuestSlot() => questDB = new QuestDB(); 
    public QuestSlot() {}
    public QuestSlot(int slotClass)
    {
        questProceeds = new QuestProceed[questDB.questData.Length];
        for (int i = 0; i < questDB.questData.Length; i++)
            questProceeds[i] = new QuestProceed();
        questProceeds[0].state = QuestState.Clear;

        outbreakIdx = 0;
        outbreakProceed = new QuestProceed();
    }

    public void QuestUpdate(QuestType type, int objectIdx, int amt)
    {
        //정식 퀘스트
        for (int i = 0; i < questProceeds.Length; i++)
        {
            if (questProceeds[i].state == QuestState.Proceeding && questDB.questData[i].type == type)
            {
                if(questDB.questData[i].type == QuestType.Level)
                    questProceeds[i].objectCurr = GameManager.slotData.lvl;
                //타입, 대상 인덱스가 같은 값 증가
                else if(questDB.questData[i].objectIdx == 0 || questDB.questData[i].objectIdx == objectIdx)
                    questProceeds[i].objectCurr++;
                   
                //클리어 처리
                if (questProceeds[i].objectCurr == questDB.questData[i].objectAmt) 
                    questProceeds[i].state = QuestState.CanClear;
            }
        }

        //돌발 퀘스트
        if(outbreakProceed.state == QuestState.Proceeding && questDB.outbreakData[outbreakIdx].type == type)
        {
            if(questDB.outbreakData[outbreakIdx].type != QuestType.Diehard)
                if(questDB.outbreakData[outbreakIdx].objectIdx == 0 || questDB.outbreakData[outbreakIdx].objectIdx == objectIdx)
                    outbreakProceed.objectCurr++;
                
            //클리어 처리
            if (questDB.outbreakData[outbreakIdx].objectAmt == outbreakProceed.objectCurr) 
                outbreakProceed.state = QuestState.CanClear;
        }
    }
    public void DiehardUpdate(float rate)
    {
        if(questDB.outbreakData[outbreakIdx].type == QuestType.Diehard)
            if(100 * rate < questDB.outbreakData[outbreakIdx].objectAmt)
                RemoveOutbreak();
    }
    public void ClearQuest(int idx)
    {
        questProceeds[idx].objectCurr = 0;
        questProceeds[idx].state = QuestState.Clear;
        GetReward(questDB.questData[idx]);
    }

    public void NewOutbreak(int idx)
    {
        outbreakIdx = idx;
        outbreakProceed.objectCurr = 0;
        outbreakProceed.state = QuestState.Proceeding;
    }   
    public void ClearOutbreak()
    {
        GetReward(questDB.outbreakData[outbreakIdx]);
        QuestUpdate(QuestType.Outbreak, 0, 1);
        RemoveOutbreak();
    }
    public void RemoveOutbreak()
    {
        outbreakIdx = 0;
        outbreakProceed.objectCurr = 0;
        outbreakProceed.state = QuestState.NotReceive;
    }
    
    void GetReward(QuestBlueprint qbp)
    {
        for(int i = 0;i < qbp.rewardCount;i++)
            {
                switch((int)qbp.rewardIdx[i])
                {
                    //경험치
                    case 150:
                        GameManager.GetExp(qbp.rewardAmt[i]);
                        break;
                }
            }
    }

    public static string GetQuestScript(bool isOutbreak, int idx) => isOutbreak ? questDB.outbreakData[idx].script : questDB.questData[idx].script;
    public KeyValuePair<QuestBlueprint, int>[] GetCurrQuest()
    {
        KeyValuePair<QuestBlueprint, int>[] currQuest = new KeyValuePair<QuestBlueprint, int>[4];
        int idx = 0;
        for (int i = 0; i < questProceeds.Length && idx < 3; i++)
            if (questProceeds[i].state == QuestState.Proceeding || questProceeds[i].state == QuestState.CanClear)
                currQuest[idx++] = new KeyValuePair<QuestBlueprint, int>(questDB.questData[i], questProceeds[i].objectCurr);
        for (; idx < 3; idx++)
            currQuest[idx++] = new KeyValuePair<QuestBlueprint, int>(null, 0);
        if (outbreakIdx != 0)
            currQuest[3] = new KeyValuePair<QuestBlueprint, int>(questDB.outbreakData[outbreakIdx], outbreakProceed.objectCurr);
        else
            currQuest[3] = new KeyValuePair<QuestBlueprint, int>(null, 0);

        return currQuest;
    }
    
    class QuestDB
    {
        public QuestBlueprint[] questData;
        public QuestBlueprint[] outbreakData;

        public QuestDB()
        {
            questData = new QuestBlueprint[2];
            outbreakData = new QuestBlueprint[2];

            for (int i = 0; i < questData.Length; i++)
                questData[i] = new QuestBlueprint(false, i);
            for (int i = 0; i < outbreakData.Length; i++)
                outbreakData[i] = new QuestBlueprint(true, i);
        }
    }
}

//퀘스트 정보 관리
public class QuestManager : MonoBehaviour
{
    #region Quest Show
    //현재 수행 중인 퀘스트 정보 반환
    public static KeyValuePair<QuestBlueprint, int>[] GetCurrQuest() => GameManager.slotData.questData.GetCurrQuest();
    
    //현재 수행 중인 퀘스트 갯수 반환
    public static int GetCurrQuestCount() => GameManager.slotData.questData.questProceeds.Count(x=>x.state == QuestState.Proceeding || x.state == QuestState.CanClear);
    public static QuestProceed[] GetQuestProceed() => GameManager.slotData.questData.questProceeds;
    #endregion

    #region Quest Progress
    public static void QuestUpdate(QuestType type, int idx, int amt)
    {
        GameManager.slotData.questData.QuestUpdate(type, idx, amt);

        SaveData();
    }
    public static void DiehardUpdate(float rate) => GameManager.slotData.questData.DiehardUpdate(rate);

    //새 퀘스트 받기, 마을 메뉴 또는 던전 돌발퀘 방에서 호출
    public static void NewQuest(bool isOutbreak, int idx)
    {
        if (isOutbreak)
            GameManager.slotData.questData.NewOutbreak(idx);
        else
            GameManager.slotData.questData.questProceeds[idx].state = QuestState.Proceeding;

        SaveData();
    }

    //완료된 퀘스트 클리어
    public static void ClearQuest(int idx)
    {
        GameManager.slotData.questData.ClearQuest(idx);
        SaveData();
    }
    public static void RemoveOutbreak()
    {
        GameManager.slotData.questData.RemoveOutbreak();
        SaveData();
    }
    #endregion

    #region Data
    public static void SaveData() => GameManager.SaveSlotData();
    #endregion
}
