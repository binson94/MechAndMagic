using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Buff
{
    public string buffName;

    //턴 지속 버프 일 경우, 남은 턴 수
    //스킬 내 버프일 경우, 1이면 모든 적중 시도 간, 0이면 다음 적중 시도에만
    public int duration;

    //버프가 들어갈 대상 스텟의 index
    public int objectIdx;

    //버프 계산 시 계수로 쓰일 스텟의 index
    public int statIdx;

    public bool isMulti;    //true : 곱연산, false : 합연산
    public float buffRate;

    public Buff(string _name = "", int du = 0, int obj = 0, int stat = 0, int ismul = 0, float rate = 0)
    {
        buffName = _name;
        objectIdx = obj;
        duration = du;
        statIdx = stat;
        isMulti = (ismul == 1) ? true : false;
        buffRate = rate;
    }
}
