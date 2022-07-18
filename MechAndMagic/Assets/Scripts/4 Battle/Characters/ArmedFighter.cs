using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArmedFighter : Character
{
    //23 싸움의 리듬
    int accCount = 0;

    //32 콤비네이션 히트
    int resentCategory = 0;
    int selfImproveCount = 0;

    //36 피니셔
    int punchCount = 0;

    //30 적응형 아머, 31 충격 동력화
    bool[] skillOn = new bool[2];

    ChargingPunch chargingPunch = null;

    public override void OnBattleStart(BattleManager BM)
    {
        base.OnBattleStart(BM);


        KeyValuePair<string, float[]> set = ItemManager.GetSetData(1);
        //종합 타격 2세트 - 손, 발 기술 AP 감소
        if (set.Value[0] > 0)
        {
            turnBuffs.Add(new Buff(BuffType.AP, BuffOrder.Default, set.Key, 31, 1, set.Value[0], 1, 99, 0, 1));
        }

        set = ItemManager.GetSetData(2);
        //소닉붐 2세트 - SPD 비례 DOG 향상
        if (set.Value[0] > 0)
            turnBuffs.Add(new Buff(BuffType.Stat, BuffOrder.Default, set.Key, (int)Obj.회피율, dungeonStat[(int)Obj.속도], set.Value[0], 0, 99, 0, 1));
    }
    public override void OnTurnStart()
    {
        //26 차징 펀치
        if (chargingPunch != null)
        {
            if (chargingPunch.target.isActiveAndEnabled && !IsStun())
            {
                if (Random.Range(0, 100) < chargingPunch.acc)
                {
                    chargingPunch.target.GetDamage(this, chargingPunch.atk, buffStat[(int)Obj.방어력무시], 100);
                }
                else
                    LogManager.instance.AddLog($"{chargingPunch.target.name}(이)가 스킬을 회피하였습니다.");
            }

            chargingPunch = null;
        }

        //30 적응형 아머
        if (skillOn[0])
        {
            Skill s = SkillManager.GetSkill(1, 30);
            turnBuffs.Add(new Buff(BuffType.Stat, BuffOrder.Default, s.name, s.effectObject[0], dmgs[3], s.effectRate[0], s.effectCalc[0], s.effectTurn[0], s.effectDispel[0], s.effectVisible[0]));
            skillOn[0] = false;
        }
        //31 충격 동력화
        if (skillOn[1])
        {
            Skill s = SkillManager.GetSkill(1, 31);
            turnBuffs.Add(new Buff(BuffType.Stat, BuffOrder.Default, s.name, s.effectObject[0], dmgs[1], s.effectRate[0], s.effectCalc[0], s.effectTurn[0], s.effectDispel[0], s.effectVisible[0]));
            skillOn[0] = false;
        }


        base.OnTurnStart();
        resentCategory = 0;
        punchCount = 0;
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

        if (skill == null)
        {
            Debug.LogError("skill is null");
            return;
        }

        Passive_SkillCast(skill);

        //36 피니셔 - punchCount 비례 공증
        if (skill.idx == 36)
        {
            Skill tmp = SkillManager.GetSkill(classIdx, 36);
            //종합 타격 5세트 - 피니셔 공증률 증가
            float rate = tmp.effectRate[0] * (1 + ItemManager.GetSetData(1).Value[2]);
            skillBuffs.Add(new Buff(BuffType.Stat, BuffOrder.Default, tmp.name, tmp.effectObject[0], punchCount, rate, tmp.effectCalc[0], tmp.effectTurn[0], tmp.effectDispel[0], tmp.effectVisible[0]));
        }
        //37 리퍼스 킥 - 타겟 체력 40% 이하일 시 100% 크리티컬
        else if (skill.idx == 37 && (selects[0].buffStat[(int)Obj.currHP] / (float)selects[0].buffStat[(int)Obj.체력]) <= 0.4f)
            skillBuffs.Add(new Buff(BuffType.Stat, BuffOrder.Default, "", (int)Obj.치명타율, 1, 999, 0, -1, 0, 0));

        LogManager.instance.AddLog($"{name}(이)가 {skill.name}(을)를 시전했습니다.");
        //skill 효과 순차적으로 계산
        Active_Effect(skill, selects);
        SoundManager.instance.PlaySFX(skill.sfx);

        //36 피니셔
        if (skill.category == 1000)
            punchCount++;
        if (skill.category == 1002)
            selfImproveCount++;

        //32 콤비네이션 히트
        resentCategory = skill.category;

        //자가개선 4세트 - 자버프 2번당 무작위 버프 1개 해제
        if ((selfImproveCount & 1) == 1 && ItemManager.GetSetData(3).Value[2] > 0)
            RemoveDebuff(1);

        orderIdx++;
        buffStat[(int)Obj.currAP] -= GetSkillCost(skill);
        cooldowns[slotIdx] = skill.cooldown;

        StatUpdate_Turn();
    }
    protected override void Active_Effect(Skill skill, List<Unit> selects)
    {
        List<Unit> effectTargets;
        List<Unit> damaged = new List<Unit>();

        int count = skill.effectCount;
        for (int i = 0; i < count; i++)
        {
            effectTargets = GetEffectTarget(selects, damaged, skill.effectTarget[i]);
            float stat = GetEffectStat(selects, skill.effectStat[i]);

            switch ((EffectType)skill.effectType[i])
            {
                //데미지 - 스킬 버프 계산 후 
                case EffectType.Damage:
                    {
                        StatUpdate_Skill(skill);

                        //skillEffectRate가 기본적으로 음수
                        float dmg = stat * skill.effectRate[i];

                        foreach (Unit u in effectTargets)
                        {
                            int acc = 20;
                            if (buffStat[(int)Obj.명중률] >= u.buffStat[(int)Obj.회피율])
                                acc = 60 + 6 * (buffStat[(int)Obj.명중률] - u.buffStat[(int)Obj.회피율]) / (u.LVL + 2);
                            else
                                acc = Mathf.Max(acc, 60 + 6 * (buffStat[(int)Obj.명중률] - u.buffStat[(int)Obj.회피율]) / (LVL + 2));

                            if (Random.Range(0, 100) < acc)
                            {
                                isAcc = true;
                                accCount++;

                                //23 싸움의 리듬 - 3회 적중 성공 시 이번 전투 동안 spd 버프
                                if (HasSkill(23) && accCount >= 3)
                                {
                                    accCount = 0;
                                    AddBuff(this, -4, SkillManager.GetSkill(1, 23), 0, 0);
                                }

                                isCrit = Random.Range(0, 100) < buffStat[(int)Obj.치명타율];

                                u.GetDamage(this, dmg, buffStat[(int)Obj.방어력무시], isCrit ? buffStat[(int)Obj.치명타피해] : 100);
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
                case EffectType.Heal:
                    {
                        float heal = stat * skill.effectRate[i];

                        foreach (Unit u in effectTargets)
                            u.GetHeal(skill.effectCalc[i] == 1 ? heal * u.buffStat[(int)Obj.체력] : heal);
                        break;
                    }
                case EffectType.Active_Buff:
                    {
                        if (skill.effectCond[i] == 0 || (skill.effectCond[i] == 1 && isAcc) || (skill.effectCond[i] == 2 && isCrit))
                            foreach (Unit u in effectTargets)
                                u.AddBuff(this, orderIdx, skill, i, stat);
                        break;
                    }
                case EffectType.Active_Debuff:
                    {
                        if (skill.effectCond[i] == 0 || (skill.effectCond[i] == 1 && isAcc) || (skill.effectCond[i] == 2 && isCrit))
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
                        //머신건 히트 히트수 결정
                        if (buffStat[(int)Obj.속도] <= 13)
                            count = 4;
                        else if (buffStat[(int)Obj.속도] <= 25)
                            count = 5;
                        else if (buffStat[(int)Obj.속도] <= 38)
                            count = 6;
                        else
                            count = 7;

                        //소닉붐 3세트 - 머신건 히트 수 추가
                        if (ItemManager.GetSetData(2).Value[1] > 0)
                            count++;
                        break;
                    }
                case EffectType.CharSpecial2:
                    {
                        AddBuff(this, orderIdx, skill, i, 0);
                        StatUpdate_Skill(skill);

                        chargingPunch = new ChargingPunch(selects[0], buffStat[(int)Obj.공격력], buffStat[(int)Obj.명중률]);
                        //AP 값 0으로
                        buffStat[(int)Obj.행동력] = buffStat[(int)Obj.currAP] = 0;
                        break;
                    }
                default:
                    break;
            }
        }
    }

    protected override void Passive_BattleStart()
    {
        List<Unit> effectTargets;
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

                //소닉붐 4세트 - 타임드 윙어 페널티 삭제
                if (s.idx == 40 && i == 0 && ItemManager.GetSetData(2).Value[2] > 0)
                    continue;

                switch ((EffectType)s.effectType[i])
                {
                    case EffectType.Passive_HasSkillBuff:
                        {
                            if (HasSkill(s.effectCond[i], true))
                                foreach (Unit u in effectTargets)
                                    u.AddBuff(this, -2, s, i, 0);
                            break;
                        }
                    case EffectType.Passive_HasSkillDebuff:
                        {
                            if (HasSkill(s.effectCond[i], true))
                                foreach (Unit u in effectTargets)
                                    u.AddDebuff(this, -2, s, i, 0);
                            break;
                        }
                    case EffectType.Passive_EternalBuff:
                        {
                            foreach (Unit u in effectTargets)
                                u.AddBuff(this, -2, s, i, 0);
                            break;
                        }
                    case EffectType.Passive_EternalDebuff:
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
    protected override void Passive_SkillCast(Skill active)
    {
        for (int j = 0; j < passiveIdxs.Length; j++)
        {
            Skill skill = SkillManager.GetSkill(classIdx, passiveIdxs[j]);

            //32 콤비네이션 히트
            if (skill.idx == 32)
            {
                if (active.category == 1000 && resentCategory == 1001 || active.category == 1001 && resentCategory == 1000)
                {
                    //종합 타격 4세트 - 버프율 증가
                    float rate = skill.effectRate[0] * (1 +  ItemManager.GetSetData(1).Value[1]);
                    turnBuffs.Add(new Buff(BuffType.Stat, new BuffOrder(this, orderIdx), skill.name, skill.effectObject[0], skill.effectStat[0], rate, skill.effectCalc[0], skill.effectTurn[0], skill.effectDispel[0], skill.effectVisible[0]));
                }
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

        //자가개선 2세트 - 자버프 스킬 ATK 상승
        KeyValuePair<string, float[]> set = ItemManager.GetSetData(3);
        if (set.Value[0] > 0 && active.category == 1002)
        {
            skillBuffs.Add(new Buff(BuffType.Stat, BuffOrder.Default, "", (int)Obj.공격력, 1, set.Value[0], 1, -1));
        }
    }

    public class ChargingPunch
    {
        public Unit target;
        public float atk;
        public int acc;

        public ChargingPunch()
        {
            target = null;
            atk = 0; acc = 0;
        }
        public ChargingPunch(Unit u, float a, int ac)
        {
            target = u;
            atk = a;
            acc = ac;
        }
    }
}
