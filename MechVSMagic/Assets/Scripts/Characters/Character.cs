using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//스텟 index - 12가지
public enum StatName { currHP = 1, HP, currAP, AP, ATK, DEF, ACC, DOG, CRC, CRB, PEN, SPD };

[System.Serializable]
public struct Stat
{
    public string statname;
    public int value;

    public static int operator +(Stat a, int b)
    {
        return a.value + b;
    }
    public static int operator +(Stat a, Stat b)
    {
        return a.value + b.value;
    }
    public static int operator -(Stat a, int b)
    {
        return a.value - b;
    }
    public static int operator -(int a, Stat b)
    {
        return a - b.value;
    }
    public static int operator -(Stat a, Stat b)
    {
        return a.value - b.value;
    }
    public static int operator *(Stat a, int b)
    {
        return a.value * b;
    }
    public static int operator *(Stat a, Stat b)
    {
        return a.value * b.value;
    }
    public static float operator *(Stat a, float b)
    {
        return a.value * b;
    }
    public static float operator /(Stat a, float b)
    {
        return a.value / b;
    }
    public static float operator /(Stat a, Stat b)
    {
        return (float)a.value / b.value;
    }
    public static bool operator < (int a, Stat b)
    {
        return a < b.value;
    }
    public static bool operator < (Stat a, int b)
    {
        return a.value < b;
    }
    public static bool operator < (Stat a, Stat b)
    {
        return a.value < b.value;
    }
    public static bool operator > (int a, Stat b)
    {
        return a > b.value;
    }
    public static bool operator > (Stat a, int b)
    {
        return a.value > b;
    }
    public static bool operator > (Stat a, Stat b)
    {
        return a.value > b.value;
    }
    public static bool operator <=(Stat a, int b)
    {
        return a.value <= b;
    }
    public static bool operator >=(Stat a, int b)
    {
        return a.value >= b;
    }
}


public class Character : MonoBehaviour
{
    [Header("Stats")]
    //레벨
    public int LVL;
    public int classIdx;
    //캐릭터 기본 스텟, 레벨만 따름
    [SerializeField] protected Stat[] basicStat = new Stat[13];
    //던전 입장 시 스텟 - 아이템 및 영구 적용 버프
    [SerializeField] protected Stat[] dungeonStat = new Stat[13];
    //유동적으로 변하는 스텟 - 최종(모든 버프 적용)
    public Stat[] buffStat = new Stat[13];

    [Header("Skills")]
    public int[] activeSkills = new int[5];     //스킬 인덱스만 저장, 스킬 데이터를 DB에서 불러옴
    public int[] cooldowns = new int[5];
    public int[] passiveSkills = new int[3];    //스킬 인덱스만 저장, 스킬 데이터를 DB에서 불러옴

    [Header("Buffs")]
    //영구 적용 버프, 버프 해제 먹지 않음, 던전 입장 시 계산
    public List<Buff> eternalBuffList = new List<Buff>();
    public List<Buff> eternalDebuffList = new List<Buff>();

    //전투 동안 적용 버프(턴 제한), 버프 해제 먹음, 전투 시작, 전투 중에 계산
    public List<Buff> inbattleBuffList = new List<Buff>();
    public List<Buff> inbattleDebuffList = new List<Buff>();

    //한 스킬 내에서만 적용되는 버프, 스킬 사용 중에 계산
    public List<Buff> inskillBuffList = new List<Buff>();
    public List<Buff> inskillDebuffList = new List<Buff>();

    [Header("Battle")]
    protected bool isAcc;     //적중 여부
    protected bool isCrit;    //크리티컬 여부

    void Start()
    {
        OnDungeonEnter();
    }

    //던전 입장 시 호출 - 영구 적용 버프/디버프 적용 및 
    public void OnDungeonEnter()
    {
        //eternalBuffList에 영구 적용 버프 추가

        //dungeonStat Update : (basicStat + 

        StatLoad();
        for (int i = 0; i < 13; i++)
            buffStat[i].value = dungeonStat[i].value = basicStat[i].value;
    }

