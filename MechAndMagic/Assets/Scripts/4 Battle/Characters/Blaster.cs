using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class ImplantBomb
{
    public Unit caster;
    public float dmg;
    public int pen;

    public ImplantBomb(Unit u = null, float d = 0, int p = 0)
    {
        caster = u;
        dmg = d;
        pen = p;
    }

    public void Bomb(BattleManager BM, Unit died)
    {
        List<Unit> targets = BM.GetEffectTarget(6);
        targets.Remove(died);

        foreach (Unit u in targets)
            u.GetDamage(caster, dmg, pen, 100);
    }
}

public class Blaster : Character
{
    public int currHeat;
    bool beforeCool = false;

    public override void OnTurnStart()
    {
        base.OnTurnStart();
        
        KeyValuePair<string, float[]> set = ItemManager.GetSetData(9);
        //혹한기 작전 4세트 - 이전 턴에 쿨링 히트 시 ATK, ACC, CRC 상승
        if(set.Value[2] > 0 && beforeCool)
        {
            turnBuffs.Add(new Buff(BuffType.Stat, new BuffOrder(this, orderIdx), set.Key, (int)Obj.공격력, 1, set.Value[2], 1, 1, 1, 1));
            turnBuffs.Add(new Buff(BuffType.Stat, new BuffOrder(this, orderIdx), set.Key, (int)Obj.명중, 1, set.Value[2], 1, 1, 1, 1));
            turnBuffs.Add(new Buff(BuffType.Stat, new BuffOrder(this, orderIdx), set.Key, (int)Obj.치명타율, 1, set.Value[2], 1, 1, 1, 1));
        }

        if (currHeat >= 5)
        {
            //무한동력 4세트 - 쿨링히트 되지 않음
            if (ItemManager.GetSetData(7).Value[1] == 0)
            {
                turnDebuffs.Add(new Buff(BuffType.None, new BuffOrder(this, orderIdx), "쿨링히트", (int)Obj.쿨링히트, 0, 0, 0, 1, 0, 1));
                LoseHeat(5);

                //혹한기 작전 2세트 - 쿨링 히트시 30% 회복
                if(ItemManager.GetSetData(9).Value[0] > 0)
                    GetHeal(0.3f * buffStat[(int)Obj.체력]);

                //106 악천후 회피 - 회피 상승
                if (HasSkill(106))
                {
                    Skill s = SkillManager.GetSkill(classIdx, 106);
                    AddBuff(this, orderIdx, s, 0, 0);
                }
                //115 바이오 쿨링 - 디버프 해제
                if (HasSkill(115))
                    RemoveDebuff(turnDebuffs.Count);

                beforeCool = true;
            }
            else
                beforeCool = false;
        }
        else
            beforeCool = false;
        //126 용광로 코어
        if (HasSkill(126))
            GetHeat(1);
        //109 캐논 예열
        if (HasSkill(109) && currHeat > 0)
        {
            Skill tmp = SkillManager.GetSkill(3, 109);
            LoseHeat(1);
            turnBuffs.Add(new Buff(BuffType.Stat, new BuffOrder(this, orderIdx), tmp.name, tmp.effectObject[0], 1, tmp.effectRate[0], tmp.effectCalc[0], tmp.effectTurn[0], tmp.effectDispel[0], tmp.effectVisible[0]));
            
            //무한동력 2세트 - 캐논 예열이 모든 열기 소모
            set = ItemManager.GetSetData(7);
            if(currHeat > 0 && set.Value[0] > 0)
            {
                turnBuffs.Add(new Buff(BuffType.Stat, new BuffOrder(this, orderIdx), set.Key, (int)Obj.공격력, currHeat, set.Value[0], 1, 1, 1, 1));
                LoseHeat(currHeat);
            }
        }
        //125 극도로 정밀한 계산
        if (HasSkill(125))
        {
            AddBuff(this, orderIdx, SkillManager.GetSkill(classIdx, 125), 0, 0);
        }
    }

