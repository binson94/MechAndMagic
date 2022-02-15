using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class VisionMaster : Character
{
    int resentCategory = 0;
    int[] skillCount_battle = new int[3];
    int[] skillCount_turn = new int[3];

    //0 : 양, 1 : 음, 2 : 둘 다
    public int skillState = 0;
    int bothskill;

    public override void OnTurnStart()
    {
        base.OnTurnStart();
        for (int i = 0; i < 3; i++) skillCount_turn[i] = 0;

        if (turnBuffs.buffs.Any(x => x.name == SkillManager.GetSkill(classIdx, 294).name))
            shieldAmount = shieldMax;
    }
    public override void OnTurnEnd()
    {
        base.OnTurnEnd();
        //297 플러스 비전 - 이번 턴에 양 스킬만 사용 시 다음 턴에 관통 버프
        if (HasSkill(297) && skillCount_turn[1] == 0)
            AddBuff(this, ++orderIdx, SkillManager.GetSkill(classIdx, 297), 0, 0);
        //298 마이너스 비전 - 이번 턴에 음 스킬만 사용 시 다음 턴에 명중 버프
        if (HasSkill(298) && skillCount_turn[0] == 0)
            AddBuff(this, ++orderIdx, SkillManager.GetSkill(classIdx, 298), 0, 0);
        //299 플랫 비전 - 이번 턴에 양, 음 스킬 사용 수 같을 시, 다음 턴 올 때까지 방어력 상승
        if(HasSkill(299) && skillCount_turn[0] == skillCount_turn[1])
            AddBuff(this, ++orderIdx, SkillManager.GetSkill(classIdx, 299), 0, 0);
    }

    public override string CanCastSkill(int idx)
    {
        if (cooldowns[idx] > 0)
            return "쿨다운";
        if(skillState < 2)
        {
            if (buffStat[(int)Obj.currAP] < GetSkillCost(SkillManager.GetSkill(classIdx, activeIdxs[idx] + skillState)))
                return "AP 부족";
            return "";
        }
        else
        {
            if (buffStat[(int)Obj.currAP] < Mathf.Min(GetSkillCost(SkillManager.GetSkill(classIdx, activeIdxs[idx])), GetSkillCost(SkillManager.GetSkill(classIdx, activeIdxs[idx] + 1))))
                return "AP 부족";
            return "";
        }
    }

    public override int GetSkillCost(Skill s)
    {
        return base.GetSkillCost(s) - (HasSkill(286) && resentCategory != s.category ? 1 : 0);
    }

    public void ActiveSkill_Both(int idx, List<Unit> selects)
    {
        bothskill = 0;
        ActiveSkill(idx, selects);
        bothskill = 1;
        ActiveSkill(idx, selects);

        buffStat[(int)Obj.currAP] -= Mathf.Min(GetSkillCost(SkillManager.GetSkill(classIdx, activeIdxs[idx])), GetSkillCost(SkillManager.GetSkill(classIdx, activeIdxs[idx] + 1)));
        cooldowns[idx] = Mathf.Min(SkillManager.GetSkill(classIdx, activeIdxs[idx]).cooldown, SkillManager.GetSkill(classIdx, activeIdxs[idx] + 1).cooldown);
    }
    public override void ActiveSkill(int idx, List<Unit> selects)
    {
        //적중 성공 여부
        isAcc = true;
        //크리티컬 성공 여부
        isCrit = false;

        Skill skill;

        //skillDB에서 스킬 불러오기
        if (skillState < 2)
            skill = SkillManager.GetSkill(classIdx, activeIdxs[idx] + skillState);
        else
            skill = SkillManager.GetSkill(classIdx, activeIdxs[idx] + bothskill);


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

        //양 스킬
        if (skill.category == 1023)
        {
            skillCount_turn[0]++;
            skillCount_battle[0]++;
        }
        if (skill.category == 1024)
        {
            skillCount_turn[1]++;
            skillCount_battle[1]++;
        }
        if(skill.category != 1023 || skillState != 2)
        {
            skillCount_turn[2]++;
            skillCount_battle[2]++;
        }

        //275 양의 생명력 - 양 스킬 3번마다 힐
        if(HasSkill(275) && skill.category == 1023 && skillCount_battle[0] % 3 == 0)
        {
            Skill tmp = SkillManager.GetSkill(classIdx, 275);
            GetHeal(buffStat[tmp.effectStat[0]] * tmp.effectRate[0]);
        }
        //276 음의 생명력 - 음 스킬 3번마다 데미지
        if(HasSkill(276) && skill.category == 1024 && skillCount_battle[1] % 3 == 0)
        {
            Skill tmp = SkillManager.GetSkill(classIdx, 276);
            Unit u = BM.GetEffectTarget(4)[0];

            StatUpdate_Turn();
            u.GetDamage(this, buffStat[tmp.effectStat[0]] * tmp.effectRate[0], buffStat[(int)Obj.PEN], 100);
        }
        //277 넘치는 생명력 - 아무 스킬 5번마다 다음 턴 AP 상승 버프
        if (HasSkill(277) && skillCount_battle[2] % 5 == 0 && !(skill.category == 1023 && skillState == 2))
        {
            AddBuff(this, ++orderIdx, SkillManager.GetSkill(classIdx, 277), 0, 0);
        }

        //287 부조화 - 직전 스킬과 카테고리 다르면 무작위 방어력 디버프
        if (HasSkill(287) && resentCategory != skill.category)
            BM.GetEffectTarget(4)[0].AddDebuff(this, ++orderIdx, SkillManager.GetSkill(classIdx, 287), 0, 0);
        //288 일치 - 직전 스킬과 카테고리 같으면 힐
        if (HasSkill(288) && resentCategory == skill.category)
        {
            Skill tmp = SkillManager.GetSkill(classIdx, 288);
            GetHeal(buffStat[tmp.effectStat[0]] * tmp.effectRate[0]);
        }

        //309 달빛 낙하 - 음 스킬 6번 사용 시마다 모든 적 명중 디버프
        if(HasSkill(309) && skill.category == 1024 && skillCount_battle[1] % 6 == 0)
        {
            Skill tmp = SkillManager.GetSkill(classIdx, 309);
            ++orderIdx;
            List<Unit> t = BM.GetEffectTarget(6);
            foreach (Unit u in t)
                u.AddDebuff(this, orderIdx, tmp, 0, 0);
        }

        orderIdx++;
        resentCategory = skill.category;
        if(skillState != 2)
        {
            buffStat[(int)Obj.currAP] -= GetSkillCost(skill);
            cooldowns[idx] = skill.cooldown;
        }

        if (HasSkill(310) && skillCount_battle[2] % 7 == 0)
            skillState = 2;
        else
            skillState = 0;
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

                            int acc = 20;
                            if (buffStat[(int)Obj.ACC] >= u.buffStat[(int)Obj.DOG])
                                acc = 60 + 6 * (buffStat[(int)Obj.ACC] - u.buffStat[(int)Obj.DOG]) / (u.LVL + 2);
                            else
                                acc = Mathf.Max(acc, 60 + 6 * (buffStat[(int)Obj.ACC] - u.buffStat[(int)Obj.DOG]) / (LVL + 2));

                            //명중 시
                            if (Random.Range(0, 100) < acc)
                            {
                                isAcc = true;
                                //크리티컬 연산 - dmg * CRB
                                int crit = (HasSkill(308) && skill.category == 1023 && skillCount_battle[0] % 5 == 0) ? 0 : Random.Range(0, 100);
                                isCrit = crit < buffStat[(int)Obj.CRC];

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

    protected override void StatUpdate_Skill(Skill s)
    {
        float[] mulPivot = new float[13];
        float[] addPivot = new float[13];

        for (int i = 0; i < 13; i++)
        {
            mulPivot[i] = 1;
            addPivot[i] = 0;
        }

        turnBuffs.GetBuffRate(ref addPivot, ref mulPivot, true);
        //302, 303 비전 대폭격 - 버프 2배로 적용
        if (s.idx == 302 || s.idx == 303)
            turnBuffs.GetBuffRate(ref addPivot, ref mulPivot, true);

        turnDebuffs.GetBuffRate(ref addPivot, ref mulPivot, false);
        skillBuffs.GetBuffRate(ref addPivot, ref mulPivot, true);
        skillDebuffs.GetBuffRate(ref addPivot, ref mulPivot, false);

        for (int i = 0; i < 13; i++)
            if (i != 1 && i != 3)
                buffStat[i] = Mathf.CeilToInt(dungeonStat[i] * mulPivot[i] + addPivot[i]);

    }
}