    //전투 시작 시 1번만 호출
    public void OnBattleStart()
    {
        BattleStartSkillProcess();
        CalcTurnBuff();

        for (int i = 0; i < 5; i++)
            cooldowns[i] = 0;

        void BattleStartSkillProcess()
        {
            for (int j = 0; j < 5; j++)
            {
                Skill skill = SkillManager.instance.GetSkillData(classIdx, activeSkills[j]);
                if (skill == null)
                    continue;

                for (int i = 0; i < skill.effectCount; i++)
                {
                    switch ((SkillType)skill.effectType[i])
                    {

                        case SkillType.Battle_Buff:
                            {
                                inbattleBuffList.Add(new Buff("", skill.effectTurn[i], skill.effectObject[i], skill.effectStat[i], skill.effectCalc[i], skill.effectRate[i]));
                                break;
                            }
                        case SkillType.Battle_Debuff:
                            {
                                inbattleDebuffList.Add(new Buff("", skill.effectTurn[i], skill.effectObject[i], skill.effectStat[i], skill.effectCalc[i], skill.effectRate[i]));
                                break;
                            }
                        case SkillType.Battle_BuffTurn:
                            {
                                inbattleBuffList.Add(new Buff("", skill.effectTurn[i], skill.effectObject[i], skill.effectStat[i], skill.effectCalc[i], skill.effectRate[i]));
                                break;
                            }
                        case SkillType.Battle_DebuffTurn:
                            {
                                inbattleDebuffList.Add(new Buff("", skill.effectTurn[i], skill.effectObject[i], skill.effectStat[i], skill.effectCalc[i], skill.effectRate[i]));
                                break;
                            }
                        case SkillType.Battle_HasSkillBuffTurn:
                            {
                                if (HasSkill(0))
                                {
                                    inbattleBuffList.Add(new Buff("", skill.effectTurn[i], skill.effectObject[i], skill.effectStat[i], skill.effectCalc[i], skill.effectRate[i]));
                                }
                                break;
                            }
                        case SkillType.Battle_HasSkillDebuffTurn:
                            if (HasSkill(0))
                            {
                                inbattleDebuffList.Add(new Buff("", skill.effectTurn[i], skill.effectObject[i], skill.effectStat[i], skill.effectCalc[i], skill.effectRate[i]));
                            }
                            break;
                        case SkillType.Battle_HasSkillBuff:
                            if (HasSkill(0))
                            {
                                inbattleDebuffList.Add(new Buff("", skill.effectTurn[i], skill.effectObject[i], skill.effectStat[i], skill.effectCalc[i], skill.effectRate[i]));
                            }
                            break;
                        case SkillType.Battle_HasSkillDebuff:
                            if (HasSkill(0))
                            {
                                inbattleDebuffList.Add(new Buff("", skill.effectTurn[i], skill.effectObject[i], skill.effectStat[i], skill.effectCalc[i], skill.effectRate[i]));
                            }
                            break;
                        default:
                            break;
                    }
                }
            }
            for(int j = 0;j<3;j++)
            {
                Skill skill = SkillManager.instance.GetSkillData(classIdx, passiveSkills[j]);
                if (skill == null)
                    continue;

                for (int i = 0; i < skill.effectCount; i++)
                {
                    switch ((SkillType)skill.effectType[i])
                    {

                        case SkillType.Battle_Buff:
                            {
                                inbattleBuffList.Add(new Buff("", skill.effectTurn[i], skill.effectObject[i], skill.effectStat[i], skill.effectCalc[i], skill.effectRate[i]));
                                break;
                            }
                        case SkillType.Battle_Debuff:
                            {
                                inbattleDebuffList.Add(new Buff("", skill.effectTurn[i], skill.effectObject[i], skill.effectStat[i], skill.effectCalc[i], skill.effectRate[i]));
                                break;
                            }
                        case SkillType.Battle_BuffTurn:
                            {
                                inbattleBuffList.Add(new Buff("", skill.effectTurn[i], skill.effectObject[i], skill.effectStat[i], skill.effectCalc[i], skill.effectRate[i]));
                                break;
                            }
                        case SkillType.Battle_DebuffTurn:
                            {
                                inbattleDebuffList.Add(new Buff("", skill.effectTurn[i], skill.effectObject[i], skill.effectStat[i], skill.effectCalc[i], skill.effectRate[i]));
                                break;
                            }
                        case SkillType.Battle_HasSkillBuffTurn:
                            {
                                if (HasSkill(0))
                                {
                                    inbattleBuffList.Add(new Buff("", skill.effectTurn[i], skill.effectObject[i], skill.effectStat[i], skill.effectCalc[i], skill.effectRate[i]));
                                }
                                break;
                            }
                        case SkillType.Battle_HasSkillDebuffTurn:
                            if (HasSkill(0))
                            {
                                inbattleDebuffList.Add(new Buff("", skill.effectTurn[i], skill.effectObject[i], skill.effectStat[i], skill.effectCalc[i], skill.effectRate[i]));
                            }
                            break;
                        case SkillType.Battle_HasSkillBuff:
                            if (HasSkill(0))
                            {
                                inbattleDebuffList.Add(new Buff("", skill.effectTurn[i], skill.effectObject[i], skill.effectStat[i], skill.effectCalc[i], skill.effectRate[i]));
                            }
                            break;
                        case SkillType.Battle_HasSkillDebuff:
                            if (HasSkill(0))
                            {
                                inbattleDebuffList.Add(new Buff("", skill.effectTurn[i], skill.effectObject[i], skill.effectStat[i], skill.effectCalc[i], skill.effectRate[i]));
                            }
                            break;
                        default:
                            break;
                    }
                }
            }
            

            bool HasSkill(int skillIdx)
            {
                for (int i = 0; i < 5; i++)
                    if (activeSkills[i] == skillIdx)
                        return true;
                for (int i = 0; i < 3; i++)
                    if (passiveSkills[i] == skillIdx)
                        return true;
                return false;
            }
        }
    }

