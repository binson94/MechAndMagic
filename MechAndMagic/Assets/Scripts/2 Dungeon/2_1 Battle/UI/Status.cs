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
        nameTxt.text = u.classIdx < 9 ? "플레이어" : "몬스터";
        lvlTxt.text = u.LVL.ToString();
    }

    public void UpdateValue(Unit u)
    {
        int curr = Mathf.Max(0, u.buffStat[(int)Obj.currHP]);
        hpBar.value = (float)curr / u.buffStat[(int)Obj.HP];
        hpTxt.text = curr.ToString();
    }
}
