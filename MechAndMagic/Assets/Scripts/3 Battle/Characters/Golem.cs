using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Golem : Character
{

    MadScientist ms;
    Queue<KeyValuePair<int, List<Unit>>> skillQueue = new Queue<KeyValuePair<int, List<Unit>>>();
    bool isImmuneCrit = false;

    public void GolemInit(MadScientist ms)
    {
        this.ms = ms;
        //궁극의 피조물 2세트 - 140 ~ 143 버프율 상승, 기초 과학자 3세트 - 140 ~ 143 버프율 상승
        float rate = 1 + ItemManager.GetSetData(11).Value[0] + ItemManager.GetSetData(12).Value[1];
        //140 골렘 경량화 - 속도 상승, 최대 체력 감소
        if (ms.HasSkill(140))
        {
            Skill tmp = SkillManager.GetSkill(ms.classIdx, 140);
            turnBuffs.Add(new Buff(BuffType.Stat, ms.LVL, new BuffOrder(ms), tmp.name, tmp.effectObject[0], tmp.effectStat[0], tmp.effectRate[0] * rate, tmp.effectCalc[0], tmp.effectTurn[0], tmp.effectDispel[0], tmp.effectVisible[0]));
            AddBuff(ms, -1, tmp, 0, 0);
            AddDebuff(ms, -1, tmp, 1, 0);
        }
        //141 골렘 중량화 - 체력 상승, 속도 감소
        if (ms.HasSkill(141))
        {
            Skill tmp = SkillManager.GetSkill(ms.classIdx, 141);
            turnBuffs.Add(new Buff(BuffType.Stat, ms.LVL, new BuffOrder(ms), tmp.name, tmp.effectObject[0], tmp.effectStat[0], tmp.effectRate[0] * rate, tmp.effectCalc[0], tmp.effectTurn[0], tmp.effectDispel[0], tmp.effectVisible[0]));
            AddDebuff(ms, -1, tmp, 1, 0);
        }
        //142 골렘 무기 강화 - 공증
        if (ms.HasSkill(142))
        {
            Skill tmp = SkillManager.GetSkill(ms.classIdx, 142);
            turnBuffs.Add(new Buff(BuffType.Stat, ms.LVL, new BuffOrder(ms), tmp.name, tmp.effectObject[0], tmp.effectStat[0], tmp.effectRate[0] * rate, tmp.effectCalc[0], tmp.effectTurn[0], tmp.effectDispel[0], tmp.effectVisible[0]));
        }
        if (ms.HasSkill(143))
        {
            Skill tmp = SkillManager.GetSkill(ms.classIdx, 143);
            turnBuffs.Add(new Buff(BuffType.Stat, ms.LVL, new BuffOrder(ms), tmp.name, tmp.effectObject[0], tmp.effectStat[0], tmp.effectRate[0] * rate, tmp.effectCalc[0], tmp.effectTurn[0], tmp.effectDispel[0], tmp.effectVisible[0]));
        }
        isImmuneCrit = ms.HasSkill(163);
    }

    public override void OnTurnStart()
    {
        base.OnTurnStart();
        if (skillQueue.Count <= 0)
            ActiveSkill(9, new List<Unit>());
        else
            while (skillQueue.Count > 0)
            {
                KeyValuePair<int, List<Unit>> token = skillQueue.Dequeue();
                ActiveSkill(token.Key, token.Value);
            }
    }
    public override void OnTurnEnd()
    {
        base.OnTurnEnd();
        skillQueue.Clear();
    }

    public override void ActiveSkill(int idx, List<Unit> selects)
    { //적중 성공 여부
        isAcc = true;
        //크리티컬 성공 여부
        isCrit = false;


        //skillDB에서 스킬 불러오기
        Skill skill = SkillManager.GetSkill(classIdx, idx);

        skillBuffs.Clear();
        skillDebuffs.Clear();

        if (skill == null)
        {
            Debug.LogError("skill is null");
            return;
        }

        Passive_SkillCast(skill);

        //skill 효과 순차적으로 계산
        Active_Effect(skill, selects);

        if (skill.idx == 6)
            AddBuff(this, ++orderIdx, skill, 1, 0);

        turnBuffs.buffs.RemoveAll(x => x.name == SkillManager.GetSkill(classIdx, 6).name && x.objectIdx[0] == 5);

        orderIdx++;
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
                        foreach(Unit u in effectTargets)
                        {
                            if (!u.isActiveAndEnabled)
                                continue;

                            int acc = 20;
                            if (buffStat[(int)Obj.ACC] >= u.buffStat[(int)Obj.DOG])
                                acc = 60 + 6 * (buffStat[(int)Obj.ACC] - u.buffStat[(int)Obj.DOG]) / (u.LVL + 2);
                            else
                                acc = Mathf.Max(acc, 60 + 6 * (buffStat[(int)Obj.ACC] - u.buffStat[(int)Obj.DOG]) / (LVL + 2));

                            //명중 시
                            if (Random.Range(0, 100) < acc)
                            {
                                isAcc = true;
                                isCrit = Random.Range(0, 100) < buffStat[(int)Obj.CRC];

                                bool kill = u.GetDamage(this, dmg, buffStat[(int)Obj.PEN], isCrit ? buffStat[(int)Obj.CRB] : 100).Key;
                                damaged.Add(u);

                                Passive_SkillHit(skill);

                                if(ItemManager.GetSetData(11).Value[2] > 0 && kill)
                                    ms.GolemKills();
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
                            foreach (Unit u in effectTargets)
                                AddDebuff(this, orderIdx, skill, i, stat);
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
                default:
                    break;
            }
        }
    }
    


    public override KeyValuePair<bool, int> GetDamage(Unit caster, float dmg, int pen, int crb)
    {
        if (isImmuneCrit) crb = 100;
        return base.GetDamage(caster, dmg, pen, crb);
    }
    public void AddControl(KeyValuePair<int, List<Unit>> token) => skillQueue.Enqueue(token);
}