    //내 턴이 시작될 때 호출 - AP 값 초기화, 스킬 쿨다운 감소, 턴 버프 계산
    public void OnTurnStart()
    {
        buffStat[(int)StatName.currAP].value = buffStat[(int)StatName.AP].value;

        for (int i = 0; i < 5; i++)
        {
            cooldowns[i]--;
            if (cooldowns[i] < 0)
                cooldowns[i] = 0;
        }

        //버프 지속시간 최신화
        TurnBuffUpdate();
       
        CalcTurnBuff();

        void TurnBuffUpdate()
        {
            for (int i = 0; i < inbattleBuffList.Count; i++)
            {
                Buff buff = inbattleBuffList[i];
                if (inbattleBuffList[i].duration == 99)
                    continue;
                inbattleBuffList[i].duration--;

                if (inbattleBuffList[i].duration < 1)
                {
                    inbattleBuffList.RemoveAt(i);
                    i--;
                }
            }
            for (int i = 0; i < inbattleDebuffList.Count; i++)
            {
                Buff buff = inbattleDebuffList[i];
                if (inbattleDebuffList[i].duration == 99)
                    continue;
                inbattleBuffList[i].duration--;

                if (inbattleBuffList[i].duration < 1)
                {
                    inbattleBuffList.RemoveAt(i);
                    i--;
                }
            }
        }
    }

    #region Skill
    //랜덤 타겟 or 전체 타겟 스킬
    public virtual void CastSkill(int idx)
    {

    }

    //타겟 지정 스킬
    public virtual void CastSkill(Character target, int idx)
    {
        //적중 성공 여부
        isAcc = false;
        //크리티컬 성공 여부
        isCrit = false;


        //skillDB에서 스킬 불러오기
        Skill skill = SkillManager.instance.GetSkillData(classIdx, activeSkills[idx]);
        inskillBuffList.Clear();
        if (skill == null)
        {
            Debug.LogError("skill is null");
            return;
        }

        SkillPassiveBuff(skill.category);

        //skill 효과 순차적으로 계산
        SkillEffectProcess(skill, target);
        
        buffStat[(int)StatName.currAP].value = buffStat[(int)StatName.currAP].value - skill.apCost;
        cooldowns[idx] = skill.cooldown;
    }

