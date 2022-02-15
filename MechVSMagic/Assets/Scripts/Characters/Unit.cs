using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

//스텟 index - 12가지
public enum Obj { None, currHP, HP, currAP, AP, ATK, DEF, ACC, DOG, CRC, CRB, PEN, SPD, 
    Stun, GetDmg, GiveDmg, LossPer, CurrPer, BuffCnt, DebuffCnt, MaxHP, Bleed, Burn, Cannon,
    Cycle, Curse, Posion, Shield, Bomb, Venom, APCost };

public class Unit : MonoBehaviour
{
    /* #region Variable */
    protected BattleManager BM;

    /* #region Stat */
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
    /* #endregion Stat */

    /* #region Buff */
    [Header("Buffs")]
    protected int orderIdx;

    public BuffSlot turnBuffs = new BuffSlot();
    public BuffSlot turnDebuffs = new BuffSlot();

    //한 스킬 내에서만 적용되는 버프, 스킬 사용 중에 계산
    public BuffSlot skillBuffs = new BuffSlot();
    public BuffSlot skillDebuffs = new BuffSlot();

    protected BuffSlot shieldBuffs = new BuffSlot();

    public ImplantBomb implantBomb = null;
    /* #endregion */

    /* #region Active */
    [Header("Battle")]
    protected bool isAcc;     //적중 여부
    protected bool isCrit;    //크리티컬 여부
    /* #endregion */

    /* #region SkillIdx */
    [Header("Skill")]
    public int[] activeIdxs = new int[6];
    public int[] cooldowns = new int[6];
    public int[] passiveIdxs = new int[4];
    /* #endregion */
    /* #endregion Variable */

    /* #region Function */
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

        buffStat[(int)Obj.currAP] = buffStat[(int)Obj.AP];

