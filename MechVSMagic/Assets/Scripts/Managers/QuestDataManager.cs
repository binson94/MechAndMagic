using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LitJson;

public enum QuestState
{
    NotReceive, Proceeding, CanClear, Clear
}

[System.Serializable]
public class QuestProceed
{
    public int[] objectCurr;
    public QuestState state;

    public QuestProceed()
    {
        objectCurr = new int[4];
        state = QuestState.NotReceive;
    }
}

[System.Serializable]
public class QuestSlot
{
    //Json Data
    static QuestDatabase questDB = null;

    //User Data
    public QuestProceed[] questProceeds;
    public int outbreakIdx;
    public int outbreakObjectCount;
    public QuestProceed outbreakProceed;
    

    static QuestSlot()
    {
        questDB = new QuestDatabase();
    }
    public QuestSlot()
    {
        questProceeds = new QuestProceed[questDB.questData.Length];
        for (int i = 0; i < questDB.questData.Length; i++)
            questProceeds[i] = new QuestProceed();
        questProceeds[0].state = QuestState.Clear;

        outbreakIdx = 0;
        outbreakObjectCount = 0;
        outbreakProceed = new QuestProceed();
    }

    //적 처치 시 호출
    public void QuestUpdate(QuestType type, int idx, int amt)
    {
        //정식 퀘스트
        for (int i = 0; i < questProceeds.Length; i++)
        {
            if (questProceeds[i].state == QuestState.Proceeding && questDB.questData[i].type == type)
            {
                //타입, 대상 인덱스가 같은 값 증가
                List<int> idxs = questDB.questData[i].GetObjectIdx(idx);
                for (int k = 0; k < idxs.Count; k++)
                    questProceeds[i].objectCurr[idxs[k]] = Mathf.Min(questDB.questData[i].objectAmt[idxs[k]], questProceeds[i].objectCurr[idxs[k]] + amt);
                
                //클리어 여부 검사
                bool isClear = true;
                for (int j = 0; j < questDB.questData[i].objectCount; j++)
                    if(questProceeds[i].objectCurr[j] < questDB.questData[i].objectAmt[j])
                    {
                        isClear = false;
                        break;
                    }

                //클리어 처리
                if (isClear) questProceeds[i].state = QuestState.CanClear;
            }
        }

        //돌발 퀘스트
        if(outbreakProceed.state == QuestState.Proceeding && questDB.outbreakData[outbreakIdx].type == type)
        {
            //타입, 대상 인덱스가 같은 값 증가
            List<int> idxs = questDB.outbreakData[outbreakIdx].GetObjectIdx(idx);
            for (int j = 0; j < idxs.Count; j++)
                outbreakProceed.objectCurr[idxs[j]] = Mathf.Min(questDB.outbreakData[outbreakIdx].objectAmt[idxs[j]], outbreakProceed.objectCurr[idxs[j]] + amt);

            //클리어 여부 검사
            bool isClear = true;
            for (int j = 0; j < questDB.outbreakData[outbreakIdx].objectCount; j++)
                if(outbreakProceed.objectCurr[j] < questDB.outbreakData[outbreakIdx].objectAmt[j])
                {
                    isClear = false;
                    break;
                }

            //클리어 처리
            if (isClear) outbreakProceed.state = QuestState.CanClear;
        }
    }

    public void NewOutbreak(int idx)
    {
        outbreakIdx = idx;
        outbreakObjectCount = questDB.outbreakData[idx].objectCount;
        for (int i = 0; i < outbreakObjectCount; i++)
            outbreakProceed.objectCurr[i] = 0;
        outbreakProceed.state = QuestState.Proceeding;
    }
    
    //던전 클리어 시 호출, outbreak data 없앰
    public void EraseOutbreak()
    {
        outbreakIdx = 0;
    }

    public static int GetQuestCount()
    {
        return questDB.questData.Length;
    }
    public static string GetQuestScript(bool isOutbreak, int idx)
    {
        return isOutbreak ? questDB.outbreakData[idx].script : questDB.questData[idx].script;
    }
    class QuestDatabase
    {
        public QuestData[] questData;
        public QuestData[] outbreakData;

        public QuestDatabase()
        {
            questData = new QuestData[2];
            outbreakData = new QuestData[2];

            for (int i = 0; i < questData.Length; i++)
                questData[i] = new QuestData(false, i);
            for (int i = 0; i < outbreakData.Length; i++)
                outbreakData[i] = new QuestData(true, i);
        }
    }
}

public class QuestDataManager : MonoBehaviour
{
    public static QuestDataManager instance = null;
    static QuestSlot[] questSlot = new QuestSlot[2];

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
            Destroy(gameObject);
    }

    #region Quest Show
    //-1 : 보여주지 않음, 0 : 수락할 수 있게 보여줌, 1, 진행 중 상태 보여줌 2 : 완료할 수 있게 보여줌
    public static int IsCanShow(int idx)
    {
        //if(char.lvl >= questDatabase.questData[idx].lvl && chapter >= questDatabase.questData[idx].chapter)
        //  return -1;

        if (questSlot[GameManager.currSlot].questProceeds[idx].state == QuestState.NotReceive)
            return 0;
        else if (questSlot[GameManager.currSlot].questProceeds[idx].state == QuestState.Proceeding)
            return 1;
        else if (questSlot[GameManager.currSlot].questProceeds[idx].state == QuestState.CanClear)
            return 2;
        else
            return -1;
    }
    #endregion

    #region Quest Progress
    //적 처치 시 호출
    public static void QuestUpdate(QuestType type, int idx, int amt)
    {
        questSlot[GameManager.currSlot].QuestUpdate(type, idx, amt);

        SaveData();
    }

    //새 퀘스트 받기, 마을 메뉴 또는 던전 돌발퀘 방에서 호출
    public static void NewQuest(bool isOutbreak, int idx)
    {
        if (isOutbreak)
            questSlot[GameManager.currSlot].NewOutbreak(idx);
        else
        {
            questSlot[GameManager.currSlot].questProceeds[idx].state = QuestState.Proceeding;
        }

        SaveData();
    }

    //완료된 퀘스트 클리어
    public static void ClearQuest(int idx)
    {
        SaveData();
    }
    #endregion

    #region Data
    public static void SaveData()
    {
        PlayerPrefs.SetString(string.Concat("QuestData", GameManager.currSlot), JsonMapper.ToJson(questSlot[GameManager.currSlot]));
    }

    public static void LoadData()
    {
        if (questSlot[GameManager.currSlot] == null)
            questSlot[GameManager.currSlot] = new QuestSlot();

        if (PlayerPrefs.HasKey(string.Concat("QuestData", GameManager.currSlot)))
            questSlot[GameManager.currSlot] = JsonMapper.ToObject<QuestSlot>(PlayerPrefs.GetString(string.Concat("QuestData", GameManager.currSlot)));
    }
    #endregion

    public static void Debug_QuestClean()
    {
        PlayerPrefs.DeleteKey(string.Concat("QuestData", GameManager.currSlot));
        questSlot[GameManager.currSlot] = null;
        UnityEngine.SceneManagement.SceneManager.LoadScene("1 Town");
    }
}
