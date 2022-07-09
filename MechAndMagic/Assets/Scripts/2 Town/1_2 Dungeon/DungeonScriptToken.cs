using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DungeonScriptToken : MonoBehaviour
{
    [SerializeField] Text scriptTxt;
    [SerializeField] Text rewardTxt;

    public void SetData(string script, string reward)
    {
        scriptTxt.text = script;
        rewardTxt.text = reward;
    }
}
