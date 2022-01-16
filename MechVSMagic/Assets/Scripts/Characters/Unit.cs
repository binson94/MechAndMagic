using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

//스텟 index - 12가지
public enum Obj { None, currHP, HP, currAP, AP, ATK, DEF, ACC, DOG, CRC, CRB, PEN, SPD, 
    Stun, GetDmg, GiveDmg, LossPer, CurrPer, BuffCnt, DebuffCnt, MaxHP, Bleed, Burn, Cannon, Cycle, Curse, Posion, Shield  };

public class Unit : MonoBehaviour
{
    protected BattleManager BM;

    [Header("Stats")]
    #region Stat
    //레벨
    public int LVL;
    public int classIdx;
    //캐릭터 기본 스텟, 불변
    [SerializeField] protected int[] basicStat = new int[13];
    //던전 입장 시 스텟 - 아이템 및 영구 적용 버프
    [SerializeField] public int[] dungeonStat = new int[13];
    //유동적으로 변하는 스텟 - 최종(모든 버프 적용)
    public int[] buffStat = new int[13];

    public int shieldAmount;
    public int shieldMax;
    #endregion

    [Header("Buffs")]
    #region Buff
    //영구 적용 버프는 BattleManager.charState에서 관리(PlayerPrefs 저장)

    //전투 동안 적용 버프(턴 제한), 버프 해제 먹음, 전투 시작, 전투 중에 계산
    public List<Buff> inbattleBuffList = new List<Buff>();
    public List<Buff> inbattleDebuffList = new List<Buff>();

    //한 스킬 내에서만 적용되는 버프, 스킬 사용 중에 계산
    public List<Buff> inskillBuffList = new List<Buff>();
    public List<Buff> inskillDebuffList = new List<Buff>();

    protected List<Buff> shieldBuffList = new List<Buff>();
    protected List<APBuff> apBuffs = new List<APBuff>();
    #endregion

    [Header("Battle")]
    protected bool isAcc;     //적중 여부
    protected bool isCrit;    //크리티컬 여부

    [Header("Skill")]
    public int[] activeIdxs = new int[6];
    public int[] cooldowns = new int[6];
    public int[] passiveIdxs = new int[4];

    //0:이번 턴 가한 데미지, 1:이전 턴 가한 데미지, 2:이번 턴 받은 데미지, 3:이전 턴 받은 데미지
    [HideInInspector] public int[] dmgs = new int[4];

    //전투 시작 시 1번만 호출
    public virtual void OnBattleStart(BattleManager BM) { this.BM = BM; }
    //내 턴이 시작될 때 호출 - AP 값 초기화, 스킬 쿨다운 감소, 턴 버프 계산
    public virtual void OnTurnStart()
    {
        dmgs[1] = dmgs[0]; dmgs[3] = dmgs[2];
        dmgs[0] = dmgs[2] = 0;

        for (int i = 0; i < 5; i++)
        {
            cooldowns[i]--;
            if (cooldowns[i] < 0)
                cooldowns[i] = 0;
        }

        //버프 지속시간 최신화
        TurnBuffUpdate();

        StatUpdate_Turn();
        HealBuffUpdate();

        buffStat[(int)Obj.currAP] = buffStat[(int)Obj.AP];

        void TurnBuffUpdate()
        {
            for (int i = 0; i < inbattleBuffList.Count; i++)
            {
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
                if (inbattleDebuffList[i].duration == 99)
                    continue;
                inbattleDebuffList[i].duration--;

                if (inbattleDebuffList[i].duration < 1)
                {
                    inbattleDebuffList.RemoveAt(i);
                    i--;
                }
            }
            for (int i = 0; i < apBuffs.Count; i++)
            {
                if (apBuffs[i].duration == 99)
                    continue;
                apBuffs[i].duration--;

                if (apBuffs[i].duration < 1)
                {
                    apBuffs.RemoveAt(i);
                    i--;
                }
            }
            for (int i = 0; i < shieldBuffList.Count; i++)
            {
                if (shieldBuffList[i].duration == 99)
                    continue;
                shieldBuffList[i].duration--;

                if (shieldBuffList[i].duration <= 0)
                {
                    shieldBuffList.RemoveAt(i);
                    i--;
                }
            }
        }
    }
    public virtual void OnTurnEnd() {}

