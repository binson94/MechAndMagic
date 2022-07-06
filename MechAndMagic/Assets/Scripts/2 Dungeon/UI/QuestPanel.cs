using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class QuestPanel : MonoBehaviour
{
    [SerializeField] Text questScript;
    [SerializeField] Text questProceed;

    public void SetQuestProceed(KeyValuePair<QuestBlueprint, int> proceed)
    {
        questScript.text = proceed.Key.script;
        questProceed.text = $"{proceed.Value}/{proceed.Key.objectAmt}";
    }
}
