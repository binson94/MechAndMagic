using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Character : Unit
{
    int debuffImmune = 0;
    bool immunePotion = false;
    //전투 시작 시 1번만 호출
    int[] skillCount = new int[2];

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
            turnBuffs.Add(new Buff(BuffType.Stat, LVL, new BuffOrder(this), set.Key, (int)Obj.DEF, buffStat[(int)Obj.HP], set.Value[0], 0, 99, 0, 1));
        //아이언하트 4세트 - CRC 상승
        if (set.Value[2] > 0)
            turnBuffs.Add(new Buff(BuffType.Stat, LVL, new BuffOrder(this), set.Key, (int)Obj.CRC, 1, set.Value[2], 1, 99, 0, 1));

        set = ItemManager.GetSetData(26);
        //시계탑의 대리인 2세트 - AP 최대값 상승
        if (set.Value[0] > 0)
            turnBuffs.Add(new Buff(BuffType.Stat, LVL, new BuffOrder(this), set.Key, (int)Obj.AP, 1, set.Value[0], 0, 99, 0, 1));

        //메탈 그리드 4세트 - 매 전투 2번 디버프 면역
        if (ItemManager.GetSetData(28).Value[1] > 0)
            debuffImmune = 2;

        //완벽한 톱니바퀴 2세트 - ACC 상승
        set = ItemManager.GetSetData(29);
        if (set.Value[0] > 0)
            turnBuffs.Add(new Buff(BuffType.Stat, LVL, new BuffOrder(this, orderIdx), set.Key, (int)Obj.ACC, 1, set.Value[0], 1, 1, 0, 1));

        set = ItemManager.GetSetData(27);
        List<Unit> targets = GetEffectTarget(null, null, 6);
        //몰락한 세력의 세트 - 몬스터 디버프
        if (set.Value[0] > 0)
            foreach (Unit u in targets)
            {
                u.turnDebuffs.Add(new Buff(BuffType.Stat, LVL, new BuffOrder(this), set.Key, (int)Obj.SPD, 1, set.Value[0], 1, 99, 0, 1));
                if (set.Value[1] > 0)
                    u.turnDebuffs.Add(new Buff(BuffType.Stat, LVL, new BuffOrder(this), set.Key, (int)Obj.DOG, 1, set.Value[1], 1, 99, 0, 1));
                if (set.Value[2] > 0)
                    u.turnDebuffs.Add(new Buff(BuffType.Stat, LVL, new BuffOrder(this), set.Key, (int)Obj.DEF, 1, set.Value[2], 1, 99, 0, 1));
            }

        set = ItemManager.GetSetData(33);
        //행운의 클로버 2세트 - CRC, CRB가 LVL 비례 상승
        if (set.Value[0] > 0)
        {
            turnBuffs.Add(new Buff(BuffType.Stat, LVL, new BuffOrder(this, -1), set.Key, (int)Obj.CRC, LVL, set.Value[0], 1, 99, 0, 1));
            turnBuffs.Add(new Buff(BuffType.Stat, LVL, new BuffOrder(this, -1), set.Key, (int)Obj.CRB, LVL, set.Value[0], 1, 99, 0, 1));
        }
    }
    public override void OnTurnStart()
    {
        base.OnTurnStart();

        //메탈 그리드 2세트 - 매 턴 1회 피격 DEF 상승
        KeyValuePair<string, float[]> set = ItemManager.GetSetData(28);
        if (set.Value[0] > 0)
            turnBuffs.Add(new Buff(BuffType.Stat, LVL, new BuffOrder(this, orderIdx), set.Key, (int)Obj.DEF, 1, set.Value[0], 1, 1, 0, 1));        

        set = ItemManager.GetSetData(29);
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

        set = ItemManager.GetSetData(30);
        //스타더스트 2세트 = 턴 시작 시 랜덤 적 1인에게 피해
        if (set.Value[0] > 0)
        {
            int crit = Random.Range(0, 100) < buffStat[(int)Obj.CRC] ? buffStat[(int)Obj.CRB] : 100;
            GetEffectTarget(null, null, 4)[0].GetDamage(this, buffStat[(int)Obj.ATK] * set.Value[0], buffStat[(int)Obj.PEN], crit);
        }

        set = ItemManager.GetSetData(32);
        //밀물과 썰물 3세트 - 턴 시작 시 잃은 체력 비례 회복
        if (set.Value[1] > 0)
            GetHeal(set.Value[1] * (buffStat[(int)Obj.HP] - buffStat[(int)Obj.currHP]));

        immunePotion = false;
        skillCount[1] = 0;
    }
    public override void OnTurnEnd()
    {
        base.OnTurnEnd();
        KeyValuePair<string, float[]> set = ItemManager.GetSetData(26);

        //시계탑의 대리인 3세트 - 턴 종 시 SPD 상승(무제한)
        if (set.Value[1] > 0)
            turnBuffs.Add(new Buff(BuffType.Stat, LVL, new BuffOrder(this), set.Key, (int)Obj.SPD, 1, set.Value[1], 1, 99, 0, 1));

        set = ItemManager.GetSetData(30);
        //스타더스트 3세트 - 턴 종 시 랜덤 적 1인에게 피해
        if (set.Value[1] > 0)
        {
            int crit = Random.Range(0, 100) < buffStat[(int)Obj.CRC] ? buffStat[(int)Obj.CRB] : 100;
            GetEffectTarget(null, null, 4)[0].GetDamage(this, buffStat[(int)Obj.ATK] * set.Value[1], buffStat[(int)Obj.PEN], crit);
        }

        set = ItemManager.GetSetData(32);
        //밀물과 썰물 4세트 - 턴 종료 시, 이번 턴에 준 피해 비례 회복
        if (set.Value[2] > 0)
            GetHeal(set.Value[2] * dmgs[0]);
    }

    protected void CountSkill()
    {
        skillCount[0]++;
        skillCount[1]++;

        KeyValuePair<string, float[]> set = ItemManager.GetSetData(34);
        //미스틱 2세트 - 한 턴에 3번 스킬 사용 시 AP 소량 회복
        if (set.Value[0] > 0 && skillCount[1] % 3 == 0)
            GetAPHeal(set.Value[0]);
        //미스틱 4세트 - 6번 스킬 사용 시 AP 소량 회복
        if (set.Value[1] > 0 && skillCount[0] % 6 == 0)
            GetAPHeal(set.Value[1]);
    }

    public string CanUsePotion(int potionIdx)
    {
        Potion p = ItemManager.GetPotion(potionIdx);
        if(potionIdx == 0)
            return "포션이 없습니다.";
        if(potionIdx == 10 || potionIdx == 17)
            return p.rate[0] >= buffStat[(int)Obj.currHP] ? "체력 부족" : "";
        else if(potionIdx == 18)
            return p.rate[0] > buffStat[(int)Obj.currAP] ? "AP 부족" : "";
        else
            return "";
    }
    public void UsePotion(int potionIdx)
    {
        Potion p = ItemManager.GetPotion(potionIdx);

        switch (potionIdx)
        {
            //회복 포션 - HP 회복
            case 1:
                {
                    GetHeal(p.rate[0]);
                    break;
                }
            //활력 포션 - AP 회복
            case 2:
                {
                    GetAPHeal(p.rate[0]);
                    break;
                }
            //정화 포션 - 모든 디버프 제거
            case 3:
                {
                    RemoveDebuff(turnDebuffs.Count);
                    break;
                }
            //분노 포션 - 2턴 ATK, CRB 버프
            case 4:
                {
                    turnBuffs.Add(new Buff(BuffType.Stat, LVL, new BuffOrder(this), p.name, (int)Obj.ATK, 1, p.rate[0], 1, 2, 0, 1));
                    turnBuffs.Add(new Buff(BuffType.Stat, LVL, new BuffOrder(this), p.name, (int)Obj.CRB, 1, p.rate[1], 1, 2, 0, 1));
                    break;
                }
            //집중 포션 - 2턴 ACC, CRC 버프
            case 5:
                {
                    turnBuffs.Add(new Buff(BuffType.Stat, LVL, new BuffOrder(this), p.name, (int)Obj.ACC, 1, p.rate[0], 1, 2, 0, 1));
                    turnBuffs.Add(new Buff(BuffType.Stat, LVL, new BuffOrder(this), p.name, (int)Obj.CRC, 1, p.rate[1], 1, 2, 0, 1));
                    break;
                }
            //신속 포션 - 2턴 DOG, SPD 버프
            case 6:
                {
                    turnBuffs.Add(new Buff(BuffType.Stat, LVL, new BuffOrder(this), p.name, (int)Obj.DOG, 1, p.rate[0], 1, 2, 0, 1));
                    turnBuffs.Add(new Buff(BuffType.Stat, LVL, new BuffOrder(this), p.name, (int)Obj.SPD, 1, p.rate[1], 1, 2, 0, 1));
                    break;
                }
            //인내 포션 - 2턴 DEF 버프
            case 7:
                {
                    turnBuffs.Add(new Buff(BuffType.Stat, LVL, new BuffOrder(this), p.name, (int)Obj.DEF, 1, p.rate[0], 1, 2, 0, 1));
                    break;
                }
            //관통 포션 - 2턴 PEN 버프
            case 8:
                {
                    turnBuffs.Add(new Buff(BuffType.Stat, LVL, new BuffOrder(this), p.name, (int)Obj.PEN, 1, p.rate[0], 1, 2, 0, 1));
                    break;
                }
            //안정화 포션 - HP 회복, 디버프 1개 제거
            case 9:
                {
                    GetHeal(p.rate[0]);
                    RemoveDebuff(1);
                    break;
                }
            //불안정 포션 - HP 소모, AP 회복
            case 10:
                {
                    buffStat[(int)Obj.currHP] -= Mathf.RoundToInt(p.rate[0]);
                    GetAPHeal(p.rate[1]);
                    break;
                }
            //석화 포션 - 2턴 SPD 디버프, 이번 전투 DEF 버프
            case 11:
                {
                    turnDebuffs.Add(new Buff(BuffType.Stat, LVL, new BuffOrder(this), p.name, (int)Obj.SPD, 1, p.rate[0], 1, 2, 1, 1));
                    turnBuffs.Add(new Buff(BuffType.Stat, LVL, new BuffOrder(this), p.name, (int)Obj.DEF, 1, p.rate[1], 1, 99, 0, 1));
                    break;
                }
            //경화 포션 - 2턴 DEF 디버프, 이번 전투 SPD 버프
            case 12:
                {
                    turnDebuffs.Add(new Buff(BuffType.Stat, LVL, new BuffOrder(this), p.name, (int)Obj.DEF, 1, p.rate[0], 1, 2, 1, 1));
                    turnBuffs.Add(new Buff(BuffType.Stat, LVL, new BuffOrder(this), p.name, (int)Obj.SPD, 1, p.rate[1], 1, 99, 0, 1));
                    break;
                }
            //수호 포션 - 1턴 디버프 면역
            case 14:
                {
                    immunePotion = true;
                    break;
                }
            //각성 포션 - 1턴 ATK, ACC, CRC, CRB 버프
            case 15:
                {
                    turnBuffs.Add(new Buff(BuffType.Stat, LVL, new BuffOrder(this), p.name, (int)Obj.ATK, 1, p.rate[0], 1, 1, 0, 1));
                    turnBuffs.Add(new Buff(BuffType.Stat, LVL, new BuffOrder(this), p.name, (int)Obj.ACC, 1, p.rate[1], 1, 1, 0, 1));
                    turnBuffs.Add(new Buff(BuffType.Stat, LVL, new BuffOrder(this), p.name, (int)Obj.CRC, 1, p.rate[2], 1, 1, 0, 1));
                    turnBuffs.Add(new Buff(BuffType.Stat, LVL, new BuffOrder(this), p.name, (int)Obj.CRB, 1, p.rate[3], 1, 1, 0, 1));
                    break;
                }
            //해방 포션 - 1턴 DOG, SPD 버프
            case 16:
                {
                    turnBuffs.Add(new Buff(BuffType.Stat, LVL, new BuffOrder(this), p.name, (int)Obj.DOG, 1, p.rate[0], 1, 1, 0, 1));
                    turnBuffs.Add(new Buff(BuffType.Stat, LVL, new BuffOrder(this), p.name, (int)Obj.SPD, 1, p.rate[1], 1, 1, 0, 1));
                    break;
                }
            //최후의 포션 - 체력 소모, AP 전체 회복
            case 17:
                {
                    buffStat[(int)Obj.currHP] -= Mathf.RoundToInt(p.rate[0]);
                    GetAPHeal(buffStat[(int)Obj.AP]);
                    break;
                }
            //활력 포션 - AP 소모, 체력 전체 회복
            case 18:
                {
                    buffStat[(int)Obj.currAP] -= Mathf.RoundToInt(p.rate[0]);
                    GetHeal(buffStat[(int)Obj.HP]);
                    break;
                }
        }
    }

    protected void OnCrit()
    {
        //행운의 클로버 3세트 - 치명타 시 디버프 1개 해제
        KeyValuePair<string, float[]> set = ItemManager.GetSetData(33);
        if (set.Value[1] > 0 && Random.Range(0, 100) < buffStat[(int)Obj.CRC])
            RemoveDebuff(1);
    }
    protected void OnKill()
    {
        KeyValuePair<string, float[]> set = ItemManager.GetSetData(30);
        //스타더스트 4세트 - 적 처치 시 랜덤 적 1인 2타 피해
        if (set.Value[2] > 0)
        {
            Unit u = GetEffectTarget(null, null, 4)[0];
            for (int i = 0; i < 2; i++)
            {
                int crit = Random.Range(0, 100) < buffStat[(int)Obj.CRC] ? buffStat[(int)Obj.CRB] : 100;
                u.GetDamage(this, buffStat[(int)Obj.ATK] * set.Value[2], buffStat[(int)Obj.PEN], crit);
            }
        }

        set = ItemManager.GetSetData(31);
        //뿌리 깊은 나무 2세트 - 적 처치 시 HP 회복
        if (set.Value[0] > 0)
            GetHeal(set.Value[0]);
        //뿌리 깊은 나무 3세트 - 적 처치 시 AP 회복, 디버프 1개 해제
        if (set.Value[1] > 0)
        {
            GetAPHeal(set.Value[1]);
            RemoveDebuff(1);
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

        QuestManager.DiehardUpdate((float)buffStat[(int)Obj.currHP] / buffStat[(int)Obj.HP]);

        KeyValuePair<string, float[]> set = ItemManager.GetSetData(28);

        if (set.Value[0] > 0)
            turnBuffs.buffs.RemoveAll(x => x.name == set.Key);

        return killed;
    }

    public override void AddDebuff(Unit caster, int order, Skill s, int effectIdx, float rate)
    {
        //메탈 그리드 4세트 - 디버프 면역
        if (caster != null && caster != this && (debuffImmune > 0 || immunePotion))
        {
            if (!immunePotion)
                debuffImmune--;
            return;
        }

        base.AddDebuff(caster, order, s, effectIdx, rate);
    }
    public override void GetHeal(float heal)
    {
        //밀물과 썰물 2세트 - 모든 회복량 증가
        float rate = 1 + ItemManager.GetSetData(32).Value[0];
        base.GetHeal(heal * rate);
    }
    public override void StatLoad()
    {
        for (int i = 0; i < 12; i++)
            dungeonStat[i] = GameManager.slotData.itemStats[i];
    }
}