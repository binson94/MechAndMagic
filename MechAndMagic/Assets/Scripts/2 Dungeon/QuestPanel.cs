using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class QuestPanel : MonoBehaviour
{
    [SerializeField] Text questScript;
    [SerializeField] Text questProceed;

    public void SetQuestProceed(KeyValuePair<QuestData, int[]> value)
    {
        questScript.text = value.Key.script;
        questProceed.text = string.Empty;

        for(int i =0;i<value.Key.objectCount;i++)
            questProceed.text = string.Concat(questProceed.text, value.Value[i], "/", value.Key.objectAmt[i], "\n");
    }
}