    #region Skill
    public virtual int GetSkillCost(Skill s)
    {
        float addPivot = 0, mulPivot = 0;

        foreach (APBuff b in apBuffs)
        {
            if (b.category == s.category || b.category == 0)
            {
                if (b.isMulti)
                    mulPivot += b.rate;
                else
                    addPivot += b.rate;
            }
        }

        return Mathf.RoundToInt(Mathf.Max(0, s.apCost - addPivot) * Mathf.Max(0, (1 - mulPivot)));
    }
    public bool HasSkill(int skillIdx, bool isCategory = false)
    {
        if (isCategory)
        {
            Skill s;
            for (int i = 0; i < activeIdxs.Length; i++)
                if (SkillManager.GetSkill(classIdx, activeIdxs[i]).category == skillIdx)
                    return true;
            for (int i = 0; i < passiveIdxs.Length; i++)
                if (SkillManager.GetSkill(classIdx, passiveIdxs[i]).category == skillIdx)
                    return true;
            return false;
        }
        else
        {

            for (int i = 0; i < 5; i++)
                if (activeIdxs[i] == skillIdx)
                    return true;
            for (int i = 0; i < 3; i++)
                if (passiveIdxs[i] == skillIdx)
                    return true;
            return false;
        }
    }

    public virtual void ActiveSkill(int idx, List<Unit> selects)
    {
        //적중 성공 여부
        isAcc = true;
        //크리티컬 성공 여부
        isCrit = false;


        //skillDB에서 스킬 불러오기
        Skill skill = SkillManager.GetSkill(classIdx, activeIdxs[idx]);

        inskillBuffList.Clear();
        if (skill == null)
        {
            Debug.LogError("skill is null");
            return;
        }

        Passive_SkillCast(skill.category);

        //skill 효과 순차적으로 계산
        Active_Effect(skill, selects);

        buffStat[(int)Obj.currAP] -= GetSkillCost(skill);
        cooldowns[idx] = skill.cooldown;
    }
    protected virtual void Active_Effect(Skill skill, List<Unit> selects)
    {
        List<Unit> effectTargets;
        List<Unit> damaged = new List<Unit>();
        float rate = 0;

        for (int i = 0; i < skill.effectCount; i++)
        {
            //타겟 결정
            switch (skill.effectTarget[i])
            {
                case 0:
                    effectTargets = new List<Unit>();
                    effectTargets.Add(this);
                    break;
                case 1:
                    effectTargets = selects;
                    break;
                case 12:
                    effectTargets = damaged;
                    break;
                default:
                    effectTargets = BM.GetEffectTarget(skill.effectTarget[i]);
                    break;
            }
            //버프 결정
            {
                if (skill.effectStat[i] <= 12)
                    rate = 0;
                //전 턴 받은 피해
                else if (skill.effectStat[i] == 14)
                    rate = dmgs[3];
                //전 턴 가한 피해
                else if (skill.effectStat[i] == 15)
                    rate = dmgs[1];
                //타겟 잃은 체력 비율
                else if (skill.effectStat[i] == 16)
                    rate = 1 - ((float)selects[0].buffStat[(int)Obj.currHP] / selects[0].buffStat[(int)Obj.HP]);
                //타겟 현재 체력 비율
                else if (skill.effectStat[i] == 17)
                    rate = (float)selects[0].buffStat[(int)Obj.currHP] / selects[0].buffStat[(int)Obj.HP];
                else if (skill.effectStat[i] == 18)
                    rate = inbattleBuffList.Count;
                else if (skill.effectStat[i] == 19)
                    rate = selects[0].inbattleDebuffList.Count;
                else if (skill.effectStat[i] == 20)
                    rate = selects[0].buffStat[(int)Obj.HP];
                else if (skill.effectStat[i] == 21)
                    rate = buffStat[(int)Obj.ATK] * 0.15f;
                else if (skill.effectStat[i] == 22)
                    rate = buffStat[(int)Obj.ATK] * 0.7f;
            }

            switch ((SkillType)skill.effectType[i])
            {
                //데미지 - 스킬 버프 계산 후 
                case SkillType.Damage:
                    {
                        StatUpdate_Skill(skill.category);
                        
                        int dmg = Mathf.CeilToInt(buffStat[skill.effectStat[i]] * skill.effectRate[i]);

                        damaged.Clear();
                        foreach(Unit u in effectTargets)
                        {
                            if (!u.isActiveAndEnabled)
                                continue;

                            //명중 연산 - 최소 명중률 10%
                            int acc = Mathf.Max(buffStat[(int)Obj.ACC] - u.buffStat[(int)Obj.DOG], 10);
                            //명중 시
                            if (Random.Range(0, 100) < acc)
                            {
                                isAcc = true;
                                //크리티컬 연산 - dmg * CRB
                                if (Random.Range(0, 100) < buffStat[(int)Obj.CRC])
                                {
                                    isCrit = true;
                                    dmg = Mathf.CeilToInt(dmg * (buffStat[(int)Obj.CRB] / 100f));
                                }
                                else
                                    isCrit = false;

                                u.GetDamage(this, dmg, buffStat[(int)Obj.PEN]);
                                damaged.Add(u);

                                Passive_SkillHit(skill.category);
                            }
                            else
                            {
                                isAcc = false;
                                LogManager.instance.AddLog("Dodge");
                            }
                        }
                        
                        break;
                    }
                case SkillType.Heal:
                    {
                        float heal = buffStat[skill.effectStat[i]] * skill.effectRate[i];

                        foreach (Unit u in effectTargets)
                            u.GetHeal(skill.effectCalc[i] == 1 ? heal * u.buffStat[(int)Obj.HP] : heal);
                        break;
                    }
                case SkillType.Active_Buff:
                    {
                        if (skill.effectCond[i] == 0 || skill.effectCond[i] == 1 && isAcc || skill.effectCond[i] == 2 && isCrit)
                            AddBuff(skill, i, rate);
                        break;
                    }
                case SkillType.Active_Debuff:
                    {
                        if (skill.effectCond[i] == 0 || skill.effectCond[i] == 1 && isAcc || skill.effectCond[i] == 2 && isCrit)
                            AddDebuff(skill, i, rate);
                        break;
                    }
                case SkillType.Passive_APBuff:
                    {
                        apBuffs.Add(new APBuff(skill.name, skill.effectTurn[i], skill.effectCond[i], skill.effectRate[i], skill.effectCalc[i] == 1));
                        break;
                    }
                default:
                    break;
            }
        }
    }

