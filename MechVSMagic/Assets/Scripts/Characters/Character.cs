using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Character : Unit
{
    int debuffImmune = 0;
    //전투 시작 시 1번만 호출
    public override void OnBattleStart(BattleManager BM)
    {
        base.OnBattleStart(BM);
        
        Passive_BattleStart();
        StatUpdate_Turn();

        for (int i = 0; i < 6; i++)
            cooldowns[i] = 0;

        //아이언하트 2세트 - 체력 비례 방어력 상승
        KeyValuePair<string, float[]> set = ItemManager.GetSetData(25);
        if (set.Value[0] > 0)
            turnBuffs.Add(new Buff(BuffType.Stat, LVL, new BuffOrder(this), set.Key, (int)Obj.DEF, 1, set.Value[0], 1, 99, 0, 1));
        //아이언하트 4세트 - CRC 상승
        if (set.Value[2] > 0)
            turnBuffs.Add(new Buff(BuffType.Stat, LVL, new BuffOrder(this), set.Key, (int)Obj.CRC, 1, set.Value[2], 1, 99, 0, 1));
    
        set = ItemManager.GetSetData(26);
        //시계탑의 대리인 2세트 - AP 최대값 상승
        if(set.Value[0] > 0)
            turnBuffs.Add(new Buff(BuffType.Stat, LVL, new BuffOrder(this), set.Key, (int)Obj.AP, 1, set.Value[0], 1, 99, 0, 1));
    
        //메탈 그리드 4세트 - 매 전투 2번 디버프 면역
        if(ItemManager.GetSetData(28).Value[1] > 0)
            debuffImmune = 2;

        set = ItemManager.GetSetData(27);
        List<Unit> targets = GetEffectTarget(null, null, 6);
        //몰락한 세력의 세트 - 몬스터 디버프
        if(set.Value[0] > 0)
            foreach(Unit u in targets)
            {
                u.turnDebuffs.Add(new Buff(BuffType.Stat, LVL, new BuffOrder(this), set.Key, (int)Obj.SPD, 1, set.Value[0], 1, 99, 0, 1));
                if(set.Value[1] > 0)
                    u.turnDebuffs.Add(new Buff(BuffType.Stat, LVL, new BuffOrder(this), set.Key, (int)Obj.DOG, 1, set.Value[1], 1, 99, 0, 1));
                if(set.Value[2] > 0)
                    u.turnDebuffs.Add(new Buff(BuffType.Stat, LVL, new BuffOrder(this), set.Key, (int)Obj.DEF, 1, set.Value[2], 1, 99, 0, 1));
            }
    
    }
    public override void OnTurnStart()
    {
        base.OnTurnStart();

        //메탈 그리드 2세트 - 매 턴 1회 피격 DEF 상승
        KeyValuePair<string, float[]> set = ItemManager.GetSetData(28);
        if (set.Value[0] > 0)
            turnBuffs.Add(new Buff(BuffType.Stat, LVL, new BuffOrder(this, orderIdx), set.Key, (int)Obj.DEF, 1, set.Value[0], 1, 1, 0, 1));
        
        //완벽한 톱니바퀴 2세트 - ACC 상승
        set = ItemManager.GetSetData(29);
        if (set.Value[0] > 0)
            turnBuffs.Add(new Buff(BuffType.Stat, LVL, new BuffOrder(this, orderIdx), set.Key, (int)Obj.ACC, 1, set.Value[0], 1, 1, 0, 1));
        //완벽한 톱니바퀴 4세트 - 매 턴 시작 시, 랜덤 스킬 1개 쿨타임 감소
        if (set.Value[1] > 0)
        {
            List<int> idxs = new List<int>();
            for (int i = 0; i < activeIdxs.Length; i++)
                if (cooldowns[i] > 0)
                    idxs.Add(i);
            if (idxs.Count > 0)
            {
                cooldowns[idxs[Random.Range(0, idxs.Count)]]--;
            }
        }
    }
    public override void OnTurnEnd()
    {
        base.OnTurnEnd();
        KeyValuePair<string, float[]> set = ItemManager.GetSetData(26);

        //시계탑의 대리인 3세트 - 턴 종시 SPD 상승(무제한)
        if(set.Value[1] > 0)
            turnBuffs.Add(new Buff(BuffType.Stat, LVL, new BuffOrder(this), set.Key, (int)Obj.SPD, 1, set.Value[1], 1, 99, 0, 1));
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

    public override KeyValuePair<bool, int> GetDamage(Unit caster, float dmg, int pen, int crb)
    {
        KeyValuePair<bool, int> killed = base.GetDamage(caster, dmg, pen, crb);
        KeyValuePair<string, float[]> set = ItemManager.GetSetData(28);
        
        if(set.Value[0] > 0)
            turnBuffs.buffs.RemoveAll(x=>x.name == set.Key);

        return killed;
    }

    public override void AddDebuff(Unit caster, int order, Skill s, int effectIdx, float rate)
    {
        //메탈 그리드 4세트 - 디버프 면역
        if(caster != null && caster != this && debuffImmune > 0)
        {
            debuffImmune--;
            return;
        }

        base.AddDebuff(caster, order, s, effectIdx, rate);
    }
    public override void StatLoad()
    {
        for (int i = 0; i < 12; i++)
            dungeonStat[i] = GameManager.slotData.itemStats[i];
    }
}