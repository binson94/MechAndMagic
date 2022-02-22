using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class QuestPanel : MonoBehaviour
{
    [SerializeField] Text questScript;
    [SerializeField] Text questProceed;

    public void SetQuestProceed(KeyValuePair<QuestBlueprint, int> value)
    {
        questScript.text = value.Key.script;
        questProceed.text = string.Concat(value.Value, "/", value.Key.objectAmt);
    }
}
