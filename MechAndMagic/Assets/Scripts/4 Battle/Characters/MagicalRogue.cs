using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class MagicalRogue : Character
{
    int resentCategory = 0;

    bool cast340 = false;

    //341, 342 1, 2형 교활함 스킬 사용 수 저장
    int[] guileCount = new int[2];

    public override void OnTurnStart()
    {
        base.OnTurnStart();
        resentCategory = 0;

        if (cast340)
            AddBuff(this, orderIdx, SkillManager.GetSkill(classIdx, 340), 2, 0);
        cast340 = false;
    }

    public override string CanCastSkill(int idx)
    {
        Skill s = SkillManager.GetSkill(classIdx, activeIdxs[idx]);
        if (s.category == 1020 && resentCategory != 1019)
            return "1형 스킬 다음에 시전해야 합니다.";
        else if (s.category == 1021 && resentCategory != 1020)
            return "2형 스킬 다음에 시전해야 합니다.";
        else
            return base.CanCastSkill(idx);
    }
    public override void ActiveSkill(int slotIdx, List<Unit> selects)
    {
        //적중 성공 여부
        isAcc = true;
        //크리티컬 성공 여부
        isCrit = false;

        //skillDB에서 스킬 불러오기
        Skill skill = SkillManager.GetSkill(classIdx, activeIdxs[slotIdx]);
        KeyValuePair<string, float[]> set;
        skillBuffs.Clear();
        skillDebuffs.Clear();

        if (skill == null)
        {
            Debug.LogError("skill is null");
            return;
        }

        //콤비네이션 3세트 - 3형 무술 CRC, CRB 상승
        set = ItemManager.GetSetData(22);
        if (skill.category == 1021 && set.Value[1] > 0)
        {
            skillBuffs.Add(new Buff(BuffType.Stat, BuffOrder.Default, "", (int)Obj.치명타율, 1, set.Value[1], 1, -1));
            skillBuffs.Add(new Buff(BuffType.Stat, BuffOrder.Default, "", (int)Obj.치명타피해, 1, set.Value[1], 1, -1));
        }
        //콤비네이션 5세트 - 잔인한 난도질, 공허의 타격 강화
        if (set.Value[2] > 0 && (skill.idx == 347 || skill.idx == 349))
        {
            skillBuffs.Add(new Buff(BuffType.Stat, BuffOrder.Default, "", (int)Obj.공격력, 1, set.Value[2], 1, -1));
            skillBuffs.Add(new Buff(BuffType.Stat, BuffOrder.Default, "", (int)Obj.명중률, 1, set.Value[2], 1, -1));
            skillBuffs.Add(new Buff(BuffType.Stat, BuffOrder.Default, "", (int)Obj.치명타피해, 1, set.Value[2], 1, -1));
            skillBuffs.Add(new Buff(BuffType.Stat, BuffOrder.Default, "", (int)Obj.속도, 1, set.Value[2], 1, -1));
        }

        set = ItemManager.GetSetData(23);
        //기민한 맹공 2세트 - 1형 무술 ACC, PEN 상승
        if (set.Value[0] > 0 && skill.category == 1019)
        {
            skillBuffs.Add(new Buff(BuffType.Stat, BuffOrder.Default, "", (int)Obj.명중률, 1, set.Value[0], 1, -1));
            skillBuffs.Add(new Buff(BuffType.Stat, BuffOrder.Default, "", (int)Obj.방어력무시, 1, set.Value[0], 1, -1));
        }
        //기민한 맹공 4세트 - 가로 베기 공격력 상승, 적 잃은 체력 비례 추가 피해
        if (set.Value[2] > 0 && skill.idx == 311)
            skillBuffs.Add(new Buff(BuffType.Stat, BuffOrder.Default, "", (int)Obj.공격력, GetEffectStat(selects, (int)Obj.LossPer), set.Value[2], 1, -1));

        Passive_SkillCast(skill);

        LogManager.instance.AddLog($"{name}(이)가 {skill.name}(을)를 시전했습니다.");
        //skill 효과 순차적으로 계산
        Active_Effect(skill, selects);
        SoundManager.instance.PlaySFX(skill.sfx);

        //321 맹독 부여
        if (HasSkill(321))
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
        if (skill.effectType.Any(x => x == (int)EffectType.CharSpecial2))
            buffStat[(int)Obj.currAP] += 2;

        cooldowns[slotIdx] = skill.cooldown;
        resentCategory = skill.category;

        //콤비네이션 3세트 - 3형 무술 쿨타임 1 감소
        if (skill.category == 1021 && ItemManager.GetSetData(22).Value[1] > 0)
            cooldowns[slotIdx]--;

        //1형 무술
        if (skill.category == 1019)
            guileCount[0]++;
        //2형 무술
        else if (skill.category == 1020)
        {
            guileCount[1]++;

            //콤비네이션 2세트 - 2형 무술 사용 시 무작위 적 버프 해제 및 내 디버프 1개 해제
            set = ItemManager.GetSetData(22);
            if (set.Value[0] > 0)
            {
                GetEffectTarget(selects, selects, 4)[0].RemoveBuff(1);
                RemoveDebuff(1);
            }
        }

        set = ItemManager.GetSetData(23);
        //두려운 악마 4세트 - 환골탈태 사용 시 모든 디버프 해제
        if (skill.idx == 348 && set.Value[2] > 0)
            RemoveDebuff(turnDebuffs.Count);
        //1형 교활함 - 1형 3번 사용 시 행동력 상승
        if (HasSkill(341) && guileCount[0] >= 3)
        {
            Skill tmp = SkillManager.GetSkill(classIdx, 341);

            //두려운 악마 4세트 - 1형 교활함 강화, 기민한 맹공 3세트 - 1형 강화 스킬 강화
            float rate = 1 + set.Value[2] + ItemManager.GetSetData(24).Value[1];
            turnBuffs.Add(new Buff(BuffType.Stat, new BuffOrder(this, orderIdx), tmp.name, tmp.effectObject[0], tmp.effectStat[0], tmp.effectRate[0] * rate, tmp.effectCalc[0], tmp.effectTurn[0], tmp.effectDispel[0], tmp.effectVisible[0]));
            guileCount[0] = 0;
        }
        //2형 교활함 - 2형 2번 사용 시 행동력 상승
        if (HasSkill(342) && guileCount[1] >= 2)
        {
            Skill tmp = SkillManager.GetSkill(classIdx, 342);

            //두려운 악마 4세트 - 2형 교활함 강화
            float rate = 1 + set.Value[2];
            turnBuffs.Add(new Buff(BuffType.Stat, new BuffOrder(this, orderIdx), tmp.name, tmp.effectObject[0], tmp.effectStat[0], tmp.effectRate[0] * rate, tmp.effectCalc[0], tmp.effectTurn[0], tmp.effectDispel[0], tmp.effectVisible[0]));
            guileCount[1] = 0;
        }

        StatUpdate_Turn();
    }
    protected override void Active_Effect(Skill skill, List<Unit> selects)
    {
        List<Unit> effectTargets;
        List<Unit> damaged = new List<Unit>();
        float stat;
        KeyValuePair<string, float[]> set = ItemManager.GetSetData(23);

        for (int i = 0; i < skill.effectCount; i++)
        {
            //두려운 악마 3세트 - 사악한 악령 소환이 2개체에 적중
            if (skill.idx == 346 && set.Value[1] > 0 && skill.effectTarget[i] == 4)
                effectTargets = GetEffectTarget(selects, damaged, 5);
            else
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
                            //명중 시
                            if (Random.Range(0, 100) < acc)
                            {
                                isAcc = true;
                                //329 깊이 찌르기
                                int crcAdd = u.turnDebuffs.buffs.Any(x => x.name == SkillManager.GetSkill(classIdx, 329).name) ? 20 : 0;
                                //크리티컬 연산 - dmg * CRB
                                isCrit = Random.Range(0, 100) < buffStat[(int)Obj.치명타율] + crcAdd;

                                KeyValuePair<bool, int> kill = u.GetDamage(this, dmg, buffStat[(int)Obj.방어력무시], isCrit ? buffStat[(int)Obj.치명타피해] : 100);
                                //348 환골탈태 - 생명력 흡수
                                if (turnBuffs.buffs.Any(x => x.name == SkillManager.GetSkill(classIdx, 348).name))
                                    GetHeal(kill.Value * 0.1f);
                                //321 맹독 부여
                                if (turnBuffs.buffs.Any(x => x.name == SkillManager.GetSkill(classIdx, 321).name))
                                    u.turnDebuffs.Add(new Buff(BuffType.Stat, new BuffOrder(this, orderIdx), "맹독", (int)Obj.맹독, buffStat[(int)Obj.공격력], 0.6f, 0, 2, 1, 1));

                                damaged.Add(u);
                                
                                if (kill.Key)
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
                        //TP 감소
                        BM.ReduceTP(effectTargets, Mathf.RoundToInt(skill.effectRate[i]));
                        return;
                    }
                case EffectType.CharSpecial3:
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
                            if (buffStat[(int)Obj.명중률] >= u.buffStat[(int)Obj.회피율])
                                acc = 60 + 6 * (buffStat[(int)Obj.명중률] - u.buffStat[(int)Obj.회피율]) / (u.LVL + 2);
                            else
                                acc = Mathf.Max(acc, 60 + 6 * (buffStat[(int)Obj.명중률] - u.buffStat[(int)Obj.회피율]) / (LVL + 2));
                            //명중 시
                            if (Random.Range(0, 100) < acc)
                            {
                                isAcc = true;
                                int crcAdd = u.turnDebuffs.buffs.Any(x => x.name == SkillManager.GetSkill(classIdx, 329).name) ? 20 : 0;
                                //크리티컬 연산 - dmg * CRB

                                isCrit = Random.Range(0, 100) < buffStat[(int)Obj.치명타율] + crcAdd;

                                int heal = u.GetDamage(this, dmg, buffStat[(int)Obj.방어력무시], isCrit ? buffStat[(int)Obj.치명타피해] : 100).Value;
                                //348 환골탈태 - 생명력 흡수
                                if (turnBuffs.buffs.Any(x => x.name == SkillManager.GetSkill(classIdx, 348).name))
                                    GetHeal(heal * 0.1f);
                                //321 맹독 부여
                                if (turnBuffs.buffs.Any(x => x.name == SkillManager.GetSkill(classIdx, 321).name))
                                    u.turnDebuffs.Add(new Buff(BuffType.Stat, new BuffOrder(this, orderIdx), "맹독", (int)Obj.맹독, buffStat[(int)Obj.공격력], 0.6f, 0, 2, 1, 1));

                                damaged.Add(u);

                                Passive_SkillHit(skill);
                            }
                            else
                            {
                                isAcc = false;
                                LogManager.instance.AddLog($"{u.name}(이)가 스킬을 회피하였습니다.");
                                break;
                            }
                        }

                        break;
                    }
                default:
                    break;
            }
        }

        //두려운 악마 2세트 - 저주 걸기가 DEF와 DOG도 감소
        if (skill.idx == 322 && set.Value[0] > 0)
        {
            foreach (Unit u in damaged)
            {
                u.turnDebuffs.Add(new Buff(BuffType.Stat, new BuffOrder(this, orderIdx), skill.name, (int)Obj.방어력, 1, set.Value[0], 1, skill.effectTurn[2], skill.effectDispel[2], skill.effectVisible[2]));
                u.turnDebuffs.Add(new Buff(BuffType.Stat, new BuffOrder(this, orderIdx), skill.name, (int)Obj.회피율, 1, set.Value[0], 1, skill.effectTurn[2], skill.effectDispel[2], skill.effectVisible[2]));
            }
        }
    }

    protected override void Passive_SkillCast(Skill active)
    {
        KeyValuePair<string, float[]> set = ItemManager.GetSetData(24);

        for (int j = 0; j < passiveIdxs.Length; j++)
        {
            Skill skill = SkillManager.GetSkill(classIdx, passiveIdxs[j]);

            //콤비네이션 5세트 - 강력함 패시브 강화
            if (skill.idx == 350 || skill.idx == 351 || skill.idx == 352)
            {
                set = ItemManager.GetSetData(22);
                float rate = 1 + set.Value[2];
                turnBuffs.Add(new Buff(BuffType.Stat, new BuffOrder(this, orderIdx), skill.name, skill.effectObject[0], skill.effectStat[0], skill.effectRate[0] * rate, skill.effectCalc[0], skill.effectTurn[0], skill.effectDispel[0], skill.effectVisible[0]));
                continue;
            }
            //두려운 악마 4세트 - 3형 교활함 강화 (1, 2형은 ActiveSkill 함수에서)
            if (skill.idx == 343)
            {
                set = ItemManager.GetSetData(23);
                float rate = 1 + set.Value[2];
                turnBuffs.Add(new Buff(BuffType.Stat, new BuffOrder(this, orderIdx), skill.name, skill.effectObject[0], skill.effectStat[0], skill.effectRate[0] * rate, skill.effectCalc[0], skill.effectTurn[0], skill.effectDispel[0], skill.effectVisible[0]));
                continue;
            }

            if(skill.idx == 317 || skill.idx == 325 || skill.idx == 333 || skill.idx == 349)
                set = ItemManager.GetSetData(24);

            for (int i = 0; i < skill.effectCount; i++)
            {
                if (active.category != 0 && active.category != skill.effectCond[i])
                    continue;

                switch ((EffectType)skill.effectType[i])
                {
                    case EffectType.Passive_CastBuff:
                        {
                            if (skill.idx == 317 || skill.idx == 325 || skill.idx == 333 || skill.idx == 349)
                                turnBuffs.Add(new Buff(BuffType.Stat, new BuffOrder(this, orderIdx), skill.name, skill.effectObject[i], skill.effectStat[i], skill.effectRate[i] * (1 + set.Value[1]), skill.effectCalc[i], skill.effectTurn[i], skill.effectDispel[i], skill.effectVisible[i]));
                            else
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
}
