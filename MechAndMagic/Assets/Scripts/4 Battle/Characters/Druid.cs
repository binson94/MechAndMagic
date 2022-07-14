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

    bool[] cycleSet = new bool[3];

    public override void OnBattleStart(BattleManager BM)
    {
        base.OnBattleStart(BM);

        //235 대자연 - 시작, 최대 생명력 1 증가
        currVitality = 0;
        if (HasSkill(235))
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
        KeyValuePair<string, float[]> set = ItemManager.GetSetData(17);

        //251 활기의 고리
        if (turnBuffs.buffs.Any(x => x.name == SkillManager.GetSkill(classIdx, 251).name))
            HealVitality(1);

        //226 자생력 - 생명력 비례 회복
        if (HasSkill(226))
        {
            //243 증폭된 자생력 - 더 회복
            if (HasSkill(243))
                GetHeal(SkillManager.GetSkill(6, 243).effectRate[0] * currVitality);
            else
                GetHeal(SkillManager.GetSkill(6, 226).effectRate[0] * currVitality);

            //자연의 선물 2세트 - 자생력 발동 시 무작위 디버프 1개 해제
            if (set.Value[0] > 0)
                RemoveDebuff(1);
        }
        //236 정화 작용
        if (HasSkill(236) && (set.Value[1] > 0 || turnNum % 2 == 0) && turnDebuffs.Count > 0)
        {
            //자연의 선물 3세트 - 정화 작용 매 턴 발동, 무작위 적 버프 1개 해제
            Buff b = (from x in turnDebuffs.buffs
                      where x.isDispel
                      select x).OrderBy(x => x.duration).First();
            turnBuffs.buffs.Remove(b);

            if (set.Value[1] > 0)
                GetEffectTarget(null, null, 4)[0].RemoveBuff(1);
        }
        //245 재생되는 갑옷
        if (HasSkill(245) && currVitality > 0)
        {
            AddBuff(this, orderIdx, SkillManager.GetSkill(6, 245), 0, 0);
            SpendVitality(1);
        }
        //246 파괴되는 갑옷
        if (HasSkill(246) && currVitality > 0)
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
        if (HasSkill(233) && dmgs[0] == 0)
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

        LogManager.instance.AddLog($"{name}(이)가 {skill.name}(을)를 시전했습니다.");
        Passive_SkillCast(skill);

        KeyValuePair<string, float[]> set = ItemManager.GetSetData(18);
        if (skill.idx == 225 && set.Value[0] > 0 && cycleSet[0])
        {
            cycleSet[0] = false;
            skillBuffs.Add(new Buff(BuffType.Stat, BuffOrder.Default, "", (int)Obj.공격력, 1, set.Value[0], 1, -1));
        }
        if (skill.idx == 229 && set.Value[1] > 0 && cycleSet[1])
        {
            cycleSet[1] = false;
            skillBuffs.Add(new Buff(BuffType.Stat, BuffOrder.Default, "", (int)Obj.공격력, 1, set.Value[1], 1, -1));
        }


        set = ItemManager.GetSetData(16);
        //자연의 응징 2세트 - 전체 공격 스킬 ATK, ACC 상승
        if (skill.category == 1014 && set.Value[0] > 0)
        {
            skillBuffs.Add(new Buff(BuffType.Stat, BuffOrder.Default, "", (int)Obj.공격력, 1, set.Value[0], 1, -1));
            skillBuffs.Add(new Buff(BuffType.Stat, BuffOrder.Default, "", (int)Obj.명중률, 1, set.Value[0], 1, -1));
        }


        //215 내려치기 - 스피드 디버프 대상 공증
        if (skill.idx == 215 && selects[0].turnDebuffs.buffs.Any(x => x.objectIdx.Any(y => y == (int)Obj.속도)))
            AddBuff(this, orderIdx, skill, 0, 0);
        //225 생명력 반환
        if (skill.idx == 225)
            buffStat[(int)Obj.currHP] -= (int)skill.effectRate[0];

        //240 활력의 타격 - 생명력 비례 공증
        if (skill.idx == 240)
            skillBuffs.Add(new Buff(BuffType.Stat, BuffOrder.Default, "", skill.effectObject[0], currVitality, skill.effectRate[0], skill.effectCalc[0], skill.effectTurn[0]));
        //247 뿌리내리기
        if (skill.idx == 247)
            rooted = true;
        //248 세계수의 힘
        if (skill.idx == 248)
        {
            skillBuffs.Add(new Buff(BuffType.Stat, BuffOrder.Default, "", skill.effectObject[0], currVitality, skill.effectRate[0], skill.effectCalc[0], skill.effectTurn[0]));
            SpendVitality(currVitality);
        }
        //자연의 응징 5세트 - 세계수의 힘, 붉은 장미 무조건 치명타
        if (set.Value[2] > 0 && (skill.idx == 248 || skill.idx == 250))
            skillBuffs.Add(new Buff(BuffType.Stat, BuffOrder.Default, "", (int)Obj.치명타율, 1, 999, 0, -1));
        //252 자연재해 - 디버프 스킬 사용 시 생명력 회복
        if (HasSkill(252) && skill.effectType.Any(x => x == (int)EffectType.Active_Debuff))
            HealVitality((int)SkillManager.GetSkill(6, 252).effectRate[0]);
        //skill 효과 순차적으로 계산
        Active_Effect(skill, selects);

        orderIdx++;
        buffStat[(int)Obj.currAP] -= GetSkillCost(skill);
        //237 숲의 기운 - 전체 공격 스킬 사용 시 생명력 회복
        if (HasSkill(237) && skill.effectTarget.Any(x => x == 6))
        {
            //자연의 응징 3세트 - 숲의 기운 생명령 회복량 상승, ap도 회복
            float rate = 0;
            if (set.Value[1] > 0)
            {
                rate = 1;
                GetAPHeal(set.Value[1]);
            }
            HealVitality((int)(SkillManager.GetSkill(6, 237).effectRate[0] + rate));
        }
        cooldowns[idx] = skill.cooldown;

        //자연의 선물 5세트 - 뿌리 내리기 쿨타임 1턴으로 감소, AP 소모량 0으로 감소
        if (skill.idx == 247)
        {
            set = ItemManager.GetSetData(17);
            if (set.Value[2] > 0)
            {
                cooldowns[idx] = 1;
                buffStat[(int)Obj.currAP] += GetSkillCost(skill);
            }
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

            switch ((EffectType)skill.effectType[i])
            {
                //데미지 - 스킬 버프 계산 후 
                case EffectType.Damage:
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
                            if (buffStat[(int)Obj.명중률] >= u.buffStat[(int)Obj.회피율])
                                acc = 60 + 6 * (buffStat[(int)Obj.명중률] - u.buffStat[(int)Obj.회피율]) / (u.LVL + 2);
                            else
                                acc = Mathf.Max(acc, 60 + 6 * (buffStat[(int)Obj.명중률] - u.buffStat[(int)Obj.회피율]) / (LVL + 2));

                            //238 거인의 타격 - 적 속도 반비례 명중 상승
                            if (skill.idx == 238)
                                acc += 50 - u.buffStat[(int)Obj.속도];
                            //명중 시
                            if (Random.Range(0, 100) < acc)
                            {
                                isAcc = true;
                                //크리티컬 연산 - dmg * CRB
                                isCrit = Random.Range(0, 100) < buffStat[(int)Obj.치명타율];

                                bool kill = u.GetDamage(this, dmg, buffStat[(int)Obj.방어력무시], isCrit ? buffStat[(int)Obj.치명타피해] : 100).Key;

                                //229 생명력 흡수
                                if (skill.idx == 229)
                                {
                                    int finalDEF = Mathf.Max(0, u.buffStat[(int)Obj.방어력] - buffStat[(int)Obj.방어력무시]);
                                    GetHeal(Mathf.Max(1, dmg - finalDEF) * 0.3f);
                                }
                                //234 자연의 순환
                                if (HasSkill(234) && kill)
                                    HealVitality((int)SkillManager.GetSkill(6, 146).effectRate[0]);
                                //239 소멸
                                if (skill.idx == 239 && kill)
                                    HealVitality(maxVitality);
                                damaged.Add(u);

                                if (kill)
                                    OnKill();
                                if (isCrit)
                                    OnCrit();

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
                                u.AddDebuff(this, orderIdx, skill, i, stat);
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
                case EffectType.CharSpecial1:
                    {
                        //생명력 소모
                        SpendVitality((int)skill.effectRate[i]);
                        break;
                    }
                case EffectType.CharSpecial2:
                    {
                        //생명력 회복
                        HealVitality((int)skill.effectRate[i]);
                        break;
                    }
                case EffectType.CharSpecial3:
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
            heal *= 2f;
        buffStat[(int)Obj.currHP] = Mathf.Min(buffStat[(int)Obj.체력], Mathf.RoundToInt(buffStat[(int)Obj.currHP] + heal));
    }
    public override KeyValuePair<bool, int> GetDamage(Unit caster, float dmg, int pen, int crb)
    {
        KeyValuePair<bool, int> killed = base.GetDamage(caster, dmg, pen, crb);

        if (HasSkill(255) && killed.Key && revive == 0)
        {
            gameObject.SetActive(true);

            buffStat[(int)Obj.currHP] = 0;
            GetHeal(SkillManager.GetSkill(6, 255).effectRate[0] * buffStat[(int)Obj.체력]);
            LogManager.instance.AddLog("세계수의 보호 효과로 치명적인 피해를 막고 회복했습니다.");
            revive = 1;
            killed = new KeyValuePair<bool, int>(false, killed.Value);
        }

        return killed;
    }

    public override void AddDebuff(Unit caster, int order, Skill s, int effectIdx, float rate)
    {
        if (caster == null || caster == this || !cycleSet[2])
            base.AddDebuff(caster, order, s, effectIdx, rate);
    }

    void HealVitality(int rate)
    {
        currVitality = Mathf.Min(maxVitality, currVitality + rate);
        if (HasSkill(253))
        {
            Skill s = SkillManager.GetSkill(6, 253);
            turnBuffs.buffs.RemoveAll(x => x.name == s.name);
            turnBuffs.Add(new Buff(BuffType.Stat, new BuffOrder(this), s.name, s.effectObject[0], currVitality, s.effectRate[0], s.effectCalc[0], s.effectTurn[0], s.effectDispel[0], s.effectVisible[0]));
            turnBuffs.Add(new Buff(BuffType.Stat, new BuffOrder(this), s.name, s.effectObject[1], currVitality, s.effectRate[1], s.effectCalc[1], s.effectTurn[1], s.effectDispel[1], s.effectVisible[1]));
        }

        KeyValuePair<string, float[]> set = ItemManager.GetSetData(18);
        //순환 2세트 - 생명력 획득 시 다음 생명력 변환 ATK 상승
        cycleSet[0] = set.Value[0] > 0;
        //순환 4세트 - 생명력 3일 경우 방어력 상승 및 디버프 면역
        cycleSet[2] = set.Value[2] > 0 && currVitality == 3;
        if (set.Value[2] > 0) turnBuffs.buffs.RemoveAll(x => x.name == set.Key);
        if (cycleSet[2]) turnBuffs.Add(new Buff(BuffType.Stat, new BuffOrder(this), set.Key, (int)Obj.방어력, 1, set.Value[2], 1, 99, 0, 1));
    }
    void SpendVitality(int rate)
    {
        currVitality = Mathf.Max(0, currVitality - rate);
        if (HasSkill(253))
        {
            Skill s = SkillManager.GetSkill(6, 253);
            turnBuffs.buffs.RemoveAll(x => x.name == s.name);

            if (currVitality > 0)
            {
                turnBuffs.Add(new Buff(BuffType.Stat, new BuffOrder(this), s.name, s.effectObject[0], currVitality, s.effectRate[0], s.effectCalc[0], s.effectTurn[0], s.effectDispel[0], s.effectVisible[0]));
                turnBuffs.Add(new Buff(BuffType.Stat, new BuffOrder(this), s.name, s.effectObject[1], currVitality, s.effectRate[1], s.effectCalc[1], s.effectTurn[1], s.effectDispel[1], s.effectVisible[1]));
            }
        }

        //순환 3세트 - 생명력 소모 시, 다음 생명력 흡수 ATK 상승
        KeyValuePair<string, float[]> set = ItemManager.GetSetData(18);
        cycleSet[1] = set.Value[1] > 0;
        //순환 4세트 - 생명력 3일 경우 방어력 상승 및 디버프 면역
        cycleSet[2] = set.Value[2] > 0 && currVitality == 3;
        if (set.Value[2] > 0) turnBuffs.buffs.RemoveAll(x => x.name == set.Key);
        if (cycleSet[2]) turnBuffs.Add(new Buff(BuffType.Stat, new BuffOrder(this), set.Key, (int)Obj.방어력, 1, set.Value[2], 1, 99, 0, 1));
    }
}
