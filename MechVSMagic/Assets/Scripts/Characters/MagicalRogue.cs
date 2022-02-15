using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class MagicalRogue : Character
{
    int resentCategory = 0;

    bool cast340 = false;

    //341, 342 1, 2형 교활함 스킬 사용 수 저장
    int[] skillCount = new int[2];

    public override void OnTurnStart()
    {
        base.OnTurnStart();
        resentCategory = 0;

        if(cast340)
            AddBuff(this, orderIdx, SkillManager.GetSkill(classIdx, 340), 2, 0);
        cast340 = false;
    }

    public override string CanCastSkill(int idx)
    {
        Skill s = SkillManager.GetSkill(classIdx, activeIdxs[idx]);
        if (s.category == 1020 && resentCategory != 1019)
            return "1형 스킬 다음에 시전";
        else if (s.category == 1021 && resentCategory != 1020)
            return "2형 스킬 다음에 시전";
        else
            return base.CanCastSkill(idx);
    }
    public override void ActiveSkill(int idx, List<Unit> selects)
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

        //321 맹독 부여
        if(HasSkill(321))
        {
            Buff b = turnBuffs.buffs.Find(x => x.name == SkillManager.GetSkill(classIdx, 321).name);
            if (b != null)
            {
                b.buffRate[0]--;
                if (b.buffRate[0] <= 0)
                    turnBuffs.buffs.Remove(b);
            }
        }
        //340 3형:어둠 속으로 - 다음 턴 치확 상승
        if (skill.idx == 340)
            cast340 = true;

        //344 그림자 돌진
        if (HasSkill(344)) turnBuffs.buffs.RemoveAll(x => x.name == SkillManager.GetSkill(classIdx, 344).name);

        orderIdx++;
        buffStat[(int)Obj.currAP] -= GetSkillCost(skill);
        if (skill.effectType.Any(x => x == (int)SkillType.CharSpecial2))
            buffStat[(int)Obj.currAP] += 2;

        cooldowns[idx] = skill.cooldown;
        resentCategory = skill.category;

        if (skill.category == 1019)
            skillCount[0]++;
        else if (skill.category == 1020)
            skillCount[1]++;

        if(HasSkill(341) && skillCount[0] >= 3)
        {
            AddBuff(this, -1, SkillManager.GetSkill(classIdx, 341), 0, 0);
            skillCount[0] = 0;
        }
        if(HasSkill(342) && skillCount[1] >= 2)
        {
            AddBuff(this, -1, SkillManager.GetSkill(classIdx, 342), 0, 0);
            skillCount[1] = 0;
        }
    }
    protected override void Active_Effect(Skill skill, List<Unit> selects)
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
                        foreach (Unit u in effectTargets)
                        {
                            if (!u.isActiveAndEnabled)
                                continue;

                            //명중 연산 - 최소 명중률 10%
                            int acc = 20;
                            if (buffStat[(int)Obj.ACC] >= u.buffStat[(int)Obj.DOG])
                                acc = 60 + 6 * (buffStat[(int)Obj.ACC] - u.buffStat[(int)Obj.DOG]) / (u.LVL + 2);
                            else
                                acc = Mathf.Max(acc, 60 + 6 * (buffStat[(int)Obj.ACC] - u.buffStat[(int)Obj.DOG]) / (LVL + 2));
                            //명중 시
                            if (Random.Range(0, 100) < acc)
                            {
                                isAcc = true;
                                //329 깊이 찌르기
                                int crcAdd = u.turnDebuffs.buffs.Any(x => x.name == SkillManager.GetSkill(classIdx, 329).name) ? 20 : 0;
                                //크리티컬 연산 - dmg * CRB
                                isCrit = Random.Range(0, 100) < buffStat[(int)Obj.CRC] + crcAdd;

                                int heal = u.GetDamage(this, dmg, buffStat[(int)Obj.PEN], isCrit ? buffStat[(int)Obj.CRB] : 100).Value;
                                //348 환골탈태 - 생명력 흡수
                                if (turnBuffs.buffs.Any(x => x.name == SkillManager.GetSkill(classIdx, 348).name))
                                    GetHeal(heal * 0.1f);
                                //321 맹독 부여
                                if (turnBuffs.buffs.Any(x => x.name == SkillManager.GetSkill(classIdx, 321).name))
                                    u.turnDebuffs.Add(new Buff(BuffType.Stat, LVL, new BuffOrder(this, orderIdx), "맹독", (int)Obj.Venom, buffStat[(int)Obj.ATK], 0.6f, 0, 2, 1, 1));

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
                                u.AddDebuff(this, orderIdx, skill, i, stat);
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
                case SkillType.CharSpecial1:
                    {
                        //TP 감소
                        BM.ReduceTP(effectTargets, Mathf.RoundToInt(skill.effectRate[i]));
                        return;
                    }
                case SkillType.CharSpecial3:
                    {
                        Unit u = effectTargets[0];

                        for (int j = 0; j < 10; i++)
                        {
                            StatUpdate_Skill(skill);

                            float dmg = buffStat[skill.effectStat[i]] * skill.effectRate[i];

                            if (!u.isActiveAndEnabled)
                                break;

                            //명중 연산 - 최소 명중률 10%
                            int acc = 20;
                            if (buffStat[(int)Obj.ACC] >= u.buffStat[(int)Obj.DOG])
                                acc = 60 + 6 * (buffStat[(int)Obj.ACC] - u.buffStat[(int)Obj.DOG]) / (u.LVL + 2);
                            else
                                acc = Mathf.Max(acc, 60 + 6 * (buffStat[(int)Obj.ACC] - u.buffStat[(int)Obj.DOG]) / (LVL + 2));
                            //명중 시
                            if (Random.Range(0, 100) < acc)
                            {
                                isAcc = true;
                                int crcAdd = u.turnDebuffs.buffs.Any(x => x.name == SkillManager.GetSkill(classIdx, 329).name) ? 20 : 0;
                                //크리티컬 연산 - dmg * CRB
                                
                                isCrit = Random.Range(0, 100) < buffStat[(int)Obj.CRC] + crcAdd;

                                int heal = u.GetDamage(this, dmg, buffStat[(int)Obj.PEN], isCrit ? buffStat[(int)Obj.CRB] : 100).Value;
                                //348 환골탈태 - 생명력 흡수
                                if (turnBuffs.buffs.Any(x => x.name == SkillManager.GetSkill(classIdx, 348).name))
                                    GetHeal(heal * 0.1f);
                                //321 맹독 부여
                                if (turnBuffs.buffs.Any(x => x.name == SkillManager.GetSkill(classIdx, 321).name))
                                    u.turnDebuffs.Add(new Buff(BuffType.Stat, LVL, new BuffOrder(this, orderIdx), "맹독", (int)Obj.Venom, buffStat[(int)Obj.ATK], 0.6f, 0, 2, 1, 1));

                                damaged.Add(u);

                                Passive_SkillHit(skill);
                            }
                            else
                            {
                                isAcc = false;
                                LogManager.instance.AddLog("Dodge");
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
}
