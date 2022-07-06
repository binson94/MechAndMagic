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
    Active_RemoveBuff, Active_RemoveDebuff,
    DoNothing, CharSpecial1, CharSpecial2, CharSpecial3
}

public class Skill
{
    [Header("Basic")]
    ///<summary> 스킬 idx </summary>
    public int idx;
    ///<summary> 스킬 이름 </summary>
    public string name;
    ///<summary> 스킬 설명 </summary>
    public string script;
    ///<summary> 긍정적 설명 </summary>
    public string posScript;
    ///<summary> 부정적 설명 </summary>
    public string negScript;

    ///<summary> 스킬 아이콘 </summary>
    public int icon;

    ///<summary> 스킬 카테고리 </summary>
    public int category;
    ///<summary> 스킬 타입(0 : 액티브, 1 : 패시브) </summary>
    public int useType;

    [Header("Require")] //습득에 필요한 요구사항
    ///<summary> 스킬 습득을 위한 요구 레벨 </summary>
    public int reqLvl;
    ///<summary> 스킬 습득을 위한 선행스킬들, 없으면 0임 </summar>
    public int[] reqskills = new int[5];

    ///<summary> 스킬 AP 소모량 </summary>
    public int apCost;     //AP 소모량
    ///<summary> 스킬 쿨다운(턴 수) </summary>
    public int cooldown;

    [Header("Target Select")]
    ///<summary> 타겟 선택 여부(0 : x, 1 : 선택) </summary>
    public int targetSelect;

    //밑의 두 개는 TargetSelect가 1이어야 유효
    ///<summary> 타겟 선택 시 소속(0 : 아군, 1 : 적군, 2 : 소환수, 3 : 미구분) </summary>
    public int targetSide;
    ///<summary> 타겟 선택 수(1 ~ 4) </summary>
    public int targetCount;

    [Header("Effects")]
    ///<summary> 스킬 효과 수 </summary>
    public int effectCount;
    ///<summary> 각 효과의 종류, SkillType 나열형 참고 </summary>
    public int[] effectType;
    ///<summary> 스킬 효과 발동을 위한 조건 </summary>
    public int[] effectCond;
    ///<summary> 스킬 효과 대상 </summary>
    public int[] effectTarget;

    ///<summary> 효과가 영향 주는 대상 스텟 </summary>
    public int[] effectObject;
    ///<summary> 효과의 계수 스텟 </summary>
    public int[] effectStat;
    ///<summary> 효과의 비율(계수 스텟에 곱하여 계산) </summary>
    public float[] effectRate;

    ///<summary> 스킬 곱연산 여부(0 합, 1 곱) </summary>
    public int[] effectCalc;
    ///<summary> 스킬이 창출하는 버프의 지속 시간 </summary>
    public int[] effectTurn;
    ///<summary> 스킬이 창출하는 버프의 디스펠 가능 여부(0 불가, 1 가능) </summary>
    public int[] effectDispel;     //버프의 디스펠 여부
    ///<summary> 스킬이 창출하는 버프의 표시 여부(0 표기 안함, 1 표기) </summary>
    public int[] effectVisible;

    ///<summary> 효과 로드 전 공간 할당 </summary>
    public void DataAssign(int count)
    {
        effectCount = count;
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
