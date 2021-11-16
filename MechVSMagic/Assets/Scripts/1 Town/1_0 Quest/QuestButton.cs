using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class QuestButton : MonoBehaviour
{
    QuestList qList;
    QuestState state;
    int idx;

    [SerializeField] Text text;
    [SerializeField] Button btn;

    public bool Equals(QuestState s, int i)
    {
        return (state == s) && (idx == i);
    }

    public void DataSet(QuestList quest, QuestState s, int i)
    {
        qList = quest;
        state = s;
        idx = i;
        btn.onClick.AddListener(Btn_QuestInteraction);
        if (s == QuestState.NotReceive)
            text.text = string.Concat(QuestSlot.GetQuestScript(false, i), "(수락 가능)");
        else if (s == QuestState.Proceeding)
            text.text = string.Concat(QuestSlot.GetQuestScript(false, i), "(진행 중)");
        else
            text.text = string.Concat(QuestSlot.GetQuestScript(false, i), "(완료 가능)");
    }

    public void SwitchState(QuestState s)
    {
        state = s;
        text.text = string.Concat(QuestSlot.GetQuestScript(false, idx), "(진행 중)");
    }

    public void Btn_QuestInteraction()
    {
        switch(state)
        {
            case QuestState.NotReceive:
                qList.Btn_QuestAccept(idx);
                break;
            case QuestState.Proceeding:
                qList.Btn_QuestProceeding(idx);
                break;
            case QuestState.CanClear:
                qList.Btn_QuestClear(idx);
                break;
        }
    }
}