    //passives - 전투 시작 시, 패시브 버프 시전
    protected virtual void Passive_BattleStart()
    {

    }
    //passives - 타격 성공 시, 패시브 버프 시전
    protected virtual void Passive_SkillHit(int skillCategory)
    {
        for (int i = 0; i < passiveIdxs.Length; i++)
        {
            Skill s = SkillManager.GetSkill(classIdx, passiveIdxs[i]);
            for (int j = 0; j < s.effectCount; j++)
            {
                switch ((SkillType)s.effectType[j])
                {
                    case SkillType.Passive_CritHitBuff:
                        AddBuff(s, j, 0);
                        break;
                    case SkillType.Passive_CritHitDebuff:
                        AddDebuff(s, j, 0);
                        break;
                }
            }

        }
    }
    //28 ~ 31 passives - 스킬 시전 시, 패시브 버프 시전
    protected virtual void Passive_SkillCast(int skillCategory)
    {
        for (int j = 0; j < passiveIdxs.Length; j++)
        {
            Skill skill = SkillManager.GetSkill(classIdx, passiveIdxs[j]);

            for (int i = 0; i < skill.effectCount; i++)
            {
                if (skillCategory != skill.effectCond[i])
                    continue;

                switch ((SkillType)skill.effectType[i])
                {
                    case SkillType.Passive_CastBuff:
                        {
                            AddBuff(skill, i, 0);
                            break;
                        }
                    case SkillType.Passive_CastDebuff:
                        {
                            AddDebuff(skill, i, 0);
                            break;
                        }
                    default:
                        break;
                }
            }
        }
    }
    #endregion

