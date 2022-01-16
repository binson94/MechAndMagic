using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Druid : Character
{
    public int currVitality;
    public int maxVitality;

    public int turnNum = 0;
    public int revive = 0;
    bool rooted = false;

    public override void OnBattleStart(BattleManager BM)
    {
        base.OnBattleStart(BM);

        //147 대자연 - 시작, 최대 생명력 1 증가
        currVitality = 0;
        if(HasSkill(147))
        {
            maxVitality = 6;
            HealVitality(1);
        }
        else
            maxVitality = 5;
    }

    public override void OnTurnStart()
    {
        base.OnTurnStart();
        turnNum++;

        if(HasSkill(163))
        {
            if (inbattleBuffList.Any(x => x.name == "활기의 고리") && HasSkill(147))
                maxVitality = 7;
            else if (inbattleBuffList.Any(x => x.name == "활기의 고리"))
                maxVitality = 6;
            else maxVitality = 5;
        }

        //138 자생력 - 생명력 비례 회복
        if (HasSkill(138))
        {
            GetHeal(SkillManager.GetSkill(6, 138).effectRate[0] * currVitality);
            //155 증폭된 자생력 - 더 회복
            if (HasSkill(155))
                GetHeal(SkillManager.GetSkill(6, 155).effectRate[0] * currVitality * buffStat[(int)Obj.HP]);
        }
        //149 정화 작용
        if(HasSkill(148) && turnNum % 2 == 0 && inbattleDebuffList.Count > 0)
        {
            Buff b = (from x in inbattleDebuffList
                      where x.isDispel
                      select x).OrderBy(x => x.duration).First();
            inbattleDebuffList.Remove(b);
        }
        //157 재생되는 갑옷
        if(HasSkill(157) && currVitality > 0)
        {
            AddBuff(SkillManager.GetSkill(6, 157), 0, 0);
            SpendVitality(1);
        }
        //158 파괴되는 갑옷
        if(HasSkill(158) && currVitality > 0)
        {
            BM.GetEffectTarget(7)[0].AddDebuff(SkillManager.GetSkill(6, 158), 0, 0);
            SpendVitality(1);
        }
        //166 마력 재생
        if (HasSkill(166) && currVitality >= 3)
        {
            for (int i = 0; i < cooldowns.Length; i++)
                cooldowns[i] = Mathf.Max(0, cooldowns[i] - 1);
            SpendVitality(3);
        }
    }
    public override void OnTurnEnd()
    {
        //145 평화주의자
        if(HasSkill(145) && dmgs[0] == 0)
            HealVitality((int)SkillManager.GetSkill(6, 145).effectRate[0]);
        //156 완벽한 상태
        if (HasSkill(156) && currVitality == maxVitality)
            AddBuff(SkillManager.GetSkill(6, 156), 0, 0);
        rooted = false;
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

        Passive_SkillCast(skill.category);


        //127 내려치기 - 스피드 디버프 대상 공증
        if (skill.idx == 127 && selects[0].inbattleDebuffList.Any(x => x.objectIdx == 12))
            AddBuff(skill, 0, 0);
        //137 생명력 반환
        if (skill.idx == 137)
            buffStat[(int)Obj.currHP] -= (int)skill.effectRate[0];
        //149 숲의 기운 - 전체 공격 스킬 사용 시 생명력 회복
        if (HasSkill(149) && skill.effectTarget.Any(x => x == 6))
            HealVitality((int)SkillManager.GetSkill(6, 149).effectRate[0]);
        //152 활력의 타격 - 생명력 비례 공증
        if (skill.idx == 152)
            inskillBuffList.Add(new Buff("", 1, skill.effectObject[0], currVitality, skill.effectCalc[0], skill.effectRate[0]));
        //159 뿌리내리기
        if (skill.idx == 159)
            rooted = true;
        //160 세계수의 힘
        if(skill.idx == 160)
        {
            inskillBuffList.Add(new Buff("", 1, skill.effectObject[0], currVitality, skill.effectCalc[0], skill.effectRate[0]));
            SpendVitality(currVitality);
        }
        //163 활기의 고리
        if (skill.idx == 163)
            maxVitality++;
        //164 자연재해 - 디버프 스킬 사용 시 생명력 회복
        if (HasSkill(164) && skill.effectType.Any(x => x == 12))
            HealVitality((int)SkillManager.GetSkill(6, 164).effectRate[0]);
        //skill 효과 순차적으로 계산
        Active_Effect(skill, selects);

        buffStat[(int)Obj.currAP] -= GetSkillCost(skill);
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

                        damaged.Clear();
                        foreach (Unit u in effectTargets)
                        {
                            if (!u.isActiveAndEnabled)
                                continue;

                            //명중 연산 - 최소 명중률 10%
                            int acc = Mathf.Max(buffStat[(int)Obj.ACC] - u.buffStat[(int)Obj.DOG], 10);
                            if (skill.idx == 150)
                                acc = Mathf.Max(10, buffStat[(int)Obj.ACC] - u.buffStat[(int)Obj.DOG] 
                                                    + Mathf.Max(0, Mathf.RoundToInt((100 - u.buffStat[(int)Obj.SPD]) * skill.effectRate[2])));
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

                                bool death = u.GetDamage(this, dmg, buffStat[(int)Obj.PEN]);

                                //141 생명력 흡수
                                if(skill.idx == 141)
                                {
                                    int finalDEF = Mathf.Max(0, u.buffStat[(int)Obj.DEF] - buffStat[(int)Obj.PEN]);
                                    GetHeal(Mathf.Max(1, dmg - finalDEF) * 0.3f);
                                }
                                //146 자연의 순환
                                if (HasSkill(146) && death)
                                    HealVitality((int)SkillManager.GetSkill(6, 146).effectRate[0]);
                                //151 소멸
                                if (HasSkill(151) && death)
                                    HealVitality(maxVitality);
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
                        //생명력 소모
                        SpendVitality((int)skill.effectRate[i]);
                        break;
                    }
                case SkillType.CharSpecial2:
                    {
                        //생명력 회복
                        HealVitality((int)skill.effectRate[i]);
                        break;
                    }
                case SkillType.CharSpecial3:
                    {
                        //강화 가능 속도 감소 디버프
                        if (skill.effectCond[i] == 0 || skill.effectCond[i] == 1 && isAcc || skill.effectCond[i] == 2 && isCrit)
                        {
                            if (HasSkill(139))
                                foreach (Unit u in effectTargets)
                                    u.AddDebuff(skill, i + 1, 0);
                            else
                                foreach (Unit u in effectTargets)
                                    u.AddDebuff(skill, i, 0);
                        }
                        break;
                    }
                default:
                    break;
            }
        }
    }

    public override void GetHeal(float heal)
    {
        if (rooted)
            heal *= 1.3f;
        buffStat[(int)Obj.currHP] = Mathf.Min(buffStat[(int)Obj.HP], Mathf.RoundToInt(buffStat[(int)Obj.currHP] + heal));
    }
    public override bool GetDamage(Unit caster, int dmg, int pen)
    {
        bool killed = base.GetDamage(caster, dmg, pen);

        if(HasSkill(167) && killed && revive == 0)
        {
            gameObject.SetActive(true);

            buffStat[(int)Obj.currHP] = 0;
            GetHeal(SkillManager.GetSkill(6, 167).effectRate[0] * buffStat[(int)Obj.HP]);
            LogManager.instance.AddLog("Revive");
            revive = 1;
            killed = false;
        }

        return killed;
    }

    void HealVitality(int rate)
    {
        currVitality = Mathf.Min(maxVitality, currVitality + rate);
        if(HasSkill(165))
        {
            Skill s = SkillManager.GetSkill(6, 165);
            inbattleBuffList.RemoveAll(x => x.name == s.name);
            inbattleBuffList.Add(new Buff(s.name, s.effectTurn[0], s.effectObject[0], currVitality, s.effectCalc[0], s.effectRate[0], s.effectDispel[0], s.effectVisible[0]));
            inbattleBuffList.Add(new Buff(s.name, s.effectTurn[1], s.effectObject[1], currVitality, s.effectCalc[1], s.effectRate[1], s.effectDispel[1], s.effectVisible[1]));
        }
    }
    void SpendVitality(int rate)
    {
        currVitality = Mathf.Max(0, currVitality - rate);
        {
            Skill s = SkillManager.GetSkill(6, 165);
            inbattleBuffList.RemoveAll(x => x.name == s.name);
            inbattleBuffList.Add(new Buff(s.name, s.effectTurn[0], s.effectObject[0], currVitality, s.effectCalc[0], s.effectRate[0], s.effectDispel[0], s.effectVisible[0]));
            inbattleBuffList.Add(new Buff(s.name, s.effectTurn[1], s.effectObject[1], currVitality, s.effectCalc[1], s.effectRate[1], s.effectDispel[1], s.effectVisible[1]));
        }
    }
    
}
