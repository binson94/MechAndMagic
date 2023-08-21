using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class QuestInfoToken : MonoBehaviour
{
    [SerializeField] Text questNameTxt;
    [SerializeField] Text questScriptTxt;

    public void SetQuestProceed(KeyValuePair<QuestBlueprint, int> proceed)
    {
        if(proceed.Key == null)
            questNameTxt.text = questScriptTxt.text = string.Empty;
        else
        {
            questNameTxt.text = proceed.Key.name;
            if(proceed.Key.type == QuestType.Diehard_Over || proceed.Key.type == QuestType.Diehard_Under)
            {
                questScriptTxt.text = $"{proceed.Key.script}";
                if(GameManager.Instance.slotData.questData.outbreakProceed.state == QuestState.Fail)
                    questScriptTxt.text += "<color=#ed2929> (실패)</color>";
            }
            else
                questScriptTxt.text = $"{proceed.Key.script} ({proceed.Value}/{proceed.Key.objectAmt})";
        }
    }
}
