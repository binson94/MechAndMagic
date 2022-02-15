using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class MadScientist : Character
{
    public bool isMagnetic = false;
    public int turnCount = 0;
    
    int bombState = 0;
    int uncontrolCount = 0;

    public override void OnBattleStart(BattleManager BM)
    {
        base.OnBattleStart(BM);
        //162 골렘 주종 역전
        if(HasSkill(162))
            isMagnetic = true;
    }
    public override void OnTurnStart()
    {
        base.OnTurnStart();
        bombState = 0;

        string s1 = SkillManager.GetSkill(classIdx, 144).name;
        string s2 = SkillManager.GetSkill(classIdx, 157).name;
        
        isMagnetic = HasSkill(162) || turnBuffs.buffs.Any(x => x.name == s1 || x.name == s2);
    }
    public override void OnTurnEnd()
    {
        base.OnTurnEnd();
        //171 타임 리프
        if(HasSkill(171))
            turnCount++;
    }

    public override int GetSkillCost(Skill s)
    {
        float mul = 1;
        if(s.idx == 158 && bombState == 2 || s.idx == 159 && bombState == 1)
            mul = 0.5f;

        KeyValuePair<string, float[]> set = ItemManager.GetSetData(11);
        if (set.Value[0] > 0 && (s.idx == 135 || s.idx == 136 || s.idx == 137))
            mul = 2;
        if(set.Value[1] > 0 && (s.idx == 154 || s.idx == 155 || s.idx == 156))
            mul = 2;
        return Mathf.RoundToInt(base.GetSkillCost(s) * mul);
    }

    public override string CanCastSkill(int idx)
    {
        if (SkillManager.GetSkill(classIdx, activeIdxs[idx]).category == 1028 && !BM.HasGolem())
            return "골렘 필요";
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

        //기초 과학자 4세트 - 순수 발명품 스킬 사용 시 버프
        KeyValuePair<string, float[]> set = ItemManager.GetSetData(12);
        if (set.Value[2] > 0 && skill.category == 1026)
        {
            skillBuffs.Add(new Buff(BuffType.Stat, LVL, new BuffOrder(), "", (int)Obj.ATK, 1, set.Value[2], 1, -1));
            skillBuffs.Add(new Buff(BuffType.Stat, LVL, new BuffOrder(), "", (int)Obj.ACC, 1, set.Value[2], 1, -1));
            skillBuffs.Add(new Buff(BuffType.Stat, LVL, new BuffOrder(), "", (int)Obj.CRC, 1, set.Value[2], 1, -1));
            skillBuffs.Add(new Buff(BuffType.Stat, LVL, new BuffOrder(), "", (int)Obj.CRB, 1, set.Value[2], 1, -1));
            skillBuffs.Add(new Buff(BuffType.Stat, LVL, new BuffOrder(), "", (int)Obj.PEN, 1, set.Value[2], 1, -1));
        }


        //skill 효과 순차적으로 계산
        Active_Effect(skill, selects);

        //144 시선 집중 기계, 157 하이라이트 부츠 - 자석탱
        if(skill.idx == 144 || skill.idx == 157)
        {
            turnBuffs.Add(new Buff(BuffType.None, LVL, new BuffOrder(this, -1), skill.name, 0, 0, 0, 0, 3, 1, 1));
            isMagnetic = true;
        }
        //165 업그레이드 세트
        if(skill.idx == 165)
        {
            Skill tmp = SkillManager.GetSkill(classIdx, 165);
            for (int i = 0; i < 3; i++)
            {
                Unit target = BM.GetEffectTarget(2)[0];

                target.AddBuff(this, orderIdx, tmp, 0, 0);
                target.AddBuff(this, orderIdx++, tmp, 1, 0);
            }
        }

        if (skill.idx == 130 || skill.idx == 139 || skill.idx == 149 || skill.idx == 164)
            uncontrolCount++;

        //카오스 패닉 2세트 - 유독류탄, 교란 폭탄 둘 중 하나 사용 시 이번 턴 동안 다른 스킬 AP 소모량 절반
        if (bombState == 0 && ItemManager.GetSetData(10).Value[0] > 0)
        {
            if (skill.idx == 158) bombState = 1;
            else if (skill.idx == 159) bombState = 2;
        }

        orderIdx++;
        buffStat[(int)Obj.currAP] -= GetSkillCost(skill);
        
        cooldowns[idx] = skill.cooldown;
        //궁극의 피조물 2, 4 세트 - 컨트롤 스킬 쿨타임 감소
        set = ItemManager.GetSetData(11);
        if (set.Value[0] > 0 && (skill.idx == 135 || skill.idx == 136 || skill.idx == 137))
            cooldowns[idx] = 0;
        if (set.Value[1] > 0 && (skill.idx == 154 || skill.idx == 155 || skill.idx == 156))
            cooldowns[idx]--;
    }
    protected override void Active_Effect(Skill skill, List<Unit> selects)
    {
        List<Unit> effectTargets;
        List<Unit> damaged = new List<Unit>();
        float stat;
        int count = skill.effectCount;

        //카오스 패닉 4세트 - 통제불능 스킬 히트 수 1 증가
        if(ItemManager.GetSetData(10).Value[1] > 0 && uncontrolCount == 4 && (skill.idx == 130 || skill.idx == 139 || skill.idx == 149 || skill.idx == 164))
        {
            uncontrolCount = 0;
            count++;
        }

        for (int i = 0; i < count; i++)
        {
            //152 피아 인식 장치
            if (skill.effectTarget[i] == 7 && HasSkill(152))
                effectTargets = GetEffectTarget(selects, damaged, 4);
            //160 세기말 방독면
            else if(skill.effectTarget[i] == 11 && HasSkill(160))
                effectTargets = GetEffectTarget(selects, damaged, 6);
            else
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

                            //172 상태 왜곡 장치, 카오스 패닉 5세트(골렘도)
                            if (HasSkill(172) && (u == this || (u.classIdx == 12 && ItemManager.GetSetData(10).Value[2] > 0)))
                                u.GetHeal(stat * skill.effectRate[i] * skill.effectCalc[i] == 1 ? buffStat[(int)Obj.HP] : 1);
                            else
                            {
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
                        }

                        break;
                    }
                case SkillType.Heal:
                    {
                        float heal = stat * skill.effectRate[i];

                        foreach (Unit u in effectTargets)
                        {
                            //172 상태 왜곡 장치
                            if (HasSkill(172) && (u == this || (u.classIdx == 12 && ItemManager.GetSetData(10).Value[2] > 0)))
                                u.GetDamage(this, heal * skill.effectCalc[i] == 1 ? u.buffStat[(int)Obj.HP] : 1, buffStat[(int)Obj.PEN], 100);
                            else
                                u.GetHeal(skill.effectCalc[i] == 1 ? heal * u.buffStat[(int)Obj.HP] : heal);
                        }
                        break;
                    }
                case SkillType.Active_Buff:
                    {
                        if (skill.effectCond[i] == 0 || skill.effectCond[i] == 1 && isAcc || skill.effectCond[i] == 2 && isCrit)
                            foreach (Unit u in effectTargets)
                                //172 상태 왜곡 장치
                                if (HasSkill(172) && (u == this || (u.classIdx == 12 && ItemManager.GetSetData(10).Value[2] > 0)))
                                    u.AddDebuff(this, orderIdx, skill, i, stat);
                                else
                                    u.AddBuff(this, orderIdx, skill, i, stat);
                        break;
                    }
                case SkillType.Active_Debuff:
                    {
                        if (skill.effectCond[i] == 0 || skill.effectCond[i] == 1 && isAcc || skill.effectCond[i] == 2 && isCrit)
                            foreach (Unit u in effectTargets)
                                //172 상태 왜곡 장치
                                if (HasSkill(172) && (u == this || (u.classIdx == 12 && ItemManager.GetSetData(10).Value[2] > 0)))
                                    u.AddBuff(this, orderIdx, skill, i, stat);
                                else
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
                        //골렘 조종 스킬
                        BM.GolemControl(new KeyValuePair<int, List<Unit>>((int)skill.effectRate[i], selects));
                        break;
                    }
                default:
                    break;
            }
        }
    }

    //궁극의 피조물 5세트 - 골렘이 처치 시 9LV 컨트롤 스킬 쿨 회복
    public void GolemKills()
    {
        for(int i = 0;i < activeIdxs.Length;i++)
        {
            Skill s = SkillManager.GetSkill(classIdx, activeIdxs[i]);
            
            if(s.idx == 168 || s.idx == 169)
                cooldowns[i] = 0;
        }
    }

    protected override void Passive_BattleStart()
    {
        List<Unit> effectTargets;
        float rate = ItemManager.GetSetData(12).Value[0];

        //passive
        for (int j = 0; j < passiveIdxs.Length; j++)
        {
            Skill s = SkillManager.GetSkill(classIdx, passiveIdxs[j]);
            if (s == null)
                continue;

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
                
                //기초 과학자 2세트 - 1레벨 패시브 강화
                if((s.idx == 132 || s.idx == 133 || s.idx == 134) && rate > 0)
                {
                    turnBuffs.Add(new Buff(BuffType.Stat, LVL, new BuffOrder(this, -1), s.name, s.effectObject[0], s.effectStat[0], s.effectRate[0] + rate, s.effectCalc[0], s.effectTurn[0], s.effectDispel[0], s.effectVisible[0]));
                    continue;
                }
                switch ((SkillType)s.effectType[i])
                {
                    case SkillType.Passive_HasSkillBuff:
                        {
                            if (HasSkill(s.effectCond[i], true))
                                foreach (Unit u in effectTargets)
                                    u.AddBuff(this, -2, s, i, 0);
                            break;
                        }
                    case SkillType.Passive_HasSkillDebuff:
                        {
                            if (HasSkill(s.effectCond[i], true))
                                foreach (Unit u in effectTargets)
                                    u.AddDebuff(this, -2, s, i, 0);
                            break;
                        }
                    case SkillType.Passive_EternalBuff:
                        {
                                foreach (Unit u in effectTargets)
                                    u.AddBuff(this, -2, s, i, 0);
                            break;
                        }
                    case SkillType.Passive_EternalDebuff:
                        {
                                foreach (Unit u in effectTargets)
                                    u.AddDebuff(this, -2, s, i, 0);
                            break;
                        }
                    default:
                        break;
                }
            }
        }
    }
}