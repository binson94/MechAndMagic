using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum SkillType
{
    //액티브 - 스킬 시전 시 계산
    Damage = 1, Heal,
    Active_Buff, Active_Debuff,

    Passive_HasSkillBuff, Passive_HasSkillDebuff,

    Passive_CastBuff, Passive_CastDebuff,
    Passive_EternalBuff, Passive_EternalDebuff,
    Passive_CritHitBuff, Passive_CritHitDebuff,
    Passive_APBuff,
    DoNothing, CharSpecial1, CharSpecial2, CharSpecial3
}

[System.Serializable]
public class Skill
{
    [Header("Basic")]
    public int idx;
    public int useclass;
    public string name;
    public string script;

    public int category;
    
    public int useType;    //스킬 타입(0 : 액티브, 1 : 패시브)

    [Header("Require")]
    //습득에 필요한 요구사항
    public int reqLvl;                     //습득 요구 레벨
    public int[] reqskills = new int[5];   //습득 선행 스킬

    public int apCost;     //AP 소모량
    public int cooldown;   //스킬 쿨다운 턴

    [Header("Target Select")]
    public int targetSelect;       //타겟 선택 여부(0 : x, 1 : 선택)

    //밑의 두 개는 TargetSelect가 1이어야 유효
    public int targetSide;         //타겟 선택 시 소속(0 : 아군, 1 : 적군, 2 : 소환수, 3 : 미구분)
    public int targetCount;        //타겟 선택 수(1 ~ 4)

    public int combo;      //스킬 적중 시도 횟수

    [Header("Effects")]
    public int effectCount;
    public int[] effectType;
    public int[] effectCond;
    public int[] effectTarget;     //대상 캐릭터

    public int[] effectObject;     //대상 스텟
    public int[] effectStat;       //계수 스텟
    public float[] effectRate;     //비율     -> skillEffectObject + or * (skillEffectStat * skillEffectRate)
    public int[] effectCalc;       //+ or *
    public int[] effectTurn;       //버프의 지속시간
    public int[] effectDispel;     //버프의 디스펠 여부
    public int[] effectVisible;

    public void DataAssign()
    {
        effectType = new int[effectCount];
        effectCond = new int[effectCount];
        effectTarget = new int[effectCount];
        effectObject = new int[effectCount];
        effectStat = new int[effectCount];
        effectRate = new float[effectCount];
        effectCalc = new int[effectCount];
        effectTurn = new int[effectCount];
        effectDispel = new int[effectCount];
        effectVisible = new int[effectCount];
    }
}
