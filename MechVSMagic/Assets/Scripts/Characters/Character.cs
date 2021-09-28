using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Character : MonoBehaviour
{
    //체력
    public int HP;
    public int currHP;

    //레벨
    public int LVL;

    public int SPD;
    public int ATK;
    public int DEF;

    //행동력
    public int AP;
    public int currAP;

    //타겟이 사망할 시 1 반환
    public bool Attack (Character target)
    {
        if(target)
        {
            Debug.Log(string.Concat(name, " Attacks ", target.name));
            target.currHP -= ATK;

            if (target.currHP <= 0)
                return true;
        }

        return false;
    }

    virtual public bool CastSkill(Character target, int idx)
    {
        currAP -= 2;

        Debug.Log(string.Concat("skill cast : ", idx, ", ", name, " Attacks ", target.name));

        target.currHP -= ATK;

        if (target.currHP <= 0)
            return true;

        return false;
    }
}
