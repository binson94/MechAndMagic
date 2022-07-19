using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Unit : MonoBehaviour
{
    #region Variable
    protected BattleManager BM;

    #region Stat
    [Header("Stats")]
    //레벨
    public int LVL;
    public int classIdx;
    //던전 입장 시 스텟 - 아이템 및 영구 적용 버프
    [SerializeField] public int[] dungeonStat = new int[13];
    //유동적으로 변하는 스텟 - 최종(모든 버프 적용)
    public int[] buffStat = new int[13];

    public int shieldAmount;
    public int shieldMax;

    //0:이번 턴 가한 데미지, 1:이전 턴 가한 데미지, 2:이번 턴 받은 데미지, 3:이전 턴 받은 데미지
    [HideInInspector] public int[] dmgs = new int[4];
    #endregion Stat

    #region Buff
    [Header("Buffs")]
    protected int orderIdx;

    public BuffSlot turnBuffs = new BuffSlot();
    public BuffSlot turnDebuffs = new BuffSlot();

    //한 스킬 내에서만 적용되는 버프, 스킬 사용 중에 계산
    public BuffSlot skillBuffs = new BuffSlot();
    public BuffSlot skillDebuffs = new BuffSlot();

    protected BuffSlot shieldBuffs = new BuffSlot();

    public ImplantBomb implantBomb = null;
    #endregion

    #region Active
    [Header("Battle")]
    protected bool isAcc;     //적중 여부
    protected bool isCrit;    //크리티컬 여부
    #endregion Active

    #region SkillIdx
    [Header("Skill")]
    public int[] activeIdxs = new int[6];
    public int[] cooldowns = new int[6];
    public int[] passiveIdxs = new int[4];
    #endregion SkillIdx
    #endregion Variable

    #region Function
    //전투 시작 시 1번만 호출
    public virtual void OnBattleStart(BattleManager BM) { this.BM = BM; StatLoad(); orderIdx = 0; }
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

        buffStat[(int)Obj.currAP] = buffStat[(int)Obj.행동력];

        void TurnBuffUpdate()
        {
            turnBuffs.TurnUpdate();
            turnDebuffs.TurnUpdate();
            shieldBuffs.TurnUpdate();
        }
    }
    public virtual void OnTurnEnd() {}

    #region Skill
    #region Skill Condition
    public virtual string CanCastSkill(int skillSlotIdx)
    {
        Skill s = SkillManager.GetSkill(classIdx, activeIdxs[skillSlotIdx]);
        if (buffStat[(int)Obj.currAP] < GetSkillCost(s))
            return $"{s.name}(을)를 사용하기 위한 행동력이 부족합니다.";
        else if (cooldowns[skillSlotIdx] > 0)
            return $"{SkillManager.GetSkill(classIdx, activeIdxs[skillSlotIdx]).name}(은)는 아직 쿨타임입니다.";
        return string.Empty;
    }   
    public virtual int GetSkillCost(Skill s)
    {
        float addPivot = 0, mulPivot = 0;

        turnBuffs.GetAPCost(ref addPivot, ref mulPivot, s.category, true);
        turnDebuffs.GetAPCost(ref addPivot, ref mulPivot, s.category, false);

        return Mathf.RoundToInt(Mathf.Max(0, s.apCost - addPivot) * Mathf.Max(0, (1 - mulPivot)));
    }
    public bool HasSkill(int skillIdx, bool isCategory = false)
    {
        if (isCategory)
        {
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
            for (int i = 0; i < activeIdxs.Length; i++)
                if (activeIdxs[i] == skillIdx)
                    return true;
            for (int i = 0; i < passiveIdxs.Length; i++)
                if (passiveIdxs[i] == skillIdx)
                    return true;
            return false;
        }
    }
    #endregion Skill Condition

    #region Active
    public virtual void ActiveSkill(int slotIdx, List<Unit> selects)
    {
        //적중 성공 여부
        isAcc = true;
        //크리티컬 성공 여부
        isCrit = false;


        //skillDB에서 스킬 불러오기
        Skill skill = SkillManager.GetSkill(classIdx, activeIdxs[slotIdx]);

        skillBuffs.Clear();
        skillDebuffs.Clear();

        if (skill == null)
        {
            Debug.LogError("skill is null");
            return;
        }

        LogManager.instance.AddLog($"{name}(이)가 {skill.name}(을)를 시전했습니다.");

        Passive_SkillCast(skill);

        //skill 효과 순차적으로 계산
        Active_Effect(skill, selects);

        orderIdx++;
        buffStat[(int)Obj.currAP] -= GetSkillCost(skill);
        cooldowns[slotIdx] = skill.cooldown;
    }
    protected virtual void Active_Effect(Skill skill, List<Unit> selects)
    {
        List<Unit> effectTargets;
        List<Unit> damaged = new List<Unit>();
        float stat;

        for (int i = 0; i < skill.effectCount; i++)
        {
            effectTargets = GetEffectTarget(selects, damaged, skill.effectTarget[i]);
            stat = GetEffectStat(effectTargets, skill.effectStat[i]);

            switch ((EffectType)skill.effectType[i])
            {
                //데미지 - 스킬 버프 계산 후 
                case EffectType.Damage:
                    {
                        StatUpdate_Skill(skill);

                        float dmg = stat * skill.effectRate[i];

                        damaged.Clear();
                        foreach(Unit u in effectTargets)
                        {
                            if (!u.isActiveAndEnabled)
                                continue;

                            int acc = 20;
                            if (buffStat[(int)Obj.명중률] >= u.buffStat[(int)Obj.회피율])
                                acc = 60 + 6 * (buffStat[(int)Obj.명중률] - u.buffStat[(int)Obj.회피율]) / (u.LVL + 2);
                            else
                                acc = Mathf.Max(acc, 60 + 6 * (buffStat[(int)Obj.명중률] - u.buffStat[(int)Obj.회피율]) / (LVL + 2));

                            //명중 시
                            if (Random.Range(0, 100) < acc)
                            {
                                isAcc = true;
                                isCrit = Random.Range(0, 100) < buffStat[(int)Obj.치명타율];

                                u.GetDamage(this, dmg, buffStat[(int)Obj.방어력무시], isCrit ? buffStat[(int)Obj.치명타피해] : 100);
                                damaged.Add(u);

                                Passive_SkillHit(skill);
                            }
                            else
                            {
                                isAcc = false;
                                LogManager.instance.AddLog($"{u.name}(이)가 스킬을 회피하였습니다.");
                            }
                        }
                        
                        break;
                    }
                case EffectType.Heal:
                    {
                        float heal = stat * skill.effectRate[i];

                        foreach (Unit u in effectTargets)
                            u.GetHeal(skill.effectCalc[i] == 1 ? heal * u.buffStat[(int)Obj.체력] : heal);
                        break;
                    }
                case EffectType.Active_Buff:
                    {
                        if (skill.effectCond[i] == 0 || skill.effectCond[i] == 1 && isAcc || skill.effectCond[i] == 2 && isCrit)
                            foreach (Unit u in effectTargets)
                                u.AddBuff(this, orderIdx, skill, i, stat);
                        break;
                    }
                case EffectType.Active_Debuff:
                    {
                        if (skill.effectCond[i] == 0 || skill.effectCond[i] == 1 && isAcc || skill.effectCond[i] == 2 && isCrit)
                            foreach (Unit u in effectTargets)
                                AddDebuff(this, orderIdx, skill, i, stat);
                        break;
                    }
                case EffectType.Active_RemoveBuff:
                    {
                        foreach (Unit u in effectTargets)
                            u.RemoveBuff(Mathf.RoundToInt(skill.effectRate[i]));
                        break;
                    }
                case EffectType.Active_RemoveDebuff:
                    {
                        foreach (Unit u in effectTargets)
                            u.RemoveDebuff(Mathf.RoundToInt(skill.effectRate[i]));
                        break;
                    }
                default:
                    break;
            }
        }
    }
    protected List<Unit> GetEffectTarget(List<Unit> selects, List<Unit> dmged, int effectTarget)
    {
        switch (effectTarget)
        {
            case 0:
                List<Unit> tmp = new List<Unit>();
                tmp.Add(this);
                return tmp;
            case 1:
                return selects;
            case 12:
                return dmged;
            default:
                return  BM.GetEffectTarget(effectTarget);
        }
    }
    ///<summary> 효과 발동 시 계수 스텟 반환 </summary>
    protected float GetEffectStat(List<Unit> targets, int effectStatIdx)
    {
        //기본 스텟
        if (effectStatIdx <= 12)
            return buffStat[effectStatIdx];
        switch ((Obj)effectStatIdx)
        {
            //전 턴 받은 피해
            case Obj.GetDmg:
                return dmgs[3];
            //전 턴 가한 피해
            case Obj.GiveDmg:
                return dmgs[1];
            //타겟 잃은 체력 비율
            case Obj.LossPer:
                if (targets.Count <= 0) return 0;
                return 1 - ((float)targets[0].buffStat[(int)Obj.currHP] / targets[0].buffStat[(int)Obj.체력]);
            //타겟 현재 체력 비율
            case Obj.CurrPer:
                if (targets.Count <= 0) return 0;
                return (float)targets[0].buffStat[(int)Obj.currHP] / targets[0].buffStat[(int)Obj.체력];
            //버프 갯수
            case Obj.BuffCnt:
                return turnBuffs.Count;
            //타겟 디버프 갯수
            case Obj.DebuffCnt:
                if (targets.Count <= 0) return 0;
                return targets[0].turnDebuffs.Count;
            //타겟 최대 체력
            case Obj.MaxHP:
                if (targets.Count <= 0) return 1;
                return targets[0].buffStat[(int)Obj.체력];
            default:
                return 0;
        }
    }
    #endregion Active

    #region Passive
    //전투 시작 시, 패시브 버프 시전
    protected virtual void Passive_BattleStart() { }
    //타격 성공 시, 패시브 버프 시전
    protected virtual void Passive_SkillHit(Skill active)
    {
        for (int i = 0; i < passiveIdxs.Length; i++)
        {
            Skill s = SkillManager.GetSkill(classIdx, passiveIdxs[i]);
            for (int j = 0; j < s.effectCount; j++)
            {
                switch ((EffectType)s.effectType[j])
                {
                    case EffectType.Passive_CritHitBuff:
                        AddBuff(this, orderIdx, s, j, 0);
                        break;
                    case EffectType.Passive_CritHitDebuff:
                        AddDebuff(this, orderIdx, s, j, 0);
                        break;
                }
            }

        }
    }
    //스킬 시전 시, 패시브 버프 시전
    protected virtual void Passive_SkillCast(Skill active)
    {
        for (int j = 0; j < passiveIdxs.Length; j++)
        {
            Skill skill = SkillManager.GetSkill(classIdx, passiveIdxs[j]);

            for (int i = 0; i < skill.effectCount; i++)
            {
                if (active.category != 0 && active.category != skill.effectCond[i])
                    continue;

                switch ((EffectType)skill.effectType[i])
                {
                    case EffectType.Passive_CastBuff:
                        {
                            AddBuff(this, orderIdx, skill, i, 0);
                            break;
                        }
                    case EffectType.Passive_CastDebuff:
                        {
                            AddDebuff(this, orderIdx, skill, i, 0);
                            break;
                        }
                    default:
                        break;
                }
            }
        }
    }
    #endregion Passive
    #endregion Skill

    #region Buff
    #region Add
    public void AddBuff(Unit caster, int order, Skill s, int effectIdx, float rate)
    {
        float stat;
        if (s.effectStat[effectIdx] <= 0) stat = 1;
        else if (s.effectStat[effectIdx] <= 12)
            stat = dungeonStat[s.effectStat[effectIdx]];
        else if (s.effectStat[effectIdx] == (int)Obj.보호막)
        {
            AddShield(s, effectIdx);
            return;
        }
        else
            stat = rate;
        if(s.effectObject[effectIdx] == (int)Obj.APCost)
        {
            Buff b = new Buff(BuffType.AP, new BuffOrder(caster, order), s.name, s.effectCond[effectIdx], 1, s.effectRate[effectIdx], s.effectCalc[effectIdx], s.effectTurn[effectIdx], s.effectDispel[effectIdx], s.effectVisible[effectIdx]);
            turnBuffs.Add(b);
        }
        else
        {
            Buff b = new Buff(BuffType.Stat, new BuffOrder(caster, order), s.name, s.effectObject[effectIdx], stat, s.effectRate[effectIdx], s.effectCalc[effectIdx], s.effectTurn[effectIdx], s.effectDispel[effectIdx], s.effectVisible[effectIdx]);
            if (s.effectTurn[effectIdx] < 0)
                skillBuffs.Add(b);
            else
                turnBuffs.Add(b);
        }

        StatUpdate_Turn();
    }
    public virtual void AddDebuff(Unit caster, int order, Skill s, int effectIdx, float rate)
    {
        float stat;
        if (s.effectStat[effectIdx] <= 0) stat = 1;
        else if (s.effectStat[effectIdx] <= 12)
            stat = dungeonStat[s.effectStat[effectIdx]];
        else
            stat = rate;

        if (s.effectObject[effectIdx] == (int)Obj.APCost)
        {
            Buff b = new Buff(BuffType.AP, new BuffOrder(caster, order), s.name, s.effectCond[effectIdx], 1, s.effectRate[effectIdx], s.effectCalc[effectIdx], s.effectTurn[effectIdx], s.effectDispel[effectIdx], s.effectVisible[effectIdx]);
            turnDebuffs.Add(b);
        }
        else
        {
            Buff b = new Buff(BuffType.Stat, new BuffOrder(caster, order), s.name, s.effectObject[effectIdx], stat, s.effectRate[effectIdx], s.effectCalc[effectIdx], s.effectTurn[effectIdx], s.effectDispel[effectIdx], s.effectVisible[effectIdx]);
            if (s.effectTurn[effectIdx] < 0)
                skillDebuffs.Add(b);
            else
                turnDebuffs.Add(b);
        }

        StatUpdate_Turn();
    }
    public void AddShield(Skill s, int effectIdx)
    {
        float rate = buffStat[s.effectStat[effectIdx]] * s.effectRate[effectIdx];
        turnBuffs.Add(new Buff(BuffType.Stat, new BuffOrder(this), s.name, (int)Obj.보호막, 1, rate, 0, s.effectTurn[effectIdx], s.effectDispel[effectIdx], s.effectVisible[effectIdx]));
        ShieldUpdate(rate);
    }
    #endregion Add

    #region Random Remove
    public virtual int RemoveBuff(int count) => turnBuffs.Remove(count);
    public virtual int RemoveDebuff(int count) => turnDebuffs.Remove(count);
    #endregion Random Remove

    #region StatUpdate
    protected virtual void StatUpdate_Turn()
    {
        float[] mulPivot = new float[13];
        float[] addPivot = new float[13];

        for (int i = 0; i < 13; i++)
        {
            mulPivot[i] = 1;
            addPivot[i] = 0;
        }

        turnBuffs.GetBuffRate(ref addPivot, ref mulPivot, true);
        turnDebuffs.GetBuffRate(ref addPivot, ref mulPivot, false);

        for (int i = 0; i < 13; i++)
            if (i != (int)Obj.currHP && i != (int)Obj.currAP)
                buffStat[i] = Mathf.CeilToInt(dungeonStat[i] * mulPivot[i] + addPivot[i]);

    }
    protected virtual void StatUpdate_Skill(Skill s)
    {
        float[] mulPivot = new float[13];
        float[] addPivot = new float[13];

        for (int i = 0; i < 13; i++)
        {
            mulPivot[i] = 1;
            addPivot[i] = 0;
        }

        turnBuffs.GetBuffRate(ref addPivot, ref mulPivot, true);
        turnDebuffs.GetBuffRate(ref addPivot, ref mulPivot, false);
        skillBuffs.GetBuffRate(ref addPivot, ref mulPivot, true);
        skillDebuffs.GetBuffRate(ref addPivot, ref mulPivot, false);

        for (int i = 0; i < 13; i++)
            if (i != 1 && i != 3)
                buffStat[i] = Mathf.CeilToInt(dungeonStat[i] * mulPivot[i] + addPivot[i]);
    }
    #endregion StatUpdate

    public bool IsStun() => turnDebuffs.buffs.Any(x => x.objectIdx.Any(y => y == (int)Obj.기절));

    protected void HealBuffUpdate()
    {
        float[] addPivot = new float[2];

        foreach (Buff b in turnBuffs.buffs)
            if (b.type == BuffType.Stat)
                for (int i = 0; i < b.count; i++)
                    if (b.objectIdx[i] == (int)Obj.currHP)
                        addPivot[0] += b.buffRate[i];
                    else if (b.objectIdx[i] == (int)Obj.currAP)
                        addPivot[1] += b.buffRate[i];
                    else if (b.objectIdx[i] == (int)Obj.순환)
                        addPivot[0] += b.buffRate[i];

        foreach (Buff b in turnDebuffs.buffs)
            if (b.type == BuffType.Stat)
                for (int i = 0; i < b.count; i++)
                    if (b.objectIdx[i] == (int)Obj.currHP)
                        addPivot[0] -= b.buffRate[i];
                    else if (b.objectIdx[i] == (int)Obj.출혈)
                        addPivot[0] -= b.buffRate[i];
                    else if (b.objectIdx[i] == (int)Obj.화상)
                        addPivot[0] -= b.buffRate[i] / (1 + 0.1f * buffStat[(int)Obj.방어력]);
                    else if (b.objectIdx[i] == (int)Obj.저주)
                        addPivot[0] -= b.buffRate[i];
                    else if (b.objectIdx[i] == (int)Obj.맹독)
                        addPivot[0] -= b.buffRate[i] / (1 + 0.1f * buffStat[(int)Obj.방어력]);

        for (int i = 0; i < 2; i++)
            buffStat[2 * i + 1] = Mathf.Min(buffStat[2 * i + 2], Mathf.RoundToInt(buffStat[2 * i + 1] + addPivot[i]));
    }
    public void ShieldUpdate(float add = 0)
    {
        float max = 0;
        foreach (Buff b in turnBuffs.buffs)
            for (int i = 0; i < b.count; i++)
                if (b.objectIdx[i] == (int)Obj.보호막)
                    max += b.buffRate[i];

        shieldMax = Mathf.RoundToInt(max);
        shieldAmount = Mathf.Min(shieldAmount + Mathf.RoundToInt(add), shieldMax);
    }
    #endregion Buff

    public virtual KeyValuePair<bool, int> GetDamage(Unit caster, float dmg, int pen, int crb)
    {
        float beforeRate = (float)buffStat[(int)Obj.currHP] / buffStat[(int)Obj.체력];
        
        //아이언하트 4세트 - 받는 피해 감소
        float ironHeartDEF = 1 - ItemManager.GetSetData(25).Value[2];

        float finalDEF = Mathf.Max(0, buffStat[(int)Obj.방어력] * (100 - pen) / 100f);
        int finalDmg = Mathf.RoundToInt(dmg / (1 + 0.1f * finalDEF) * ironHeartDEF * crb / 100);

        if (shieldAmount - finalDmg >= 0)
            shieldAmount -= finalDmg;
        else
        {
            buffStat[(int)Obj.currHP] -= finalDmg - shieldAmount;
            shieldAmount = 0;
        }
        dmgs[2] += finalDmg;
        caster.dmgs[0] += finalDmg;
        //피격 시 차감되는 버프 처리

        if(crb <= 100)
            LogManager.instance.AddLog($"{name}(이)가 피해 {finalDmg}를 입었습니다.");
        else
            LogManager.instance.AddLog($"{name}(이)가 치명타 피해 {finalDmg}를 입었습니다.");
        
        //아이언하트 3세트 - 체력 40% 이하로 떨어질 때 디버프 하나 해제
        if(ItemManager.GetSetData(25).Value[1] > 0 && (float)buffStat[(int)Obj.currHP] / buffStat[(int)Obj.체력] < 0.4f && beforeRate >= 0.4f)
            RemoveDebuff(1);

        bool killed = false;
        if (buffStat[(int)Obj.currHP] <= 0)
        {
            killed = true;

            if(implantBomb != null)
            {
                List<Unit> targets = BM.GetEffectTarget(6);

                foreach (Unit u in targets)
                    u.GetDamage(implantBomb.caster, implantBomb.dmg, implantBomb.pen, 100);
            }
        }

        return new KeyValuePair<bool, int>(killed, -finalDmg);
    }
    public virtual void GetHeal(float heal) => buffStat[(int)Obj.currHP] = Mathf.Min(buffStat[(int)Obj.체력], Mathf.RoundToInt(buffStat[(int)Obj.currHP] + heal));
    public void GetAPHeal(float heal) => buffStat[(int)Obj.currAP] = Mathf.Min(buffStat[(int)Obj.행동력], Mathf.RoundToInt(buffStat[(int)Obj.currAP] + heal));

    public virtual bool IsBoss() => false;
    public virtual void StatLoad() { }
    #endregion Function
}