    void SkillEffectProcess(Skill skill, Character target)
    {
        for (int i = 0; i < skill.effectCount; i++)
        {
            Character effectTarget = GetEffectTarget(skill, i, target);
            switch ((SkillType)skill.effectType[i])
            {
                //데미지 - 스킬 버프 계산 후 
                case SkillType.Damage:
                    {
                        CalcSkillBuff(skill.category);

                        //skillEffectRate가 기본적으로 음수
                        int dmg = Mathf.CeilToInt(buffStat[skill.effectStat[i]] * skill.effectRate[i]);

                        //아군인 경우 - 회복 스킬
                        if (IsAllie(effectTarget))
                        {
                            effectTarget.buffStat[(int)StatName.currHP].value += dmg;
                        }
                        //적군인 경우 - 피해 스킬
                        else
                        {
                            //명중 연산 - 최소 명중률 10%
                            int acc = Mathf.Max(buffStat[(int)StatName.ACC] - effectTarget.buffStat[(int)StatName.DOG], 10);
                            //명중 시
                            if (Random.Range(0, 100) < acc)
                            {
                                isAcc = true;
                                //크리티컬 연산 - dmg * CRB
                                if (Random.Range(0, 100) < buffStat[(int)StatName.CRC])
                                {
                                    isCrit = true;
                                    dmg = Mathf.CeilToInt(dmg * (buffStat[(int)StatName.CRB] / 100f));
                                }
                                else
                                    isCrit = false;

                                int finalDEF = Mathf.Max(0, target.buffStat[(int)StatName.DEF] - buffStat[(int)StatName.PEN]);
                                int finalDmg = Mathf.Min(-1, dmg + finalDEF);

                                effectTarget.buffStat[(int)StatName.currHP].value += finalDmg;

                                Debug.Log(string.Concat(name, "damages ", effectTarget.name, ", ", finalDmg));
                            }
                            else
                            {
                                isAcc = false;
                                Debug.Log("Dodge");
                            }
                        }

                        break;
                    }
                case SkillType.BuffNxtATK:
                    {
                        inskillBuffList.Add(new Buff("", 0, skill.effectObject[i], skill.effectStat[i], skill.effectCalc[i], skill.effectRate[i]));
                        Debug.Log("Buff next Attack");
                        break;
                    }
                case SkillType.DebuffNxtATK:
                    {
                        inskillDebuffList.Add(new Buff("", 0, skill.effectObject[i], skill.effectStat[i], skill.effectCalc[i], skill.effectRate[i]));
                        Debug.Log("Debuff next Attack");
                        break;
                    }
                case SkillType.BuffCurrSkill:
                    {
                        inskillBuffList.Add(new Buff("", 1, skill.effectObject[i], skill.effectStat[i], skill.effectCalc[i], skill.effectRate[i]));
                        Debug.Log("Buff inskill");
                        break;
                    }
                case SkillType.DebuffCurrSkill:
                    {
                        inskillDebuffList.Add(new Buff("", 1, skill.effectObject[i], skill.effectStat[i], skill.effectCalc[i], skill.effectRate[i]));
                        Debug.Log("Debuff inskill");
                        break;
                    }
                case SkillType.Acc_BuffNxtATK:
                    {
                        if (isAcc)
                        {
                            inskillBuffList.Add(new Buff("", 0, skill.effectObject[i], skill.effectStat[i], skill.effectCalc[i], skill.effectRate[i]));
                            Debug.Log("Acc_Buff inskill");
                        }
                        break;
                    }
                case SkillType.Acc_DebffNxtATK:
                    {
                        if (isAcc)
                        {
                            inskillDebuffList.Add(new Buff("", 0, skill.effectObject[i], skill.effectStat[i], skill.effectCalc[i], skill.effectRate[i]));
                            Debug.Log("Acc_Debuff next Attack");
                        }
                        break;
                    }
                case SkillType.Acc_BuffCurrSkill:
                    {
                        if (isAcc)
                        {
                            inskillBuffList.Add(new Buff("", 1, skill.effectObject[i], skill.effectStat[i], skill.effectCalc[i], skill.effectRate[i]));
                            Debug.Log("Acc_Buff inskill");
                        }
                        break;
                    }
                case SkillType.Acc_DebuffCurrSkill:
                    {
                        if (isAcc)
                        {
                            inskillDebuffList.Add(new Buff("", 1, skill.effectObject[i], skill.effectStat[i], skill.effectCalc[i], skill.effectRate[i]));
                            Debug.Log("Debuff inskill");
                        }
                        break;
                    }
                case SkillType.Acc_BuffTurn:
                    {
                        if (isAcc)
                        {
                            target.inbattleBuffList.Add(new Buff("", skill.effectTurn[i], skill.effectObject[i], skill.effectStat[i], skill.effectCalc[i], skill.effectRate[i]));
                        }
                        break;
                    }
                case SkillType.Acc_DebuffTurn:
                    {
                        if (isAcc)
                        {
                            target.inbattleDebuffList.Add(new Buff("", skill.effectTurn[i], skill.effectObject[i], skill.effectStat[i], skill.effectCalc[i], skill.effectRate[i]));
                        }
                        break;
                    }
                case SkillType.Acc_Buff:
                    {
                        if (isAcc)
                        {
                            target.inbattleBuffList.Add(new Buff("", skill.effectTurn[i], skill.effectObject[i], skill.effectStat[i], skill.effectCalc[i], skill.effectRate[i]));
                        }
                        break;
                    }
                case SkillType.Acc_Debuff:
                    {
                        if (isAcc)
                        {
                            target.inbattleDebuffList.Add(new Buff("", skill.effectTurn[i], skill.effectObject[i], skill.effectStat[i], skill.effectCalc[i], skill.effectRate[i]));
                        }
                        break;
                    }
                case SkillType.Cast_HasSkillBuffNxtATK:
                    {
                        if (HasSkill(0))
                        {
                            inskillBuffList.Add(new Buff("", 0, skill.effectObject[i], skill.effectStat[i], skill.effectCalc[i], skill.effectRate[i]));
                        }
                        break;
                    }
                case SkillType.Cast_HasSkillDebuffNxtATK:
                    {
                        if (HasSkill(0))
                        {
                            inskillDebuffList.Add(new Buff("", 0, skill.effectObject[i], skill.effectStat[i], skill.effectCalc[i], skill.effectRate[i]));
                        }
                        break;
                    }
                case SkillType.Cast_HasSkillBuffCurrSkill:
                    {
                        if (HasSkill(0))
                        {
                            inskillBuffList.Add(new Buff("", 1, skill.effectObject[i], skill.effectStat[i], skill.effectCalc[i], skill.effectRate[i]));
                        }
                        break;
                    }
                case SkillType.Cast_HasSkillDebuffCurrSkill:
                    {
                        if (HasSkill(0))
                        {
                            inskillDebuffList.Add(new Buff("", 1, skill.effectObject[i], skill.effectStat[i], skill.effectCalc[i], skill.effectRate[i]));
                        }
                        break;
                    }
                case SkillType.Cast_HasSkillBuff:
                    {
                        if (HasSkill(0))
                        {
                            target.inbattleBuffList.Add(new Buff("", skill.effectTurn[i], skill.effectObject[i], skill.effectStat[i], skill.effectCalc[i], skill.effectRate[i]));
                        }
                        break;
                    }
                case SkillType.Cast_HasSkillDebuff:
                    {
                        if (HasSkill(0))
                        {
                            target.inbattleDebuffList.Add(new Buff("", skill.effectTurn[i], skill.effectObject[i], skill.effectStat[i], skill.effectCalc[i], skill.effectRate[i]));
                        }
                        break;
                    }
                case SkillType.Crit_BuffNxtATK:
                    {
                        if (isCrit)
                        {
                            inskillBuffList.Add(new Buff("", 0, skill.effectObject[i], skill.effectStat[i], skill.effectCalc[i], skill.effectRate[i]));
                        }
                        break;
                    }
                case SkillType.Crit_DebuffNxtATK:
                    {
                        if (isCrit)
                        {
                            inskillDebuffList.Add(new Buff("", 0, skill.effectObject[i], skill.effectStat[i], skill.effectCalc[i], skill.effectRate[i]));
                        }
                        break;
                    }
                case SkillType.Crit_BuffCurrSkill:
                    {
                        if (isCrit)
                        {
                            inskillBuffList.Add(new Buff("", 1, skill.effectObject[i], skill.effectStat[i], skill.effectCalc[i], skill.effectRate[i]));
                        }
                        break;
                    }
                case SkillType.Crit_DebuffCurrSkill:
                    {
                        if (isCrit)
                        {
                            inskillDebuffList.Add(new Buff("", 1, skill.effectObject[i], skill.effectStat[i], skill.effectCalc[i], skill.effectRate[i]));
                        }
                        break;
                    }
                case SkillType.Crit_BuffTurn:
                    {
                        if (isCrit)
                        {
                            target.inbattleBuffList.Add(new Buff("", skill.effectTurn[i], skill.effectObject[i], skill.effectStat[i], skill.effectCalc[i], skill.effectRate[i]));
                        }
                        break;
                    }
                case SkillType.Crit_DebuffTurn:
                    {
                        if (isCrit)
                        {
                            target.inbattleDebuffList.Add(new Buff("", skill.effectTurn[i], skill.effectObject[i], skill.effectStat[i], skill.effectCalc[i], skill.effectRate[i]));
                        }
                        break;
                    }
                case SkillType.Crit_Buff:
                    {
                        if (isCrit)
                        {
                            target.inbattleBuffList.Add(new Buff("", skill.effectTurn[i], skill.effectObject[i], skill.effectStat[i], skill.effectCalc[i], skill.effectRate[i]));
                        }
                        break;
                    }
                case SkillType.Crit_Debuff:
                    {
                        if (isCrit)
                        {
                            target.inbattleDebuffList.Add(new Buff("", skill.effectTurn[i], skill.effectObject[i], skill.effectStat[i], skill.effectCalc[i], skill.effectRate[i]));
                        }
                        break;
                    }
                default:
                    break;
            }
        }

        bool IsAllie(Character c)
        {
            if (c.classIdx > 9)
                return false;
            else
                return true;
        }
        bool HasSkill(int skillIdx)
        {
            for (int i = 0; i < 5; i++)
                if (activeSkills[i] == skillIdx)
                    return true;
            for (int i = 0; i < 3; i++)
                if (passiveSkills[i] == skillIdx)
                    return true;
            return false;
        }
    }