    #region buff
    //type :0-next attack, 1-inskill, 2-turn 
    public void AddBuff(Skill s, int effectIdx, float rate)
    {
        float stat;
        if (s.effectStat[effectIdx] <= 12)
            stat = dungeonStat[s.effectStat[effectIdx]];
        else if (s.effectStat[effectIdx] == (int)Obj.Shield)
        {
            AddShield(s, effectIdx);
            return;
        }
        else
            stat = rate;

        if (s.effectTurn[effectIdx] < 0)
            inskillBuffList.Add(new Buff(s.name, s.effectTurn[effectIdx], s.effectObject[effectIdx], stat, s.effectCalc[effectIdx], s.effectRate[effectIdx]));

        else
            inbattleBuffList.Add(new Buff(name, s.effectTurn[effectIdx], s.effectObject[effectIdx], stat, s.effectCalc[effectIdx], s.effectRate[effectIdx], s.effectDispel[effectIdx], s.effectVisible[effectIdx]));
    }
    public void AddDebuff(Skill s, int effectIdx, float rate)
    {
        float stat;
        if (s.effectStat[effectIdx] <= 12)
            stat = dungeonStat[s.effectStat[effectIdx]];
        else
            stat = rate;

        if (s.effectTurn[effectIdx] < 0)
            inskillDebuffList.Add(new Buff(s.name, s.effectTurn[effectIdx], s.effectObject[effectIdx], stat, s.effectCalc[effectIdx], s.effectRate[effectIdx]));
        else
            inbattleDebuffList.Add(new Buff(s.name, s.effectTurn[effectIdx], s.effectObject[effectIdx], stat, s.effectCalc[effectIdx], s.effectRate[effectIdx], s.effectDispel[effectIdx], s.effectVisible[effectIdx]));
    }

    public void AddShield(Skill s, int effectIdx)
    {
        float rate = buffStat[s.effectStat[effectIdx]] * s.effectRate[effectIdx];
        shieldBuffList.Add(new Buff(s.name, s.effectTurn[effectIdx], (int)Obj.Shield, 1, 0, rate, s.effectDispel[effectIdx], s.effectVisible[effectIdx]));
        ShieldUpdate(rate);
    }

    public bool IsStun() => inbattleDebuffList.Any(x => x.objectIdx == (int)Obj.Stun);


    //매 턴 시작 시, 버프 스텟 계산
    protected virtual void StatUpdate_Turn()
    {
        float[] mulPivot = new float[13];
        float[] addPivot = new float[13];

        for (int i = 0; i < 13; i++)
        {
            mulPivot[i] = 1;
            addPivot[i] = 0;
        }

        CalcTurnBuffPivot(ref addPivot, ref mulPivot);

        for (int i = 0; i < 13; i++)
            if (i != 1 && i != 3)
                buffStat[i] = Mathf.CeilToInt(dungeonStat[i] * mulPivot[i] + addPivot[i]);
        
    }
    //공격 시 계산
    protected virtual void StatUpdate_Skill(int skillCategory)
    {
        float[] mulPivot = new float[13];
        float[] addPivot = new float[13];

        for (int i = 0; i < 13; i++)
        {
            mulPivot[i] = 1;
            addPivot[i] = 0;
        }

        CalcTurnBuffPivot(ref addPivot, ref mulPivot);
        CalcSkillBuffPivot(ref addPivot, ref mulPivot);

        for (int i = 0; i < 13; i++)
            if (i != 1 && i != 3)
                buffStat[i] = Mathf.CeilToInt(dungeonStat[i] * mulPivot[i] + addPivot[i]);
    }
    protected void HealBuffUpdate()
    {
        float[] addPivot = new float[2];

        foreach (Buff b in inbattleBuffList)
            if (b.objectIdx == (int)Obj.currHP)
                addPivot[0] += b.buffRate;
            else if (b.objectIdx == (int)Obj.currAP)
                addPivot[1] += b.buffRate;
            else if (b.objectIdx == (int)Obj.Cycle)
                addPivot[0] += dungeonStat[(int)Obj.HP] * 0.2f;

        foreach (Buff b in inbattleDebuffList)
            if (b.objectIdx == (int)Obj.Bleed)
                addPivot[0] -= b.buffRate;
            else if (b.objectIdx == (int)Obj.Burn)
                addPivot[0] -= Mathf.Min(0, buffStat[(int)Obj.DEF] - b.buffRate);
            else if (b.objectIdx == (int)Obj.Curse)
                addPivot[0] -= b.buffRate;

        for (int i = 0; i < 2; i++)
            buffStat[2 * i + 1] = Mathf.Min(buffStat[2 * i + 2], Mathf.RoundToInt(buffStat[2 * i + 1] + addPivot[i]));
    }
    public void ShieldUpdate(float add = 0)
    {
        float max = 0;
        foreach (Buff b in inbattleBuffList)
            if (b.objectIdx == (int)Obj.Shield)
                max += b.buffRate;

        shieldMax = Mathf.RoundToInt(max);
        shieldAmount = Mathf.Min(shieldAmount + Mathf.RoundToInt(add), shieldMax);
    }

