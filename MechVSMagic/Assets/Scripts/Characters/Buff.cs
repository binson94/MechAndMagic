using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Buff
{
    public string name;

    //턴 지속 버프 일 경우, 남은 턴 수
    //스킬 내 버프일 경우, 1이면 모든 적중 시도 간, 0이면 다음 적중 시도에만
    public int duration;

    //버프가 들어갈 대상 스텟의 index
    public int objectIdx;

    public bool isMulti;    //true : 곱연산, false : 합연산
    public float buffRate;

    public bool isDispel;
    public bool isVisible;

    public Buff(string _name = "", int du = 0, int obj = 0, float stat = 0, int ismul = 0, float rate = 0, int dispel = 0, int visible = 0)
    {
        name = _name;
        objectIdx = obj;
        duration = du == 0 ? 99 : du;
        //statIdx = stat;
        isMulti = ismul == 1;
        buffRate = stat * rate;

        isDispel = dispel == 1;
        isVisible = visible == 1;
    }
}

public class APBuff
{
    public string name;

    public int category;
    public float rate;
    public bool isMulti;

    public bool isTurn;
    public int duration;

    public APBuff(string _name = "", int duration = 0, int category = 0, float rate = 0, bool mul = false, bool turn = true)
    {
        name = _name;
        this.category = category;
        this.rate = rate;
        isMulti = mul;
        isTurn = turn;
    }
}

public class GuardBuff
{
    public string name;

    //버프 횟수
    public int buffTime;

    //반격 여부
    public bool isReturn;

    public int objectIdx;
    public float rate;
    public bool isMulti;

    public GuardBuff(string name = "", int time = 0, bool ret = false, int obj = 0, float rate = 0, bool mul = false)
    {
        this.name = name;
        buffTime = time;
        isReturn = ret;
        objectIdx = obj;
        this.rate = rate;
        isMulti = mul;
    }
}