    void SkillPassiveBuff(int skillCategory)
    {
        for (int j = 0; j < 3; j++)
        {
            Skill skill = SkillManager.instance.GetSkillData(classIdx, passiveSkills[j]);

            for (int i = 0; i < skill.effectCount; i++)
            {
                if (skillCategory != skill.effectCond[i])
                    continue;

                switch ((SkillType)skill.effectType[i])
                {
                    case SkillType.Passive_SkillBuffCurrSkill:
                        {
                            inskillBuffList.Add(new Buff("", 1, skill.effectObject[i], skill.effectStat[i], skill.effectCalc[i], skill.effectRate[i]));
                            break;
                        }
                    case SkillType.Passive_SkillDebuffCurrSkill:
                        {
                            inskillDebuffList.Add(new Buff("", 1, skill.effectObject[i], skill.effectStat[i], skill.effectCalc[i], skill.effectRate[i]));
                            break;
                        }
                    case SkillType.Passive_SkillBuffTurn:
                        {
                            inbattleBuffList.Add(new Buff("", skill.effectTurn[i], skill.effectObject[i], skill.effectStat[i], skill.effectCalc[i], skill.effectRate[i]));
                            break;
                        }
                    case SkillType.Passive_SkillDebuffTurn:
                        {
                            inbattleDebuffList.Add(new Buff("", skill.effectTurn[i], skill.effectObject[i], skill.effectStat[i], skill.effectCalc[i], skill.effectRate[i]));
                            break;
                        }
                    case SkillType.Passive_SkillBuff:
                        {
                            inbattleBuffList.Add(new Buff("", skill.effectTurn[i], skill.effectObject[i], skill.effectStat[i], skill.effectCalc[i], skill.effectRate[i]));
                            break;
                        }
                    case SkillType.Passive_SkillDebuff:
                        {
                           inbattleDebuffList.Add(new Buff("", skill.effectTurn[i], skill.effectObject[i], skill.effectStat[i], skill.effectCalc[i], skill.effectRate[i]));
                            break;
                        }
                    default:
                        break;
                }
            }
        }
    }

