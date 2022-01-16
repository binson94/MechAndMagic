using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Character : Unit
{
    //전투 시작 시 1번만 호출
    public override void OnBattleStart(BattleManager BM)
    {
        this.BM = BM;

        StatLoad();
        Passive_BattleStart();
        StatUpdate_Turn();

        for (int i = 0; i < 6; i++)
            cooldowns[i] = 0;
    }

    protected override void Passive_BattleStart()
    {
        List<Unit> effectTargets;
        //passive
        for (int j = 0; j < 3; j++)
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

                switch ((SkillType)s.effectType[i])
                {
                    case SkillType.Passive_HasSkillBuff:
                        {
                            if (HasSkill(s.effectCond[i], true))
                                foreach (Unit u in effectTargets)
                                    u.AddBuff(s, i, 0);
                            break;
                        }
                    case SkillType.Passive_HasSkillDebuff:
                        {
                            if (HasSkill(s.effectCond[i], true))
                                foreach (Unit u in effectTargets)
                                    u.AddDebuff(s, i, 0);
                            break;
                        }
                    case SkillType.Passive_EternalBuff:
                        {
                                foreach (Unit u in effectTargets)
                                    u.AddBuff(s, i, 0);
                            break;
                        }
                    case SkillType.Passive_EternalDebuff:
                        {
                                foreach (Unit u in effectTargets)
                                    u.AddDebuff(s, i, 0);
                            break;
                        }
                    case SkillType.Passive_APBuff:
                        apBuffs.Add(new APBuff(s.name, s.effectTurn[i], s.effectCond[i], s.effectRate[i], s.effectCalc[i] == 1));
                        break;
                    default:
                        break;
                }
            }
        }
    }

    public override void StatLoad()
    {
        for (int i = 0; i < 12; i++)
        {
            dungeonStat[i] = GameManager.slotData.itemStats[i];
            basicStat[i] = SlotData.baseStats[i];
        }
    }
}
