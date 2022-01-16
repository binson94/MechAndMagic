using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class QuestPanel : MonoBehaviour
{
    [SerializeField] Text questScript;
    [SerializeField] Text questProceed;

    public void SetQuestProceed(KeyValuePair<string, int[]> value)
    {
        questScript.text = value.Key;
        questProceed.text = string.Concat("(", value.Value[0], "/", value.Value[1], ")");
    }
}
