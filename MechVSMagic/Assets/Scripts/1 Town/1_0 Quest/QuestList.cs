using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuestList : MonoBehaviour
{
    //0 : canReceive, 
    public List<QuestButton> btnsList = new List<QuestButton>();

    [SerializeField] GameObject btnPrefab;
    List<QuestButton> btnPool = new List<QuestButton>();

    [SerializeField] Transform content;
    [SerializeField] Transform poolParent;

    private void Start()
    {
        //버튼 풀 제작
        for (int i = 0; i < 3; i++)
        {
            btnPool.Add(Instantiate(btnPrefab).GetComponent<QuestButton>());
            btnPool[btnPool.Count - 1].transform.parent = poolParent;
            btnPool[btnPool.Count - 1].gameObject.SetActive(false);
        }

        QuestDataManager.LoadData();
        int count = QuestSlot.GetQuestCount();
        for (int i = 0; i < count; i++)
        {
            int state = QuestDataManager.IsCanShow(i);
            if (state != -1)
                btnsList.Add(GetNewButton((QuestState)state, i));
        }

    }

    public void Btn_QuestAccept(int idx)
    {
        QuestDataManager.NewQuest(false, idx);
        SwitchButtonState(QuestState.NotReceive, QuestState.Proceeding, idx);
    }

    public void Btn_QuestProceeding(int idx)
    {
        Debug.Log(string.Concat(idx, " is proceeding"));
    }

    public void Btn_QuestClear(int idx)
    {
        QuestDataManager.ClearQuest(idx);
        RemoveBtn(QuestState.CanClear, idx);
    }

    public void Btn_Debug_QuestClean()
    {
        QuestDataManager.Debug_QuestClean();
    }

    QuestButton GetNewButton(QuestState state, int questIdx)
    {
        QuestButton tmp;
        if (btnPool.Count > 0)
        {
            tmp = btnPool[0];
            btnPool.RemoveAt(0);
        }
        else
            tmp = Instantiate(btnPrefab).GetComponent<QuestButton>();

        tmp.DataSet(this, state, questIdx);
        tmp.transform.parent = content;
        tmp.gameObject.SetActive(true);

        return tmp;
    }
    void RemoveBtn(QuestState state, int questIdx)
    {
        for (int i = 0; i < btnsList.Count; i++)
            if (btnsList[i].Equals(state, questIdx))
            {
                QuestButton tmp = btnsList[i];
                btnsList.RemoveAt(i);
                tmp.transform.parent = poolParent;
                tmp.gameObject.SetActive(false);
                break;
            }
    }
    void SwitchButtonState(QuestState before, QuestState after, int questIdx)
    {
        for (int i = 0; i < btnsList.Count; i++)
            if (btnsList[i].Equals(before, questIdx))
            {
                btnsList[i].SwitchState(after);
                break;
            }
    }
}
