using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArmedFighter : Character
{
    int accCount = 0;

    //32 콤비네이션 히트
    int resentSkillCategory = 0;

    //36 피니셔
    int punchCount = 0;

    //30 적응형 아머, 31 충격 동력화
    bool[] skillOn = new bool[2];

    ChargingPunch chargingPunch = null;
   
    public override void OnTurnStart()
    {
        //26 차징 펀치
        if(chargingPunch != null)
        {
            if (chargingPunch.target.isActiveAndEnabled && !IsStun())
            {
                if (Random.Range(0, 100) < chargingPunch.acc)
                {
                    chargingPunch.target.GetDamage(this, chargingPunch.atk, buffStat[(int)Obj.PEN]);
                }
                else
                    LogManager.instance.AddLog("Dodge");
            }

            chargingPunch = null;
        }

        //30 적응형 아머
        if(skillOn[0])
        {
            Skill s = SkillManager.GetSkill(1, 30);
            inbattleBuffList.Add(new Buff(s.name, s.effectTurn[0], s.effectObject[0], dmgs[3], 0, s.effectRate[0]));
            skillOn[0] = false;
        }
        //31 충격 동력화
        else if (skillOn[1])
        {
            Skill s = SkillManager.GetSkill(1, 31);
            inbattleBuffList.Add(new Buff(s.name, s.effectTurn[0], s.effectObject[0], dmgs[1], 0, s.effectRate[0]));
            skillOn[0] = false;
        }

        base.OnTurnStart();
        resentSkillCategory = 0;
        punchCount = 0;
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

        //36 피니셔 - punchCount 비례 공증
        if (skill.idx == 36)
        {
            Skill tmp = SkillManager.GetSkill(classIdx, 36);
            inskillBuffList.Add(new Buff(tmp.name, tmp.effectTurn[0], tmp.effectObject[0], punchCount, tmp.effectCalc[0], tmp.effectRate[0]));
        }
        //37 리퍼스 킥 - 타겟 체력 40% 이하일 시 100% 크리티컬
        else if (skill.idx == 37 && (selects[0].buffStat[(int)Obj.currHP] / (float)selects[0].buffStat[(int)Obj.HP]) <= 0.4f)
            inskillBuffList.Add(new Buff("", -1, (int)Obj.CRC, 100, 0, 1));

        //skill 효과 순차적으로 계산
        Active_Effect(skill, selects);

        //36 피니셔
        if (skill.category == 1000)
            punchCount++;
        //32 콤비네이션 히트
        resentSkillCategory = skill.category;

        buffStat[(int)Obj.currAP]-= GetSkillCost(skill);
        cooldowns[idx] = skill.cooldown;
    }
    protected override void Active_Effect(Skill skill, List<Unit> selects)
    {
        List<Unit> effectTargets;
        List<Unit> damaged = new List<Unit>();
        float rate = 0;

        int count = skill.effectCount;
        for (int i = 0; i < count; i++)
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

            switch((SkillType)skill.effectType[i])
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
                                accCount++;

                                //23 싸움의 리듬 - 3회 적중 성공 시 이번 전투 동안 spd 버프
                                if (HasSkill(23) && accCount >= 3)
                                {
                                    accCount = 0;
                                    AddBuff(SkillManager.GetSkill(1, 23), 0, 0);
                                }

                                //크리티컬 연산 - dmg * CRB
                                if (Random.Range(0, 100) < buffStat[(int)Obj.CRC])
                                {
                                    isCrit = true;
                                    dmg = Mathf.CeilToInt(dmg * (buffStat[(int)Obj.CRB] / 100f));
                                }
                                else
                                    isCrit = false;

                                u.GetDamage(this, dmg, buffStat[(int)Obj.PEN]);
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
                        //머신건 히트 히트수 결정
                        if (buffStat[(int)Obj.SPD] <= 13)
                            count = 4;
                        else if (buffStat[(int)Obj.SPD] <= 25)
                            count = 5;
                        else if (buffStat[(int)Obj.SPD] <= 38)
                            count = 6;
                        else
                            count = 7;
                        break;
                    }
                case SkillType.CharSpecial2:
                    {
                        AddBuff(skill, i, 0);
                        StatUpdate_Skill(skill.category);

                        chargingPunch = new ChargingPunch(selects[0], buffStat[(int)Obj.ATK], buffStat[(int)Obj.ACC]);
                        //AP 값 0으로
                        buffStat[(int)Obj.AP] = buffStat[(int)Obj.currAP] = 0;
                        break;
                    }
                default:
                    break;
            }
        }
    }

    protected override void Passive_SkillCast(int skillCategory)
    {
        for (int j = 0; j < passiveIdxs.Length; j++)
        {
            Skill skill = SkillManager.GetSkill(classIdx, passiveIdxs[j]);

            //32 콤비네이션 히트
            if (skill.idx == 32)
            {
                if (skillCategory == 1000 && resentSkillCategory == 1001 || skillCategory == 1001 && resentSkillCategory == 1000)
                {
                    AddBuff(SkillManager.GetSkill(1, 32), 0, 0);
                }
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

    public class ChargingPunch
    {
        public Unit target;
        public int atk;
        public int acc;

        public ChargingPunch()
        {
            target = null;
            atk = 0; acc = 0;
        }
        public ChargingPunch(Unit u, int a, int ac)
        {
            target = u;
            atk = a;
            acc = ac;
        }
    }
}
