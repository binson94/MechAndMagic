using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class VisionMaster : Character
{
    ///<summary> 이번 턴에 최근 사용한 스킬 카테고리, 287 ~ 289에 사용 </summary>
    int resentCategory = 0;
    ///<summary> 전투 중 사용한 스킬 수 
    ///<para> 0 양, 1 음, 2 둘 다 </para> </summary>
    int[] skillCount_battle = new int[3];
    ///<summary> 이번 턴에 사용한 스킬 수 
    ///<para> 0 양, 1 음 </para> </summary>
    int[] skillCount_turn = new int[2];

    ///<summary> 스킬 선택 상태에 따라 달라지는 값
    ///<para> 0 : 양, 1 : 음, 2 : 둘 다 </para> </summary>
    public int skillState = 0;
    ///<summary> 둘 다 동시 시전 시, 0, 1값 순차적으로 가짐 </summary>
    int bothskill;

    public override void OnBattleStart(BattleManager BM)
    {
        base.OnBattleStart(BM);
        
        //완벽한 균형 5세트 - 10레벨일 때, 모든 스텟 증가
        KeyValuePair<string, float[]> set = ItemManager.GetSetData(21);
        if(set.Value[2] > 0 && LVL >= 10)
        {
            turnBuffs.Add(new Buff(BuffType.Stat, new BuffOrder(this, 0), set.Key, (int)Obj.행동력, 1, set.Value[2], 1, 99));
            turnBuffs.Add(new Buff(BuffType.Stat, new BuffOrder(this, 0), set.Key, (int)Obj.공격력, 1, set.Value[2], 1, 99));
            turnBuffs.Add(new Buff(BuffType.Stat, new BuffOrder(this, 0), set.Key, (int)Obj.방어력, 1, set.Value[2], 1, 99));
            turnBuffs.Add(new Buff(BuffType.Stat, new BuffOrder(this, 1), set.Key, (int)Obj.명중, 1, set.Value[2], 1, 99));
            turnBuffs.Add(new Buff(BuffType.Stat, new BuffOrder(this, 1), set.Key, (int)Obj.회피, 1, set.Value[2], 1, 99));
            turnBuffs.Add(new Buff(BuffType.Stat, new BuffOrder(this, 1), set.Key, (int)Obj.치명타율, 1, set.Value[2], 1, 99));
            turnBuffs.Add(new Buff(BuffType.Stat, new BuffOrder(this, 2), set.Key, (int)Obj.치명타피해, 1, set.Value[2], 1, 99));
            turnBuffs.Add(new Buff(BuffType.Stat, new BuffOrder(this, 2), set.Key, (int)Obj.방어력무시, 1, set.Value[2], 1, 99));
            turnBuffs.Add(new Buff(BuffType.Stat, new BuffOrder(this, 2), set.Key, (int)Obj.속도, 1, set.Value[2], 1, 99));
        }
    }
    public override void OnTurnStart()
    {
        base.OnTurnStart();
        for (int i = 0; i < 2; i++) skillCount_turn[i] = 0;

        //294 비전 방패 : 음 - 다음 턴 시작 시 보호막 리필
        if (turnBuffs.buffs.Any(x => x.name == SkillManager.GetSkill(classIdx, 294).name))
            shieldAmount = shieldMax;
        resentCategory = 0;
    }
    public override void OnTurnEnd()
    {
        base.OnTurnEnd();
        Skill tmp;
        KeyValuePair<string, float[]> set;
        //297 플러스 비전 - 이번 턴에 양 스킬만 사용 시 다음 턴에 관통 버프
        if (HasSkill(297) && skillCount_turn[1] == 0)
        {
            tmp = SkillManager.GetSkill(classIdx, 297);
            AddBuff(this, orderIdx, tmp, 0, 0);

            //찬란한 태양 3세트 - 플러스 비전이 DEF도 버프
            set = ItemManager.GetSetData(19);
            if (set.Value[1] > 0)
                turnBuffs.Add(new Buff(BuffType.Stat, new BuffOrder(this, orderIdx), set.Key, (int)Obj.방어력, 1, set.Value[1], 1, 1, 1, 1));
        }
        //298 마이너스 비전 - 이번 턴에 음 스킬만 사용 시 다음 턴에 명중 버프
        if (HasSkill(298) && skillCount_turn[0] == 0)
        {
            tmp = SkillManager.GetSkill(classIdx, 298);
            AddBuff(this, orderIdx, tmp, 0, 0);

            //웅장한 월광 3세트 - 마이너스 비전이 DOG도 버프
            set = ItemManager.GetSetData(20);
            if (set.Value[1] > 0)
                turnBuffs.Add(new Buff(BuffType.Stat, new BuffOrder(this, orderIdx), set.Key, (int)Obj.회피, 1, set.Value[1], 1, 1, 1, 1));

        }
        //299 플랫 비전 - 이번 턴에 양, 음 스킬 사용 수가 같을 시 방어력 버프
        if (HasSkill(299) && skillCount_turn[0] == skillCount_turn[1])
        {
            tmp = SkillManager.GetSkill(classIdx, 299);
            //완벽한 균형 3세트 - 플랫 비전 방어력 상승량 증가
            float rate = 1 + ItemManager.GetSetData(21).Value[1];
            turnBuffs.Add(new Buff(BuffType.Stat, new BuffOrder(this, orderIdx), tmp.name, tmp.effectObject[0], tmp.effectStat[0], tmp.effectRate[0] * rate, tmp.effectCalc[0], tmp.effectTurn[0], tmp.effectDispel[0], tmp.effectVisible[0]));
        }
    }

    public override int GetSkillCost(Skill s)
    {
        //286 조화 - 반대 스킬 사용 시 AP 소모량 1 감소
        return base.GetSkillCost(s) - (HasSkill(286) && s.cooldown > 0 && Mathf.Abs(resentCategory - s.category) == 1 ? 1 : 0);
    }
    public string CanCastSkill(int skillSlotIdx, bool isMinus)
    {
        Skill s = SkillManager.GetSkill(classIdx, activeIdxs[skillSlotIdx] + (isMinus ? 1 : 0));
        string skillName = s.category == 1023 ? $"{s.name} : 양" : s.name;

        if (buffStat[(int)Obj.currAP] < GetSkillCost(s))
            return $"{skillName}(을)를 사용하기 위한 행동력이 부족합니다.";
        else if (cooldowns[skillSlotIdx] > 0)
            return $"{skillName}(은)는 아직 쿨타임입니다.";
        return string.Empty;
    }

    public void ActiveSkill_Both(int slotIdx, List<Unit> selects)
    {
        bothskill = 0;
        ActiveSkill(slotIdx, selects);
        bothskill = 1;
        ActiveSkill(slotIdx, selects);

        buffStat[(int)Obj.currAP] -= Mathf.Min(GetSkillCost(SkillManager.GetSkill(classIdx, activeIdxs[slotIdx])), GetSkillCost(SkillManager.GetSkill(classIdx, activeIdxs[slotIdx] + 1)));
        cooldowns[slotIdx] = Mathf.Min(SkillManager.GetSkill(classIdx, activeIdxs[slotIdx]).cooldown, SkillManager.GetSkill(classIdx, activeIdxs[slotIdx] + 1).cooldown);
    }
    public override void ActiveSkill(int slotIdx, List<Unit> selects)
    {
        //적중 성공 여부
        isAcc = true;
        //크리티컬 성공 여부
        isCrit = false;

        Skill skill;

        //skillDB에서 스킬 불러오기
        if (skillState < 2)
            skill = SkillManager.GetSkill(classIdx, activeIdxs[slotIdx] + skillState);
        else
            skill = SkillManager.GetSkill(classIdx, activeIdxs[slotIdx] + bothskill);


        skillBuffs.Clear();
        skillDebuffs.Clear();

        if (skill == null) return;

        Passive_SkillCast(skill);

        KeyValuePair<string, float[]> set = ItemManager.GetSetData(19);
        //308 햇빛 승천 - 양 스킬 5번 사용 시마다 반드시 치명타
        if (HasSkill(308) && skill.category == 1023 && skillCount_battle[0] % 5 == 0)
        {
            AddBuff(this, orderIdx, SkillManager.GetSkill(classIdx, 308), 0, 0);

            //찬란한 태양 4세트 - 햇빛 승천 발동 시 CRB도 상승
            if (set.Value[2] > 0)
                skillBuffs.Add(new Buff(BuffType.Stat, BuffOrder.Default, "", (int)Obj.치명타피해, 1, set.Value[2], 1, -1));
        }

        if(skill.category == 1023)
            LogManager.instance.AddLog($"{name}(이)가 {skill.name} : 양(을)를 시전했습니다.");
        else
            LogManager.instance.AddLog($"{name}(이)가 {skill.name}(을)를 시전했습니다.");
        //skill 효과 순차적으로 계산
        Active_Effect(skill, selects);
        SoundManager.Instance.PlaySFX(skill.sfx);

        //양 스킬
        if (skill.category == 1023)
        {
            skillCount_turn[0]++;
            skillCount_battle[0]++;

            //찬란한 태양 2세트 - 양 스킬 사용 시 1턴 ATK 버프
            if (set.Value[0] > 0)
                turnBuffs.Add(new Buff(BuffType.Stat, new BuffOrder(this, orderIdx), set.Key, (int)Obj.공격력, 1, set.Value[0], 1, 1, 1, 1));
        }
        //음 스킬
        if (skill.category == 1024)
        {
            skillCount_turn[1]++;
            skillCount_battle[1]++;

            //웅장한 월광 2세트 - 음 스킬 사용 시 1턴 ACC 버프
            set = ItemManager.GetSetData(20);
            if (set.Value[0] > 0)
                turnBuffs.Add(new Buff(BuffType.Stat, new BuffOrder(this, orderIdx), set.Key, (int)Obj.명중, 1, set.Value[0], 1, 1, 1, 1));

        }
        //스킬 카운트 - 동시 발동은 1번만 카운트
        if (skill.category != 1023 || skillState != 2)
            skillCount_battle[2]++;

        //275 양의 생명력 - 양 스킬 3번마다 힐
        if (HasSkill(275) && skill.category == 1023 && skillCount_battle[0] % 3 == 0)
        {
            Skill tmp = SkillManager.GetSkill(classIdx, 275);
            GetHeal(buffStat[tmp.effectStat[0]] * tmp.effectRate[0]);
        }
        //276 음의 생명력 - 음 스킬 3번마다 데미지
        if (HasSkill(276) && skill.category == 1024 && skillCount_battle[1] % 3 == 0)
        {
            Skill tmp = SkillManager.GetSkill(classIdx, 276);
            List<Unit> targets = BM.GetEffectTarget(4);

            if(targets.Count > 0)
            {
                StatUpdate_Turn();
                targets[0].GetDamage(this, buffStat[tmp.effectStat[0]] * tmp.effectRate[0], buffStat[(int)Obj.방어력무시], 100);
            }
        }

        set = ItemManager.GetSetData(21);
        //완벽한 균형 2세트 - 넘치는 생명력이 4회마다 발동
        //277 넘치는 생명력 - 아무 스킬 6번마다 다음 턴 AP 상승 버프
        if (HasSkill(277) && ((set.Value[0] > 0 && skillCount_battle[2] % 4 == 0) || (set.Value[0] == 0 && skillCount_battle[2] % 6 == 0)) && !(skill.category == 1023 && skillState == 2))
            AddBuff(this, orderIdx, SkillManager.GetSkill(classIdx, 277), 0, 0);

        //287 부조화 - 직전 스킬과 카테고리 다르면 무작위 방어력 디버프
        if (HasSkill(287) && Mathf.Abs(resentCategory - skill.category) == 1)
            BM.GetEffectTarget(4)[0].AddDebuff(this, ++orderIdx, SkillManager.GetSkill(classIdx, 287), 0, 0);
        //288 일치 - 직전 스킬과 카테고리 같으면 힐
        if (HasSkill(288) && resentCategory == skill.category)
        {
            //완벽한 균형 3세트 - 일치 회복량 상승
            set = ItemManager.GetSetData(21);
            float rate = 1 + set.Value[1];
            Skill tmp = SkillManager.GetSkill(classIdx, 288);
            GetHeal(buffStat[tmp.effectStat[0]] * tmp.effectRate[0] * rate);
        }


        //309 달빛 낙하 - 음 스킬 6번 사용 시마다 모든 적 명중 디버프
        if (HasSkill(309) && skill.category == 1024 && skillCount_battle[1] % 6 == 0)
        {
            Skill tmp = SkillManager.GetSkill(classIdx, 309);
            ++orderIdx;
            List<Unit> t = BM.GetEffectTarget(6);
            foreach (Unit u in t)
                u.AddDebuff(this, orderIdx, tmp, 0, 0);

            //웅장한 월광 4세트 - 달빛 낙하가 DOG도 디버프
            set = ItemManager.GetSetData(20);
            if (set.Value[2] > 0)
                foreach (Unit u in t)
                    u.turnDebuffs.Add(new Buff(BuffType.Stat, new BuffOrder(this, orderIdx), tmp.name, (int)Obj.회피, 1, set.Value[2], 1, tmp.effectTurn[0], tmp.effectDispel[0], tmp.effectVisible[0]));
        }

        if (skillState != 2)
        {
            buffStat[(int)Obj.currAP] -= GetSkillCost(skill);
            cooldowns[slotIdx] = skill.cooldown;
        }

        orderIdx++;
        resentCategory = skill.category;

        //310 물아일체 - 7번 스킬 사용 시마다 다음 스킬 2번 발동
        if (HasSkill(310) && skillCount_battle[2] % 7 == 0)
            skillState = 2;
        else
            skillState = 0;

        CountSkill();
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

                            //명중 시
                            if (Random.Range(0, 100) < acc)
                            {
                                isAcc = true;
                                //크리티컬 연산 - dmg * CRB
                                isCrit = Random.Range(0, 100) < buffStat[(int)Obj.치명타율];

                                bool kill = u.GetDamage(this, dmg, buffStat[(int)Obj.방어력무시], isCrit ? buffStat[(int)Obj.치명타피해] : 100).Key;
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
                case EffectType.DoNothing:
                    break;
                default:
                    ActiveDefaultCase(skill, i , effectTargets, GetEffectStat(selects, skill.effectStat[i]));
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
        //302, 303 비전 대폭격 - 버프 1.5배로 적용
        if (s.idx == 302 || s.idx == 303)
            for(int i = 1;i <=12 ;i++)
            {
                mulPivot[i] *= 1.5f;
                addPivot[i] *= 1.5f;
            }

        turnDebuffs.GetBuffRate(ref addPivot, ref mulPivot, false);
        skillBuffs.GetBuffRate(ref addPivot, ref mulPivot, true);
        skillDebuffs.GetBuffRate(ref addPivot, ref mulPivot, false);

        for (int i = 0; i < 13; i++)
            if (i != 1 && i != 3)
                buffStat[i] = Mathf.CeilToInt(dungeonStat[i] * mulPivot[i] + addPivot[i]);

    }
}