    protected void CalcTurnBuffPivot(ref float[] add, ref float[] mul)
    {
        foreach (Buff b in inbattleBuffList)
        {
            if (b.objectIdx > 11)
                continue;
            if (b.isMulti)
                mul[b.objectIdx] += b.buffRate;
            else
                add[b.objectIdx] += b.buffRate;
        }
        foreach (Buff b in inbattleDebuffList)
        {
            if (b.objectIdx > 11)
                continue;
            if (b.isMulti)
                mul[b.objectIdx] -= b.buffRate;
            else
                add[b.objectIdx] -= b.buffRate;
        }
    }
    protected void CalcSkillBuffPivot(ref float[] add, ref float[] mul)
    {
        for (int i = 0; i < inskillBuffList.Count; i++)
        {
            Buff buff = inskillBuffList[i];
            if (buff.objectIdx > 11)
                continue;

            if (buff.isMulti)
                mul[buff.objectIdx] += buff.buffRate;
            else
                add[buff.objectIdx] += buff.buffRate;

            //다음 적중 시도만 강화일 경우, buff 제거
            if (buff.duration == -2)
            {
                inskillBuffList.RemoveAt(i);
                i--;
            }
        }
        for (int i = 0; i < inskillDebuffList.Count; i++)
        {
            Buff buff = inskillDebuffList[i];
            if (buff.objectIdx > 11)
                continue;

            if (buff.isMulti)
                mul[buff.objectIdx] -= buff.buffRate;
            else
                add[buff.objectIdx] -= buff.buffRate;

            //다음 적중 시도만 강화일 경우, debuff 제거
            if (buff.duration == -2)
            {
                inskillDebuffList.RemoveAt(i);
                i--;
            }
        }
    }
    #endregion buff

    public virtual bool GetDamage(Unit caster, int dmg, int pen)
    {
        int finalDEF = Mathf.Max(0, buffStat[(int)Obj.DEF] - pen);
        int finalDmg = Mathf.Min(-1, -dmg + finalDEF);

        if (shieldAmount >= finalDmg)
            shieldAmount -= finalDmg;
        else
        {
            finalDmg -= shieldAmount;
            shieldAmount = 0;
            buffStat[(int)Obj.currHP] += finalDmg;
        }
        dmgs[2] -= finalDmg;
        caster.dmgs[0] -= finalDmg;
        //피격 시 차감되는 버프 처리

        LogManager.instance.AddLog(string.Concat(caster.name, " damages ", name, ", ", finalDmg));


        bool killed = false;
        if (buffStat[(int)Obj.currHP] <= 0)
        {
            killed = true;
            gameObject.SetActive(false);
        }

        return killed;
    }
    public virtual void GetHeal(float heal) => buffStat[(int)Obj.currHP] = Mathf.Min(buffStat[(int)Obj.HP], Mathf.RoundToInt(buffStat[(int)Obj.currHP] + heal));


    public virtual bool IsBoss() => false;
    public virtual void StatLoad()
    {

    }
}

//패시브 버프 발동 트리거
//1. 전투 시작 시
//2. 스킬 시전 시 - 28 ~ 31
//3. 스킬 적중 시