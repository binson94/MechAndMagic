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

        //235 대자연 - 시작, 최대 생명력 1 증가
        currVitality = 0;
        if(HasSkill(235))
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

        //251 활기의 고리
        if (turnBuffs.buffs.Any(x => x.name == SkillManager.GetSkill(classIdx, 251).name))
            HealVitality(1);

        //226 자생력 - 생명력 비례 회복
        if (HasSkill(226))
        {
            GetHeal(SkillManager.GetSkill(6, 226).effectRate[0] * currVitality);
            //243 증폭된 자생력 - 더 회복
            if (HasSkill(243))
                GetHeal(SkillManager.GetSkill(6, 243).effectRate[0] * currVitality * buffStat[(int)Obj.HP]);
        }
        //236 정화 작용
        if(HasSkill(236) && turnNum % 2 == 0 && turnDebuffs.Count > 0)
        {
            Buff b = (from x in turnDebuffs.buffs
                      where x.isDispel
                      select x).OrderBy(x => x.duration).First();
            turnBuffs.buffs.Remove(b);
        }
        //245 재생되는 갑옷
        if(HasSkill(245) && currVitality > 0)
        {
            AddBuff(this, orderIdx, SkillManager.GetSkill(6, 245), 0, 0);
            SpendVitality(1);
        }
        //246 파괴되는 갑옷
        if(HasSkill(246) && currVitality > 0)
        {
            BM.GetEffectTarget(7)[0].AddDebuff(this, orderIdx, SkillManager.GetSkill(6, 246), 0, 0);
            SpendVitality(1);
        }
        //254 마력 재생
        if (HasSkill(254) && currVitality >= 3)
        {
            bool[] iscool = new bool[activeIdxs.Length];
            for (int i = 0; i < cooldowns.Length; i++)
                iscool[i] = cooldowns[i] > 0;

            if (iscool.Any(x => x))
            {
                int idx = 0;
                while (!iscool[idx])
                    idx = Random.Range(0, cooldowns.Length);

                cooldowns[idx]--;

                SpendVitality(3);
            }
        }
    }
    public override void OnTurnEnd()
    {
        //233 평화주의자
        if(HasSkill(233) && dmgs[0] == 0)
            HealVitality((int)SkillManager.GetSkill(6, 145).effectRate[0]);
        //244 완벽한 상태
        if (HasSkill(244) && currVitality == maxVitality)
            AddBuff(this, orderIdx, SkillManager.GetSkill(6, 156), 0, 0);
        rooted = false;
    }

    public override string CanCastSkill(int idx)
    {
        Skill s = SkillManager.GetSkill(classIdx, activeIdxs[idx]);
        if (s.effectType[0] == 39 && currVitality < s.effectRate[0])
            return "생명력 부족";
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


        //215 내려치기 - 스피드 디버프 대상 공증
        if (skill.idx == 215 && selects[0].turnDebuffs.buffs.Any(x=>x.objectIdx.Any(y=>y == (int)Obj.SPD)))
            AddBuff(this, orderIdx, skill, 0, 0);
        //225 생명력 반환
        if (skill.idx == 225)
            buffStat[(int)Obj.currHP] -= (int)skill.effectRate[0];
        //237 숲의 기운 - 전체 공격 스킬 사용 시 생명력 회복
        if (HasSkill(237) && skill.effectTarget.Any(x => x == 6))
            HealVitality((int)SkillManager.GetSkill(6, 237).effectRate[0]);
        //240 활력의 타격 - 생명력 비례 공증
        if (skill.idx == 240)
            skillBuffs.Add(new Buff(BuffType.Stat, LVL, new BuffOrder(), "", skill.effectObject[0], currVitality, skill.effectRate[0], skill.effectCalc[0], skill.effectTurn[0]));            
        //247 뿌리내리기
        if (skill.idx == 247)
            rooted = true;
        //248 세계수의 힘
        if(skill.idx == 248)
        {
            skillBuffs.Add(new Buff(BuffType.Stat, LVL, new BuffOrder(), "", skill.effectObject[0], currVitality, skill.effectRate[0], skill.effectCalc[0], skill.effectTurn[0]));
            SpendVitality(currVitality);
        }
        //252 자연재해 - 디버프 스킬 사용 시 생명력 회복
        if (HasSkill(252) && skill.effectType.Any(x => x == (int)SkillType.Active_Debuff))
            HealVitality((int)SkillManager.GetSkill(6, 252).effectRate[0]);
        //skill 효과 순차적으로 계산
        Active_Effect(skill, selects);

        orderIdx++;
        buffStat[(int)Obj.currAP] -= GetSkillCost(skill);
        cooldowns[idx] = skill.cooldown;
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

                            //238 거인의 타격 - 적 속도 반비례 명중 상승
                            if (skill.idx == 238)
                                acc += Mathf.RoundToInt(skill.effectRate[2] / Mathf.Max(1, u.buffStat[(int)Obj.SPD]));
                            //명중 시
                            if (Random.Range(0, 100) < acc)
                            {
                                isAcc = true;
                                //크리티컬 연산 - dmg * CRB
                                isCrit = Random.Range(0, 100) < buffStat[(int)Obj.CRC];

                                bool death = u.GetDamage(this, dmg, buffStat[(int)Obj.PEN], isCrit ? buffStat[(int)Obj.CRB] : 100).Key;

                                //229 생명력 흡수
                                if(skill.idx == 229)
                                {
                                    int finalDEF = Mathf.Max(0, u.buffStat[(int)Obj.DEF] - buffStat[(int)Obj.PEN]);
                                    GetHeal(Mathf.Max(1, dmg - finalDEF) * 0.3f);
                                }
                                //234 자연의 순환
                                if (HasSkill(234) && death)
                                    HealVitality((int)SkillManager.GetSkill(6, 146).effectRate[0]);
                                //239 소멸
                                if (skill.idx == 239 && death)
                                    HealVitality(maxVitality);
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
                            foreach(Unit u in effectTargets)
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
                            //227 끈끈한 점액
                            if (HasSkill(227))
                                foreach (Unit u in effectTargets)
                                    u.AddDebuff(this, orderIdx, skill, i + 1, 0);
                            else
                                foreach (Unit u in effectTargets)
                                    u.AddDebuff(this, orderIdx, skill, i, 0);
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
    public override KeyValuePair<bool, int> GetDamage(Unit caster, float dmg, int pen, int crb)
    {
        KeyValuePair<bool, int> killed = base.GetDamage(caster, dmg, pen, crb);

        if(HasSkill(255) && killed.Key && revive == 0)
        {
            gameObject.SetActive(true);

            buffStat[(int)Obj.currHP] = 0;
            GetHeal(SkillManager.GetSkill(6, 255).effectRate[0] * buffStat[(int)Obj.HP]);
            LogManager.instance.AddLog("Revive");
            revive = 1;
            killed = new KeyValuePair<bool, int>(false, killed.Value);
        }

        return killed;
    }

    void HealVitality(int rate)
    {
        currVitality = Mathf.Min(maxVitality, currVitality + rate);
        if(HasSkill(253))
        {
            Skill s = SkillManager.GetSkill(6, 253);
            turnBuffs.buffs.RemoveAll(x => x.name == s.name);
            turnBuffs.Add(new Buff(BuffType.Stat, LVL, new BuffOrder(this, -3), s.name, s.effectObject[0], currVitality, s.effectRate[0], s.effectCalc[0], s.effectTurn[0], s.effectDispel[0], s.effectVisible[0]));
            turnBuffs.Add(new Buff(BuffType.Stat, LVL, new BuffOrder(this, -3), s.name, s.effectObject[1], currVitality, s.effectRate[1], s.effectCalc[1], s.effectTurn[1], s.effectDispel[1], s.effectVisible[1]));
        }
    }
    void SpendVitality(int rate)
    {
        currVitality = Mathf.Max(0, currVitality - rate);
        if (HasSkill(253))
        {
            Skill s = SkillManager.GetSkill(6, 253);
            turnBuffs.buffs.RemoveAll(x => x.name == s.name);

            if(currVitality > 0)
            {
                turnBuffs.Add(new Buff(BuffType.Stat, LVL, new BuffOrder(this, -3), s.name, s.effectObject[0], currVitality, s.effectRate[0], s.effectCalc[0], s.effectTurn[0], s.effectDispel[0], s.effectVisible[0]));
                turnBuffs.Add(new Buff(BuffType.Stat, LVL, new BuffOrder(this, -3), s.name, s.effectObject[1], currVitality, s.effectRate[1], s.effectCalc[1], s.effectTurn[1], s.effectDispel[1], s.effectVisible[1]));
            }
        }
    }   
}
