using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Elemental : Unit
{
    ///<summary> 정령 타입(불 1007, 물 1008, 바람 1009) </summary>
    public int type;
    ///<summary> 강화 정령 </summary>
    public bool isUpgraded;

    int pattern = 0;

    public void Summon(BattleManager bm, ElementalController ec, int type, bool upgrade = false)
    {
        BM = bm;
        isUpgraded = upgrade;
        this.type = type;
        LVL = ec.LVL;

        StatLoad(ec);
        SkillBuff(ec);
        StatUpdate_Turn();
        buffStat[(int)Obj.currHP] = buffStat[(int)Obj.체력];

        SkillSet();
    }

    public override void OnTurnStart()
    {
        base.OnTurnStart();

        if(!IsStun())
            ElementalSkill();
    }

    void SkillSet()
    {
        int pivot = isUpgraded ? 5 : -1;
        for (int i = 0; i < 2; i++)
            activeIdxs[i] = (type - 1006) * 2 + i + pivot;
    }
    void ElementalSkill()
    {
        ActiveSkill(pattern++, new List<Unit>());
        pattern %= 2;
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
                        damaged.Clear();
                        StatUpdate_Skill(skill);

                        float dmg = GetEffectStat(selects, skill.effectStat[i]) * skill.effectRate[i];

                        foreach (Unit u in effectTargets)
                        {
                            int acc = 20;
                            if (buffStat[(int)Obj.명중] >= u.buffStat[(int)Obj.회피])
                                acc = 6 * (buffStat[(int)Obj.명중] - u.buffStat[(int)Obj.회피]) / (u.LVL + 2);
                            else
                                acc = 6 * (buffStat[(int)Obj.명중] - u.buffStat[(int)Obj.회피]) / (LVL + 2);
                            
                            acc = Mathf.Max(20, acc);

                            if (Random.Range(0, 100) < acc)
                            {
                                isAcc = true;
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
                case EffectType.DoNothing:
                    break;
                default:
                    ActiveDefaultCase(skill, i, effectTargets, GetEffectStat(selects, skill.effectStat[i]));
                    break;
            }
        }
    }


    void StatLoad(ElementalController ec)
    {
        dungeonStat[0] = 1;
        for (Obj i = Obj.체력; i <= Obj.속도; i++)
            if(isUpgraded)
            {
                switch(i)
                {
                    case Obj.체력:
                    case Obj.방어력:
                        dungeonStat[(int)i] = Mathf.RoundToInt(0.8f * ec.dungeonStat[(int)i]);
                        break;
                    case Obj.공격력:
                    case Obj.치명타율:
                    case Obj.치명타피해:
                    case Obj.방어력무시:
                        dungeonStat[(int)i] = ec.dungeonStat[(int)i];
                        break;
                    case Obj.명중:
                        dungeonStat[(int)i] = Mathf.RoundToInt(1.1f * ec.dungeonStat[(int)i]);
                        break;
                    case Obj.회피:
                        dungeonStat[(int)i] = Mathf.RoundToInt(0.5f * ec.dungeonStat[(int)i]);
                        break;
                    case Obj.속도:
                        dungeonStat[(int)i] = Mathf.RoundToInt(1.2f * ec.dungeonStat[(int)i]);
                        break;
                }
            }
            else
            {
                switch(i)
                {
                    case Obj.체력:
                        dungeonStat[(int)i] = Mathf.RoundToInt(0.6f * ec.dungeonStat[(int)i]);
                        break;
                    case Obj.공격력:
                    case Obj.치명타율:
                    case Obj.치명타피해:
                        dungeonStat[(int)i] = Mathf.RoundToInt(0.7f * ec.dungeonStat[(int)i]);
                        break;
                    case Obj.명중:
                    case Obj.속도:
                        dungeonStat[(int)i] = Mathf.RoundToInt(0.9f * ec.dungeonStat[(int)i]);
                        break;
                    case Obj.방어력:
                    case Obj.회피:
                        dungeonStat[(int)i] = Mathf.RoundToInt(0.5f * ec.dungeonStat[(int)i]);
                        break;
                    case Obj.방어력무시:
                        dungeonStat[(int)i] = ec.dungeonStat[(int)i];
                        break;
                }
            }

        dungeonStat[1] = dungeonStat[2];
    }
    void SkillBuff(ElementalController ec)
    {
        //정령의 대리인 2세트 - 정령 힘, 생명 부여 강화
        float rate = 1 + ItemManager.GetSetData(13).Value[0];

        //194 정령 힘 부여
        if (ec.HasSkill(194))
        {
            Skill s= SkillManager.GetSkill(5, 194);
            turnBuffs.Add(new Buff(BuffType.Stat, new BuffOrder(ec), s.name, s.effectObject[0], ec.buffStat[s.effectStat[0]], s.effectRate[0] * rate, s.effectCalc[0], s.effectTurn[0], s.effectDispel[0], s.effectVisible[0]));
        }
        //195 정령 생명 부여
        if (ec.HasSkill(195))
        {
            Skill s= SkillManager.GetSkill(5, 195);
            turnBuffs.Add(new Buff(BuffType.Stat, new BuffOrder(ec), s.name, s.effectObject[0], ec.buffStat[s.effectStat[0]], s.effectRate[0] * rate, s.effectCalc[0], s.effectTurn[0], s.effectDispel[0], s.effectVisible[0]));
        }
        //202 화염 고급 숙달
        if (ec.HasSkill(202) && type == 1007)
            AddBuff(ec, -2, SkillManager.GetSkill(5, 202), 1, 0);
        //203 물 고급 숙달
        if (ec.HasSkill(203) && type == 1008)
            AddBuff(ec, -2, SkillManager.GetSkill(5, 203), 1, 0);
        //204 바람 고급 숙달
        if (ec.HasSkill(204) && type == 1009)
            AddBuff(ec, -2, SkillManager.GetSkill(5, 204), 1, 0);
    }
}
