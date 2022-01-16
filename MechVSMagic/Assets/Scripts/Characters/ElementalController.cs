using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class ElementalController : Character
{
    int resentCategory = 0;
    bool[] elementalUsed = new bool[3];
    int usedAP = 0;

    public override void OnBattleStart(BattleManager BM)
    {
        base.OnBattleStart(BM);
        usedAP = 0;
    }

    public override void OnTurnStart()
    {
        base.OnTurnStart();
        for (int i = 0; i < 3; i++)
            elementalUsed[i] = false;
    }

    public override void ActiveSkill(int idx, List<Unit> selects)
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

        //112 불안정한 타격
        if(skill.idx == 112)
        {
            //불 -> 무조건 치명
            if (resentCategory == 1007)
                inskillBuffList.Add(new Buff("", -1, 9, 999, 0, 1));
            //물 -> 무조건 명중
            else if(resentCategory == 1008)
                inskillBuffList.Add(new Buff("", -1, 7, 999, 0, 1));
        }
        
        Passive_SkillCast(skill.category);

        //skill 효과 순차적으로 계산
        Active_Effect(skill, selects);

        if (skill.category == 1007)
            elementalUsed[0] = true;
        else if (skill.category == 1008)
            elementalUsed[1] = true;
        else if (skill.category == 1009)
            elementalUsed[2] = true;

        if (1007 <= skill.category && skill.category <= 1009)
            usedAP += GetSkillCost(skill);

        int tmp = resentCategory;
        resentCategory = skill.category;

        //112 불안정한 타격 - 바람 -> AP 소모 반환
        if (skill.idx != 112 || tmp != 1009)
            buffStat[(int)Obj.currAP] -= GetSkillCost(skill);

        if (HasSkill(123) && skill.category == 1011)
        {
            apBuffs.RemoveAll(x => x.isTurn == false);
            Skill s = SkillManager.GetSkill(5, 123);
            apBuffs.Add(new APBuff(s.name, s.effectTurn[0], s.effectCond[0], s.effectRate[0], s.effectCalc[0] == 1, false));
        }

        //124 정령왕의 계약
        if (HasSkill(124) && ((94 <= skill.idx && skill.idx <= 96) || (109 <= skill.idx && skill.idx <= 111)))
            cooldowns[idx] = Mathf.RoundToInt(skill.cooldown * 0.5f);
        else
            cooldowns[idx] = skill.cooldown;
    }
    protected override void Active_Effect(Skill skill, List<Unit> selects)
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
                else if (skill.effectStat[i] == 19)
                    rate = inbattleBuffList.Count;
                else if (skill.effectStat[i] == 20)
                    rate = selects[0].inbattleDebuffList.Count;
                else if (skill.effectStat[i] == 21)
                    rate = selects[0].buffStat[(int)Obj.HP];
                else if (skill.effectStat[i] == 24)
                    rate = buffStat[(int)Obj.ATK] * 0.15f;
                else if (skill.effectStat[i] == 25)
                    rate = buffStat[(int)Obj.ATK] * 0.7f;
            }

            switch ((SkillType)skill.effectType[i])
            {
                //데미지 - 스킬 버프 계산 후 
                case SkillType.Damage:
                    {
                        StatUpdate_Skill(skill.category);

                        int dmg = Mathf.CeilToInt(buffStat[skill.effectStat[i]] * skill.effectRate[i]);

                        //105 원소 결합
                        if (skill.idx == 105)
                            dmg = Mathf.RoundToInt(dmg * 0.5f * (1 + elementalUsed.Count(x => x)));
                        //120 응축된 원소
                        else if (skill.idx == 120)
                            dmg = Mathf.RoundToInt(dmg * usedAP / 5f);

                        damaged.Clear();
                        foreach (Unit u in effectTargets)
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
                case SkillType.CharSpecial1:
                    {
                        BM.SummonElemental(this, skill.category);
                        break;
                    }
                case SkillType.CharSpecial2:
                    {
                        BM.UpgradeElemental(this, skill.category);
                        break;
                    }
                case SkillType.CharSpecial3:
                    {
                        int type = BM.SacrificeElemental(this, skill);
                        switch (type)
                        {
                            //불 - 적 하나 즉사(보스는 데미지)
                            case 1007:
                                {
                                    Unit target = BM.GetEffectTarget(4)[0];

                                    if (target.IsBoss())
                                    {
                                        int dmg = Mathf.CeilToInt(buffStat[skill.effectStat[1]] * skill.effectRate[1]);

                                        //명중 연산 - 최소 명중률 10%
                                        int acc = Mathf.Max(buffStat[(int)Obj.ACC] - target.buffStat[(int)Obj.DOG], 10);
                                        //명중 시
                                        if (Random.Range(0, 100) < acc)
                                        {
                                            //크리티컬 연산 - dmg * CRB
                                            if (Random.Range(0, 100) < buffStat[(int)Obj.CRC])
                                            {
                                                dmg = Mathf.CeilToInt(dmg * (buffStat[(int)Obj.CRB] / 100f));
                                            }

                                            target.GetDamage(this, dmg, buffStat[(int)Obj.PEN]);

                                            Passive_SkillHit(skill.category);
                                        }
                                        else
                                        {
                                            LogManager.instance.AddLog("Dodge");
                                        }

                                        break;
                                    }
                                    else
                                    {
                                        target.GetDamage(this, 9999, 999);
                                    }
                                    break;
                                }
                            //물 - 2턴 피해 면역
                            case 1008:
                                {
                                    inbattleBuffList.Add(new Buff("고귀한 희생", 2, 0, 0, 0, 0, 1, 1));
                                    break;
                                }
                            //바람 - 적 전체 데미지, 맞은 적 TP 0으로
                            case 1009:
                                {
                                    List<Unit> targets = BM.GetEffectTarget(6);

                                    int dmg = Mathf.CeilToInt(buffStat[skill.effectStat[2]] * skill.effectRate[2]);

                                    foreach (Unit u in targets)
                                    {
                                        if (!u.isActiveAndEnabled)
                                            continue;

                                        //명중 연산 - 최소 명중률 10%
                                        int acc = Mathf.Max(buffStat[(int)Obj.ACC] - u.buffStat[(int)Obj.DOG], 10);
                                        //명중 시
                                        if (Random.Range(0, 100) < acc)
                                        {
                                            //크리티컬 연산 - dmg * CRB
                                            if (Random.Range(0, 100) < buffStat[(int)Obj.CRC])
                                            {
                                                dmg = Mathf.CeilToInt(dmg * (buffStat[(int)Obj.CRB] / 100f));
                                            }

                                            u.GetDamage(this, dmg, buffStat[(int)Obj.PEN]);
                                            damaged.Add(u);

                                            Passive_SkillHit(skill.category);
                                        }
                                        else
                                        {
                                            LogManager.instance.AddLog("Dodge");
                                        }
                                    }

                                    BM.Sacrifice_TP(damaged);

                                    break;
                                }
                        }
                        break;
                    }
                default:
                    break;
            }
        }
    }

    protected override void Passive_BattleStart()
    {
        List<Unit> effectTargets;

        for (int j = 0; j < 3; j++)
        {
            Skill s = SkillManager.GetSkill(classIdx, passiveIdxs[j]);
            if (s == null)
                continue;

            if (s.idx == 122)
            {
                if (activeIdxs.Count(x => SkillManager.GetSkill(5, activeIdxs[x]).category == 1007) + activeIdxs.Count(x => x == 0) == 6 ||
                    activeIdxs.Count(x => SkillManager.GetSkill(5, activeIdxs[x]).category == 1008) + activeIdxs.Count(x => x == 0) == 6 ||
                    activeIdxs.Count(x => SkillManager.GetSkill(5, activeIdxs[x]).category == 1009) + activeIdxs.Count(x => x == 0) == 6)
                    AddBuff(s, 0, 0);

                continue;
            }

            for (int i = 0; i < s.effectCount; i++)
            {
                switch (s.effectTarget[i])
                {
                    case 0:
                        effectTargets = new List<Unit>();
                        effectTargets.Add(this);
                        break;
                    default:
                        effectTargets = BM.GetEffectTarget(s.effectTarget[i]);
                        break;
                }

                switch ((SkillType)s.effectType[i])
                {
                    case SkillType.Passive_HasSkillBuff:
                        {
                            if (HasSkill(s.effectCond[i], true))
                                foreach (Unit u in effectTargets)
                                    u.AddBuff(s, i, 0);
                            break;
                        }
                    case SkillType.Passive_HasSkillDebuff:
                        {
                            if (HasSkill(s.effectCond[i], true))
                                foreach (Unit u in effectTargets)
                                    u.AddDebuff(s, i, 0);
                            break;
                        }
                    case SkillType.Passive_EternalBuff:
                        {
                            foreach (Unit u in effectTargets)
                                u.AddBuff(s, i, 0);
                            break;
                        }
                    case SkillType.Passive_EternalDebuff:
                        {
                            foreach (Unit u in effectTargets)
                                u.AddDebuff(s, i, 0);
                            break;
                        }
                    case SkillType.Passive_APBuff:
                        apBuffs.Add(new APBuff(s.name, s.effectTurn[i], s.effectCond[i], s.effectRate[i], s.effectCalc[i] == 1));
                        break;
                    default:
                        break;
                }
            }
        }
    }
    public override bool GetDamage(Unit caster, int dmg, int pen)
    {
        if(inbattleBuffList.Any(x=>x.name == "고귀한 희생"))
        {
            LogManager.instance.AddLog("invincible");
            return false;
        }    

        return base.GetDamage(caster, dmg, pen);
    }
}