    Character GetEffectTarget(Skill skill, int effectIdx, Character skillTarget)
    {
        switch (skill.effectTarget[effectIdx])
        {
            //나 자신
            case 0:
                return this;
            //스킬 타겟
            case 1:
                return skillTarget;
            //아군 측 랜덤 1개체
            case 2:
                return this;
            //아군 측 전체
            case 3:
                return this;
            //적군 측 랜덤 1개체
            case 4:
            //적군 측 랜덤 2개체
            case 5:
            //적군 측 전체
            case 6:
            //피아 미구분 랜덤 1개체
            case 7:
            //피아 미구분 랜덤 2개체
            case 8:
            //피아 미구분 랜덤 3개체
            case 9:
            //피아 미구분 랜덤 4개체
            case 10:
            //피아 미구분 전체
            case 11:
            default:
                return skillTarget;
        }
    }
    #endregion

    //매 턴 시작 시, 버프 스텟 계산
    void CalcTurnBuff()
    {
        float[] mulPivot = new float[13];
        float[] addPivot = new float[13];

        for (int i = 0; i < 13; i++)
        {
            mulPivot[i] = 1;
            addPivot[i] = 0;
        }

        for (int i = 0; i < inbattleBuffList.Count; i++)
        {
            Buff buff = inbattleBuffList[i];

            if (buff.isMulti)
                mulPivot[buff.objectIdx] += buff.buffRate;
            else
                addPivot[buff.objectIdx] += buff.buffRate;
        }
        for (int i = 0; i < inbattleDebuffList.Count; i++)
        {
            Buff buff = inbattleDebuffList[i];

            if (buff.isMulti)
                mulPivot[buff.objectIdx] += buff.buffRate;
            else
                addPivot[buff.objectIdx] += buff.buffRate;
        }


        for (int i = 0; i < 13; i++)
            buffStat[i].value = Mathf.CeilToInt(dungeonStat[i] * mulPivot[i] + addPivot[i]);
    }

