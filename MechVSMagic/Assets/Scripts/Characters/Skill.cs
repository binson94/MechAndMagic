using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Skill
{
    [Header("Basic")]
    public int skillIdx;
    public int skillClass;

    public string skillName;
    public string skillScript;

    public int skillCategory;

    public int skillUsetype;    //스킬 타입(0 : 액티브, 1 : 패시브)

    [Header("Require")]
    //습득에 필요한 요구사항
    public int skillReqlvl;                     //습득 요구 레벨
    public int[] skillReqskills = new int[5];   //습득 선행 스킬

    public int skillAPCost;     //AP 소모량
    public int skillCooldown;   //스킬 쿨다운 턴

    public int skillCombo;      //스킬 적중 시도 횟수

    [Header("Effects")]
    public int[] skillEffectType = new int[5];
    public int[] skillEffectCond = new int[5];
    public int[] skillEffectTarget = new int[5];
    public int[] skillEffectObject = new int[5];
    public int[] skillEffectStat = new int[5];
    public float[] skillEffectRate = new float[5];
    public int[] skillEffectCalc = new int[5];
    public int[] skillEffectTurn = new int[5];
    public int[] skillEffectDispel = new int[5];

    public void SkillLoad()
    {

    }
}
