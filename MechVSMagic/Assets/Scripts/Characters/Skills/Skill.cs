using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum SkillType {
                        //스킬 시전 시 계산
                        Damage = 1,
                        BuffNxtATK, DebuffNxtATK, BuffCurrSkill, DebuffCurrSkill,               // 버프 or 디버프
                        Acc_BuffNxtATK, Acc_DebffNxtATK, Acc_BuffCurrSkill, Acc_DebuffCurrSkill,// 적중 시 버프 or 디버프(다음 적중, 스킬 내)
                        Acc_BuffTurn, Acc_DebuffTurn, Acc_Buff, Acc_Debuff,                     // 적중 시 버프 or 디버프(일정 턴, 반영구)

                        //스킬 시전 시, 특정 스킬을 장착하고 있는 경우 계산
                        Cast_HasSkillBuffNxtATK, Cast_HasSkillDebuffNxtATK,
                        Cast_HasSkillBuffCurrSkill, Cast_HasSkillDebuffCurrSkill,
                        Cast_HasSkillBuff, Cast_HasSkillDebuff,                 

                        //전투 돌입 시 계산
                        Battle_Buff, Battle_Debuff, Battle_BuffTurn, Battle_DebuffTurn,

                        //전투 돌입 시, 특정 스킬 장착 시
                        Battle_HasSkillBuffTurn, Battle_HasSkillDebuffTurn,
                        Battle_HasSkillBuff, Battle_HasSkillDebuff, 

                        //패시브 - 전투 중 스킬 사용 시 계산
                        Passive_SkillBuffCurrSkill, Passive_SkillDebuffCurrSkill,
                        Passive_SkillBuffTurn, Passive_SkillDebuffTurn,
                        Passive_SkillBuff, Passive_SkillDebuff,

                        //패시브 - 던전 입장 시 계산
                        Passive_EternalBuff, Passive_EternalDebuff,

                        //크리티컬 시 계산
                        Crit_BuffNxtATK, Crit_DebuffNxtATK, Crit_BuffCurrSkill, Crit_DebuffCurrSkill,
                        Crit_BuffTurn, Crit_DebuffTurn, Crit_Buff, Crit_Debuff
                        };

[System.Serializable]
public class Skill
{
    [Header("Basic")]
    public int idx;
    public int useclass;
    public string skillName;
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
