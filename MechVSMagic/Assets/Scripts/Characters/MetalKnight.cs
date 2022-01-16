using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class MetalKnight : Character
{
    //63 신속 사격
    int resentSkillCategory = 0;
    float coolRate = 1;
    public override void OnTurnStart()
    {
        base.OnTurnStart();
        resentSkillCategory = 0;
    }

    public override int GetSkillCost(Skill s)
    {
        //63 신속 사격
        if (s.idx == 63 && resentSkillCategory == 1004)
            return 1;

        float addPivot = 0, mulPivot = 1;

        foreach (APBuff b in apBuffs)
        {
            if (b.category == s.category || b.category == 0)
            {
                if (b.isMulti)
                    mulPivot *= b.rate;
                else
                    addPivot += b.rate;
            }
        }

        return Mathf.RoundToInt((s.apCost + addPivot) * mulPivot);
    }

    //44 빠르게 막기, 78 절대 방어
    List<GuardBuff> guardBuffList = new List<GuardBuff>();

    //55 방어구 파괴
    Dictionary<Unit, int> armorBreakCount = new Dictionary<Unit, int>();
    
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

        Passive_SkillCast(skill.category);

        //58 저격수
        if(skill.idx == 58 && selects[0].inbattleDebuffList.Any(x=>x.name == "표식"))
        {
            AddBuff(SkillManager.GetSkill(2, 58), 0, 0);
        }
        //63 신속 사격
        if(skill.idx == 63 && resentSkillCategory == 1004)
        {
            AddBuff(skill, 0, 0);
        }
        //69 상처 벌리기
        if(skill.idx == 69 && selects[0].inbattleDebuffList.Any(x=>x.objectIdx == 21))
        {
            AddBuff(skill, 0, 0);
            AddBuff(skill, 1, 0);
            foreach (Buff b in selects[0].inbattleDebuffList)
                if (b.objectIdx == 24)
                    b.duration++;
        }
        //79 현상금 사냥
        if(skill.idx == 79 && selects[0].inbattleDebuffList.Any(x=>x.name == "표식"))
        {
            inskillBuffList.Add(new Buff("", 1, 9, 1, 0, 100));
        }
        
        //skill 효과 순차적으로 계산
        Active_Effect(skill, selects);

        //79 현상금 사냥
        if (skill.idx == 79 && selects[0].inbattleDebuffList.Any(x => x.name == "표식"))
        {
            selects[0].inbattleDebuffList.RemoveAll(x => x.name == "표식");
            AddBuff(skill, 1, 0);
        }

        buffStat[(int)Obj.currAP]-= GetSkillCost(skill);
        resentSkillCategory = skill.category;
        cooldowns[idx] = Mathf.RoundToInt(coolRate * skill.cooldown);
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

                        //skillEffectRate가 기본적으로 음수
                        int dmg = Mathf.CeilToInt(buffStat[skill.effectStat[i]] * skill.effectRate[i]);

                        foreach(Unit u in effectTargets)
                        {
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

                                bool kill = u.GetDamage(this, dmg, buffStat[(int)Obj.PEN]);

                                //55 방어구 파괴, 73 난도질
                                if (HasSkill(55) || HasSkill(73))
                                {
                                    if (skill.category == 1004 || (HasSkill(83) && skill.category == 1005))
                                        if (armorBreakCount.ContainsKey(u))
                                        {
                                            armorBreakCount[u] += 1;
                                            if (HasSkill(55) && armorBreakCount[u] % 3 == 0)
                                            {
                                                u.AddDebuff(SkillManager.GetSkill(2, 55), 0, 0);
                                            }
                                            if (HasSkill(73) && armorBreakCount[u] % 5 == 0)
                                            {
                                                Skill s = SkillManager.GetSkill(2, 73);
                                                u.GetDamage(this, Mathf.RoundToInt(u.buffStat[(int)Obj.currHP] * s.effectRate[0]), buffStat[(int)Obj.PEN]);
                                                u.AddDebuff(s, 1, 0);

                                            }
                                        }
                                        else
                                            armorBreakCount.Add(u, 1);
                                }
                                //57 자신감
                                if (kill && HasSkill(57))
                                    AddBuff(SkillManager.GetSkill(2, 57), 0, 0);

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
                        //막기 버프
                        guardBuffList.Add(new GuardBuff(skill.name, skill.effectTurn[i], skill.idx == 78, skill.effectObject[i], skill.effectRate[i], skill.effectCalc[i] == 1));
                        break;
                    }
                case SkillType.CharSpecial2:
                    {
                        //표식 부여
                        if(isAcc)
                        {
                            effectTargets[0].inbattleDebuffList.Add(new Buff("표식", 1, (int)Obj.DEF, 1, 1, 20));
                            effectTargets[0].inbattleDebuffList.Add(new Buff("표식", 1, (int)Obj.DOG, 1, 1, 20));

                            //대상 무력화
                            if(HasSkill(82))
                            {
                                effectTargets[0].inbattleDebuffList.Add(new Buff("표식", 1, (int)Obj.ACC, 1, 1, 10));
                                effectTargets[0].inbattleDebuffList.Add(new Buff("표식", 1, (int)Obj.CRC, 1, 1, 10));
                                effectTargets[0].inbattleDebuffList.Add(new Buff("표식", 1, (int)Obj.SPD, 1, 1, 10));
                            }
                        }
                        break;
                    }
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

            //75 균형잡힌 전투법
            if (s.idx == 75)
            {
                if (activeIdxs.Count(x => SkillManager.GetSkill(2, x).category == 1004) == activeIdxs.Count(x => SkillManager.GetSkill(2, x).category == 1005))
                {
                    apBuffs.Add(new APBuff(s.name, s.effectTurn[0], 0, s.effectRate[0], s.effectCalc[0] == 1));
                }
                continue;
            }
            //84 한길만을 걷다
            if (s.idx == 84)
            {
                if (activeIdxs.Count(x => SkillManager.GetSkill(2, x).category == 1004) + activeIdxs.Count(x => x == 0) == 6 ||
                    activeIdxs.Count(x => SkillManager.GetSkill(2, x).category == 1005) + activeIdxs.Count(x => x == 0) == 6)
                {
                    AddBuff(s, 0, 0);
                    coolRate = 1 - s.effectRate[1];
                }
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
    protected override void Passive_SkillCast(int skillCategory)
    {
        for (int j = 0; j < passiveIdxs.Length; j++)
        {
            Skill skill = SkillManager.GetSkill(classIdx, passiveIdxs[j]);

            //65 칼 마무리
            if (skill.idx == 65 && skillCategory == 1004)
            {
                Skill tmp = SkillManager.GetSkill(classIdx, 65);
                inskillBuffList.Add(new Buff("", -1, 5, activeIdxs.Count(x => SkillManager.GetSkill(classIdx, x).category == 1005), tmp.effectCalc[0], tmp.effectRate[0]));
                continue;
            }
            //67 총 마무리
            if (skill.idx == 67 && skillCategory == 1005)
            {
                Skill tmp = SkillManager.GetSkill(2, 67);
                inskillBuffList.Add(new Buff("", -1, 5, activeIdxs.Count(x => SkillManager.GetSkill(2, x).category == 1004), tmp.effectCalc[0], tmp.effectRate[0]));
                continue;
            }

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
    protected override void Passive_SkillHit(int skillCategory)
    {
        for (int i = 0; i < passiveIdxs.Length; i++)
        {
            Skill s = SkillManager.GetSkill(classIdx, passiveIdxs[i]);

            //74 헤드샷
            if (s.idx == 74)
            {
                if (skillCategory == 1005 && isCrit)
                    AddBuff(s, 0, 0);
                continue;
            }
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

    protected override void StatUpdate_Turn()
    {
        float[] mulPivot = new float[13];
        float[] addPivot = new float[13];

        for (int i = 0; i < 13; i++)
        {
            mulPivot[i] = 1;
            addPivot[i] = 0;
        }

        CalcTurnBuffPivot(ref addPivot, ref mulPivot);
        CalcShieldBuffPivot(ref addPivot, ref mulPivot);

        for (int i = 0; i < 13; i++)
            if (i == 1 || i == 3)
                buffStat[i] = Mathf.Min(buffStat[i + 1], Mathf.RoundToInt(buffStat[i] + addPivot[i]));
            else
                buffStat[i] = Mathf.CeilToInt(dungeonStat[i] * mulPivot[i] + addPivot[i]);
    }
    protected override void StatUpdate_Skill(int skillCategory)
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
        CalcShieldBuffPivot(ref addPivot, ref mulPivot);

        for (int i = 0; i < 13; i++)
            if (i == 1 || i == 3)
                buffStat[i] = Mathf.Min(buffStat[i + 1], Mathf.RoundToInt(buffStat[i] + addPivot[i]));
            else
                buffStat[i] = Mathf.CeilToInt(dungeonStat[i] * mulPivot[i] + addPivot[i]);
    }
    void CalcShieldBuffPivot(ref float[] add, ref float[] mul)
    {
        foreach(GuardBuff s in guardBuffList)
        {
            if (s.isMulti)
                mul[s.objectIdx] += s.rate;
            else
                add[s.objectIdx] += s.rate;
        }
    }

    public override bool GetDamage(Unit caster, int dmg, int pen)
    {
        bool killed = base.GetDamage(caster, dmg, pen);

        if (guardBuffList.Count > 0)
        {
            //56 카운터 어택
            if (HasSkill(56))
                caster.AddDebuff(SkillManager.GetSkill(2, 56), 0, 0);
            //81 가드 아드레날린
            if (HasSkill(81))
                AddBuff(SkillManager.GetSkill(2, 81), 0, 0);
        }

        if (guardBuffList.Any(x => x.isReturn))
            caster.GetDamage(this, Mathf.Max(1, Mathf.RoundToInt(0.2f * dmg)), 999);

        for (int i = 0; i < guardBuffList.Count; i++)
        {
            guardBuffList[i].buffTime--;
            if (guardBuffList[i].buffTime == 0)
            {
                guardBuffList.RemoveAt(i);
                i--;
            }
        }

        return killed;
    }

}