    public override string CanCastSkill(int idx)
    {
        if (SkillManager.GetSkill(classIdx, activeIdxs[idx]).category == 1016 && turnDebuffs.buffs.Any(x => x.name == "쿨링히트"))
            return "열기로 인해 이번 턴에 공격할 수 없습니다.";
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

        skillBuffs.Clear();
        skillDebuffs.Clear();

        if (skill == null) return;
        

        Passive_SkillCast(skill);

        LogManager.instance.AddLog($"{name}(이)가 {skill.name}(을)를 시전했습니다.");
        //skill 효과 순차적으로 계산
        Active_Effect(skill, selects);
        SoundManager.Instance.PlaySFX(skill.sfx);

        //109 캐논 예열, 125 극도로 정밀한 계산 - 매 턴 처음 스킬에만 적용
        if (HasSkill(109))
            turnBuffs.buffs.RemoveAll(x => x.name == SkillManager.GetSkill(classIdx, 109).name);
        if (HasSkill(125))
            turnBuffs.buffs.RemoveAll(x => x.name == SkillManager.GetSkill(classIdx, 125).name);

        orderIdx++;
        buffStat[(int)Obj.currAP] -= GetSkillCost(skill);
        cooldowns[slotIdx] = skill.cooldown;

        //중화기 전문가 5세트 - 쿨타임 3턴 스킬 1턴 감소
        if(skill.cooldown == 3 && ItemManager.GetSetData(8).Value[1] > 0)
            cooldowns[slotIdx]--;

        StatUpdate_Turn();
    }
    protected override void Active_Effect(Skill skill, List<Unit> selects)
    {
        List<Unit> effectTargets;
        List<Unit> damaged = new List<Unit>();

        for (int i = 0; i < skill.effectCount; i++)
        {
            effectTargets = GetEffectTarget(selects, damaged, skill.effectTarget[i]);

            switch ((EffectType)skill.effectType[i])
            {
                //데미지 - 스킬 버프 계산 후 
                case EffectType.Damage:
                    {
                        StatUpdate_Skill(skill);

                        float dmg = GetEffectStat(selects, skill.effectStat[i]) * skill.effectRate[i];

                        damaged.Clear();
                        foreach (Unit u in effectTargets)
                        {
                            if (!u.isActiveAndEnabled)
                                continue;

                            int acc = 20;
                            if (buffStat[(int)Obj.명중] >= u.buffStat[(int)Obj.회피])
                                acc = 6 * (buffStat[(int)Obj.명중] - u.buffStat[(int)Obj.회피]) / (u.LVL + 2);
                            else
                                acc = 6 * (buffStat[(int)Obj.명중] - u.buffStat[(int)Obj.회피]) / (LVL + 2);
                            
                            acc = Mathf.Max(20, acc);

                            if (Random.Range(0, 100) < acc)
                            {
                                isAcc = true;
                                //크리티컬 연산 - dmg * CRB
                                //119 네이팜 탄 - 화상 상대 100% 크리
                                int crt = (skill.idx == 119 && u.turnDebuffs.buffs.Any(x => x.objectIdx.Any(y => y == (int)Obj.화상)) ? 0 : Random.Range(0, 100));
                                isCrit = crt <= buffStat[(int)Obj.치명타율];

                                //123 타이탄 킬러 - 적 체력 비례 추뎀
                                if (HasSkill(123))
                                {
                                    Skill tmp = SkillManager.GetSkill(classIdx, 123);
                                    u.GetDamage(this, dmg + u.buffStat[(int)Obj.currHP] * tmp.effectRate[0], buffStat[(int)Obj.방어력무시], isCrit ? buffStat[(int)Obj.치명타피해] : 100);
                                }
                                else
                                    u.GetDamage(this, dmg, buffStat[(int)Obj.방어력무시], isCrit ? buffStat[(int)Obj.치명타피해] : 100);

                                //112 임플란트 봄
                                if (skill.idx == 112)
                                {
                                    u.implantBomb = new ImplantBomb(this, buffStat[(int)Obj.공격력], buffStat[(int)Obj.방어력무시]);
                                    if(u.buffStat[(int)Obj.currHP] <= 0)
                                        u.implantBomb.Bomb(BM, u);
                                }
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
                case EffectType.Active_RemoveBuff:
                    {
                        int count = 0;
                        foreach (Unit u in effectTargets)
                            count += u.RemoveBuff(Mathf.RoundToInt(skill.effectRate[i]));

                        //108 취약점 발견
                        if (HasSkill(108) && count > 0)
                        {
                            Skill tmp = SkillManager.GetSkill(classIdx, 108);
                            turnBuffs.Add(new Buff(BuffType.Stat, new BuffOrder(this, orderIdx), tmp.name, tmp.effectObject[0], count, tmp.effectRate[0], tmp.effectCalc[0], tmp.effectTurn[0], tmp.effectDispel[0], tmp.effectVisible[0]));
                        }
                        break;
                    }
                case EffectType.DoNothing:
                    break;
                case EffectType.CharSpecial1:
                    {
                        //TP 감소
                        BM.ReduceTP(effectTargets, Mathf.RoundToInt(skill.effectRate[i]));
                        break;
                    }
                case EffectType.CharSpecial2:
                    {
                        //열기 상승
                        GetHeat(skill.effectRate[i]);
                        break;
                    }
                case EffectType.CharSpecial3:
                    {
                        //열기 감소
                        LoseHeat(skill.effectRate[i]);
                        break;
                    }
                default:
                    ActiveDefaultCase(skill, i, effectTargets, GetEffectStat(selects, skill.effectStat[i]));
                    break;
            }
        }
    }

    protected override void Passive_SkillCast(Skill active)
    {
        for (int j = 0; j < passiveIdxs.Length; j++)
        {
            Skill skill = SkillManager.GetSkill(classIdx, passiveIdxs[j]);

            //124 숙련된 포격술 - 쿨타임 3 스킬 강화
            if (skill.idx == 124 && active.cooldown == 3)
            {
                Skill tmp = SkillManager.GetSkill(classIdx, 124);
                //중화기 전문가 3세트 - 숙련된 포격술 강화
                float rate = 1 + ItemManager.GetSetData(8).Value[0];
                for (int i = 0; i < 3; i++)
                    skillBuffs.Add(new Buff(BuffType.Stat, BuffOrder.Default, "", tmp.effectObject[i], buffStat[tmp.effectStat[i]], tmp.effectRate[i] * rate, tmp.effectCalc[0], tmp.effectTurn[0]));
                continue;
            }

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

   public override int RemoveDebuff(int count)
    {
        int cnt = base.RemoveDebuff(count);

        //107 맑아진 정신 - 디버프 해제 시 명중 버프
        if (HasSkill(107))
        {
            Skill s = SkillManager.GetSkill(classIdx, 107);
            turnBuffs.Add(new Buff(BuffType.Stat, new BuffOrder(this, orderIdx), s.name, s.effectObject[0], cnt, s.effectRate[0], s.effectCalc[0], s.effectTurn[0], s.effectDispel[0], s.effectVisible[0]));
            
            KeyValuePair<string, float[]> set = ItemManager.GetSetData(9);
            //혹한기 작전 3세트 - 맑아진 정신이 최대 AP 버프
            if(set.Value[1] > 0)
                turnBuffs.Add(new Buff(BuffType.Stat, new BuffOrder(this, orderIdx), set.Key, (int)Obj.행동력, cnt, set.Value[1], 0, 2, s.effectDispel[0], s.effectVisible[0]));
        }

        return cnt;
    }

    void GetHeat(float amt)
    {
        //89 - 열기 : 강화, 98 - 열기 : 가속
        bool heatPower = HasSkill(89);
        bool heatSpeed = HasSkill(98);

        if (heatPower || heatSpeed)
        {
            currHeat = Mathf.RoundToInt(Mathf.Min(5, currHeat + amt));
            if (heatPower)
            {
                Skill s = SkillManager.GetSkill(classIdx, 89);
                turnBuffs.buffs.RemoveAll(x=>x.name == s.name);
                float rate = s.effectRate[0] * (1 + ItemManager.GetSetData(7).Value[1]);
                turnBuffs.Add(new Buff(BuffType.Stat, new BuffOrder(this), s.name, s.effectObject[0], currHeat, rate, s.effectCalc[0], s.effectTurn[0], s.effectDispel[0], s.effectVisible[0]));
            }
            if (heatSpeed)
            {
                Skill s = SkillManager.GetSkill(classIdx, 98);
                turnBuffs.buffs.RemoveAll(x=>x.name == s.name);
                float rate = s.effectRate[0] * (1 + ItemManager.GetSetData(7).Value[1]);
                turnBuffs.Add(new Buff(BuffType.Stat, new BuffOrder(this), s.name, s.effectObject[0], currHeat, rate, s.effectCalc[0], s.effectTurn[0], s.effectDispel[0], s.effectVisible[0]));
            }

            //126 용광로 코어
            if (HasSkill(126) && currHeat == 4)
            {
                Skill s = SkillManager.GetSkill(classIdx, 126);
                AddBuff(this, -1, s, 0, 0);
                }
            else
                turnBuffs.buffs.RemoveAll(x => x.name == SkillManager.GetSkill(classIdx, 126).name);

        }

    }
    void LoseHeat(float amt)
    {
        bool heatPower = HasSkill(89);
        bool heatSpeed = HasSkill(98);

        //117 캐논 과부화
        if(HasSkill(117))
            amt++;
        //118 캐논 안정화
        if(HasSkill(118))
            amt--;

        if (heatPower || heatSpeed)
        {
            currHeat = Mathf.RoundToInt(Mathf.Max(0, currHeat - amt));
            if (heatPower)
            {
                Skill s = SkillManager.GetSkill(classIdx, 89);
                turnBuffs.buffs.RemoveAll(x=>x.name == s.name);
                float rate = s.effectRate[0] * (1 + ItemManager.GetSetData(7).Value[1]);
                if(currHeat > 0)
                    turnBuffs.Add(new Buff(BuffType.Stat, new BuffOrder(this), s.name, s.effectObject[0], currHeat, rate, s.effectCalc[0], s.effectTurn[0], s.effectDispel[0], s.effectVisible[0]));
            }
            if (heatSpeed)
            {
                Skill s = SkillManager.GetSkill(classIdx, 98);
                turnBuffs.buffs.RemoveAll(x=>x.name == s.name);
                float rate = 1 + ItemManager.GetSetData(7).Value[1];
                if (currHeat > 0)
                {
                    turnBuffs.Add(new Buff(BuffType.Stat, new BuffOrder(this), s.name, s.effectObject[0], currHeat, s.effectRate[0] * rate, s.effectCalc[0], s.effectTurn[0], s.effectDispel[0], s.effectVisible[0]));
                    turnBuffs.Add(new Buff(BuffType.Stat, new BuffOrder(this), s.name, s.effectObject[1], currHeat, s.effectRate[1] * rate, s.effectCalc[1], s.effectTurn[1], s.effectDispel[1], s.effectVisible[1]));
                }
            }

            //126 용광로 코어
            if (HasSkill(126) && currHeat != 4)
                turnBuffs.buffs.RemoveAll(x => x.name == SkillManager.GetSkill(classIdx, 126).name);
        }
    }
}