        void TurnBuffUpdate()
        {
            turnBuffs.TurnUpdate();
            turnDebuffs.TurnUpdate();
            shieldBuffs.TurnUpdate();
        }
    }
    public virtual void OnTurnEnd() {}

    /* #region Skill */
    /* #region Skill Condition */
    public virtual string CanCastSkill(int idx)
    {
        if (buffStat[(int)Obj.currAP] < GetSkillCost(SkillManager.GetSkill(classIdx, activeIdxs[idx])))
            return "AP 부족";
        else if (cooldowns[idx] > 0)
            return "쿨다운";
        return "";
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
    /* #endregion Skill Condition */

    /* #region Active */
    public virtual void ActiveSkill(int idx, List<Unit> selects)
    {
        //적중 성공 여부
        isAcc = true;
        //크리티컬 성공 여부
        isCrit = false;


        //skillDB에서 스킬 불러오기
        Skill skill = SkillManager.GetSkill(classIdx, activeIdxs[idx]);

        skillBuffs.Clear();
        skillDebuffs.Clear();

        if (skill == null)
        {
            Debug.LogError("skill is null");
            return;
        }

        Passive_SkillCast(skill);

        //skill 효과 순차적으로 계산
        Active_Effect(skill, selects);

        orderIdx++;
        buffStat[(int)Obj.currAP] -= GetSkillCost(skill);
        cooldowns[idx] = skill.cooldown;
    }
    protected virtual void Active_Effect(Skill skill, List<Unit> selects)
    {
        List<Unit> effectTargets;
        List<Unit> damaged = new List<Unit>();
        float stat;

        for (int i = 0; i < skill.effectCount; i++)
        {
            effectTargets = GetEffectTarget(selects, damaged, skill.effectTarget[i]);
            stat = GetEffectStat(selects, skill.effectStat[i]);
            
            switch ((SkillType)skill.effectType[i])
            {
                //데미지 - 스킬 버프 계산 후 
                case SkillType.Damage:
                    {
                        StatUpdate_Skill(skill);

                        float dmg = stat * skill.effectRate[i];

                        damaged.Clear();
                        foreach(Unit u in effectTargets)
                        {
                            if (!u.isActiveAndEnabled)
                                continue;

                            int acc = 20;
                            if (buffStat[(int)Obj.ACC] >= u.buffStat[(int)Obj.DOG])
                                acc = 60 + 6 * (buffStat[(int)Obj.ACC] - u.buffStat[(int)Obj.DOG]) / (u.LVL + 2);
                            else
                                acc = Mathf.Max(acc, 60 + 6 * (buffStat[(int)Obj.ACC] - u.buffStat[(int)Obj.DOG]) / (LVL + 2));

                            //명중 시
                            if (Random.Range(0, 100) < acc)
                            {
                                isAcc = true;
                                isCrit = Random.Range(0, 100) < buffStat[(int)Obj.CRC];

                                u.GetDamage(this, dmg, buffStat[(int)Obj.PEN], isCrit ? buffStat[(int)Obj.CRB] : 100);
                                damaged.Add(u);

                                Passive_SkillHit(skill);
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
                        float heal = stat * skill.effectRate[i];

                        foreach (Unit u in effectTargets)
                            u.GetHeal(skill.effectCalc[i] == 1 ? heal * u.buffStat[(int)Obj.HP] : heal);
                        break;
                    }
                case SkillType.Active_Buff:
                    {
                        if (skill.effectCond[i] == 0 || skill.effectCond[i] == 1 && isAcc || skill.effectCond[i] == 2 && isCrit)
                            foreach (Unit u in effectTargets)
                                u.AddBuff(this, orderIdx, skill, i, stat);
                        break;
                    }
                case SkillType.Active_Debuff:
                    {
                        if (skill.effectCond[i] == 0 || skill.effectCond[i] == 1 && isAcc || skill.effectCond[i] == 2 && isCrit)
                            foreach (Unit u in effectTargets)
                                AddDebuff(this, orderIdx, skill, i, stat);
                        break;
                    }
                case SkillType.Active_RemoveBuff:
                    {
                        foreach (Unit u in effectTargets)
                            u.RemoveBuff(Mathf.RoundToInt(skill.effectRate[i]));
                        break;
                    }
                case SkillType.Active_RemoveDebuff:
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
    protected float GetEffectStat(List<Unit> selects, int effectStat)
    {
        if (effectStat <= 12)
            return buffStat[effectStat];
        //전 턴 받은 피해
        else if (effectStat == (int)Obj.GetDmg)
            return dmgs[3];
        //전 턴 가한 피해
        else if (effectStat == (int)Obj.GiveDmg)
            return dmgs[1];
        //타겟 잃은 체력 비율
        else if (effectStat == (int)Obj.LossPer)
            return 1 - ((float)selects[0].buffStat[(int)Obj.currHP] / selects[0].buffStat[(int)Obj.HP]);
        //타겟 현재 체력 비율
        else if (effectStat == (int)Obj.CurrPer)
            return (float)selects[0].buffStat[(int)Obj.currHP] / selects[0].buffStat[(int)Obj.HP];
        else if (effectStat == (int)Obj.BuffCnt)
            return turnBuffs.Count;
        else if (effectStat == (int)Obj.DebuffCnt)
            return selects[0].turnDebuffs.Count;
        else if (effectStat == (int)Obj.MaxHP)
            return selects[0].buffStat[(int)Obj.HP];
        //출혈
        else if (effectStat == (int)Obj.Bleed)
            return buffStat[(int)Obj.ATK] * 0.15f;
        //화상
        else if (effectStat == (int)Obj.Burn)
            return buffStat[(int)Obj.ATK] * 0.7f;
        //중독
        else if (effectStat == (int)Obj.Posion)
            return buffStat[(int)Obj.ATK] * 0.1f;
        else
            return 0;
    }
    /* #endregion Active */

    /* #region Passive */
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
                switch ((SkillType)s.effectType[j])
                {
                    case SkillType.Passive_CritHitBuff:
                        AddBuff(this, orderIdx, s, j, 0);
                        break;
                    case SkillType.Passive_CritHitDebuff:
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

                switch ((SkillType)skill.effectType[i])
                {
                    case SkillType.Passive_CastBuff:
                        {
                            AddBuff(this, orderIdx, skill, i, 0);
                            break;
                        }
                    case SkillType.Passive_CastDebuff:
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
    /* #endregion Passive */
    /* #endregion */

    /*#region Buff */
    /* #region Add */
    public void AddBuff(Unit caster, int order, Skill s, int effectIdx, float rate)
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
        if(s.effectObject[effectIdx] == (int)Obj.APCost)
        {
            Buff b = new Buff(BuffType.AP, caster.LVL, new BuffOrder(caster, order), s.name, s.effectCond[effectIdx], 1, s.effectRate[effectIdx], s.effectCalc[effectIdx], s.effectTurn[effectIdx], s.effectDispel[effectIdx], s.effectVisible[effectIdx]);
            turnBuffs.Add(b);
        }
        else
        {
            Buff b = new Buff(BuffType.Stat, caster.LVL, new BuffOrder(caster, order), s.name, s.effectObject[effectIdx], stat, s.effectRate[effectIdx], s.effectCalc[effectIdx], s.effectTurn[effectIdx], s.effectDispel[effectIdx], s.effectVisible[effectIdx]);
            if (s.effectTurn[effectIdx] < 0)
                skillBuffs.Add(b);
            else
                turnBuffs.Add(b);
        }
    }
    public virtual void AddDebuff(Unit caster, int order, Skill s, int effectIdx, float rate)
    {
        float stat;
        if (s.effectStat[effectIdx] <= 12)
            stat = dungeonStat[s.effectStat[effectIdx]];
        else
            stat = rate;
        if (s.effectObject[effectIdx] == (int)Obj.APCost)
        {
            Buff b = new Buff(BuffType.AP, caster.LVL, new BuffOrder(caster, order), s.name, s.effectCond[effectIdx], 1, s.effectRate[effectIdx], s.effectCalc[effectIdx], s.effectTurn[effectIdx], s.effectDispel[effectIdx], s.effectVisible[effectIdx]);
            turnDebuffs.Add(b);
        }
        else
        {
            Buff b = new Buff(BuffType.Stat, caster.LVL, new BuffOrder(caster, order), s.name, s.effectObject[effectIdx], stat, s.effectRate[effectIdx], s.effectCalc[effectIdx], s.effectTurn[effectIdx], s.effectDispel[effectIdx], s.effectVisible[effectIdx]);
            if (s.effectTurn[effectIdx] < 0)
                skillDebuffs.Add(b);
            else
                turnDebuffs.Add(b);
        }
    }
    public void AddShield(Skill s, int effectIdx)
    {
        float rate = buffStat[s.effectStat[effectIdx]] * s.effectRate[effectIdx];
        turnBuffs.Add(new Buff(BuffType.Stat, LVL, new BuffOrder(this, -2), s.name, (int)Obj.Shield, 1, rate, 0, s.effectTurn[effectIdx], s.effectDispel[effectIdx], s.effectVisible[effectIdx]));
        ShieldUpdate(rate);
    }
    /* #endregion Add */

    /* #region Random Remove */
    public virtual int RemoveBuff(int count) => turnBuffs.Remove(count);
    public virtual int RemoveDebuff(int count) => turnDebuffs.Remove(count);
    /* #endregion Random Remove */

    /* #region StatUpdate */
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
            if (i != 1 && i != 3)
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
    /* #endregion StatUpdate */

    public bool IsStun() => turnDebuffs.buffs.Any(x => x.objectIdx.Any(y => y == (int)Obj.Stun));

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
                    else if (b.objectIdx[i] == (int)Obj.Cycle)
                        addPivot[0] += dungeonStat[(int)Obj.HP] * 0.2f;

        foreach (Buff b in turnDebuffs.buffs)
            if (b.type == BuffType.Stat)
                for (int i = 0; i < b.count; i++)
                    if (b.objectIdx[i] == (int)Obj.currHP)
                        addPivot[0] -= b.buffRate[i];
                    else if (b.objectIdx[i] == (int)Obj.Bleed)
                        addPivot[0] -= b.buffRate[i];
                    else if (b.objectIdx[i] == (int)Obj.Burn)
                        addPivot[0] -= Mathf.Min(0, buffStat[(int)Obj.DEF] - b.buffRate[i]);
                    else if (b.objectIdx[i] == (int)Obj.Curse)
                        addPivot[0] -= b.buffRate[i];
                    else if (b.objectIdx[i] == (int)Obj.Venom)
                        addPivot[0] -= Mathf.Min(0, buffStat[(int)Obj.DEF] - b.buffRate[i]);

        for (int i = 0; i < 2; i++)
            buffStat[2 * i + 1] = Mathf.Min(buffStat[2 * i + 2], Mathf.RoundToInt(buffStat[2 * i + 1] + addPivot[i]));
    }
    public void ShieldUpdate(float add = 0)
    {
        float max = 0;
        foreach (Buff b in turnBuffs.buffs)
            for (int i = 0; i < b.count; i++)
                if (b.objectIdx[i] == (int)Obj.Shield)
                    max += b.buffRate[i];

        shieldMax = Mathf.RoundToInt(max);
        shieldAmount = Mathf.Min(shieldAmount + Mathf.RoundToInt(add), shieldMax);
    }
    /*#endregion Buff */

    public virtual KeyValuePair<bool, int> GetDamage(Unit caster, float dmg, int pen, int crb)
    {
        float beforeRate = (float)buffStat[(int)Obj.currHP] / buffStat[(int)Obj.HP];
        
        //아이언하트 4세트 - 받는 피해 감소
        float ironHeartDEF = 1 - ItemManager.GetSetData(25).Value[2];

        float finalDEF = Mathf.Max(0, buffStat[(int)Obj.DEF] * (100 - pen) / 100f);
        int finalDmg = Mathf.RoundToInt(-dmg / Mathf.Max(1, Mathf.Log(finalDEF, caster.LVL + 1)) * ironHeartDEF * crb / 100);

        if (shieldAmount >= finalDmg)
            shieldAmount += finalDmg;
        else
        {
            finalDmg += shieldAmount;
            shieldAmount = 0;
            buffStat[(int)Obj.currHP] += finalDmg;
        }
        dmgs[2] -= finalDmg;
        caster.dmgs[0] -= finalDmg;
        //피격 시 차감되는 버프 처리

        LogManager.instance.AddLog(string.Concat(caster.name, "의 공격, ", name, "에게 ", finalDmg, "만큼 피해"));
        
        //아이언하트 3세트 - 체력 40% 이하로 떨어질 때 디버프 하나 해제
        if(ItemManager.GetSetData(25).Value[1] > 0 && (float)buffStat[(int)Obj.currHP] / buffStat[(int)Obj.HP] < 0.4f && beforeRate >= 0.4f)
            RemoveDebuff(1);

        bool killed = false;
        if (buffStat[(int)Obj.currHP] <= 0)
        {
            killed = true;
            gameObject.SetActive(false);

            if(implantBomb != null)
            {
                List<Unit> targets = BM.GetEffectTarget(6);

                foreach (Unit u in targets)
                    u.GetDamage(implantBomb.caster, implantBomb.dmg, implantBomb.pen, 100);
            }
        }

        return new KeyValuePair<bool, int>(killed, -finalDmg);
    }
    public virtual void GetHeal(float heal) => buffStat[(int)Obj.currHP] = Mathf.Min(buffStat[(int)Obj.HP], Mathf.RoundToInt(buffStat[(int)Obj.currHP] + heal));


    public virtual bool IsBoss() => false;
    public virtual void StatLoad() { }
    /* #endregion Function */
}