using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Status : MonoBehaviour
{
    [SerializeField] Text nameTxt;
    [SerializeField] Text lvlTxt;

    [SerializeField] Slider hpBar;
    [SerializeField] Text hpTxt;

    public void SetName(Unit u)
    {
        nameTxt.text = u.name;
        lvlTxt.text = $"{u.LVL}";
    }

    public void UpdateValue(Unit u)
    {
        int curr = Mathf.Max(0, u.buffStat[(int)Obj.currHP]);
        hpBar.value = (float)curr / u.buffStat[(int)Obj.HP];
        hpTxt.text = curr.ToString();
    }
}
