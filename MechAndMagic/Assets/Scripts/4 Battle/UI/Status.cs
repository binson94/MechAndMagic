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

    ///<summary> 전투 시작 시 호출, 이름, 레벨 설정 </summary>
    public void StatusInit(Unit u)
    {
        nameTxt.text = u.name;
        lvlTxt.text = $"{u.LVL}";
    }

    ///<summary> 전투 중 호출, 체력 슬라이더 설정, 버프 설정 </summary>
    public void StatusUpdate(Unit u)
    {
        int curr = Mathf.Max(0, u.buffStat[(int)Obj.currHP]);
        hpBar.value = (float)curr / u.buffStat[(int)Obj.HP];
        hpTxt.text = curr.ToString();
    }
}