    //공격 시 계산
    void CalcSkillBuff(int skillCategory)
    {
        float[] mulPivot = new float[13];
        float[] addPivot = new float[13];

        for (int i = 0; i < 13; i++)
        {
            mulPivot[i] = 1;
            addPivot[i] = 0;
        }


        for (int i = 0; i < inbattleBuffList.Count; i++)
        {
            Buff buff = inbattleBuffList[i];

            if (buff.isMulti)
                mulPivot[buff.objectIdx] += buff.buffRate;
            else
                addPivot[buff.objectIdx] += buff.buffRate;
        }
        for (int i = 0; i < inbattleDebuffList.Count; i++)
        {
            Buff buff = inbattleDebuffList[i];

            if (buff.isMulti)
                mulPivot[buff.objectIdx] += buff.buffRate;
            else
                addPivot[buff.objectIdx] += buff.buffRate;
        }

        for (int i = 0; i < inskillBuffList.Count; i++)
        {
            Buff buff = inskillBuffList[i];

            if (buff.isMulti)
                mulPivot[buff.objectIdx] += buff.buffRate;
            else
                addPivot[buff.objectIdx] += buff.buffRate;

            //다음 적중 시도만 강화일 경우, buff 제거
            if(buff.duration == 0)
            {
                inskillBuffList.RemoveAt(i);
                i--;
            }
        }
        for (int i = 0; i < inskillDebuffList.Count; i++)
        {
            Buff buff = inskillDebuffList[i];

            if (buff.isMulti)
                mulPivot[buff.objectIdx] += buff.buffRate;
            else
                addPivot[buff.objectIdx] += buff.buffRate;

            //다음 적중 시도만 강화일 경우, debuff 제거
            if (buff.duration == 0)
            {
                inskillDebuffList.RemoveAt(i);
                i--;
            }
        }

        buffStat[0].value = 1;
        for (int i = 1; i < 13; i++)
            if (i != (int)StatName.currHP && i != (int)StatName.currAP)
                buffStat[i].value = Mathf.CeilToInt(dungeonStat[i] * mulPivot[i] + addPivot[i]);
    }

    public virtual void StatLoad()
    {

    }
}
