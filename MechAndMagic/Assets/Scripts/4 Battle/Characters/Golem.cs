using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Golem : Unit
{

    MadScientist ms;
    Queue<int> skillQueue = new Queue<int>();
    bool isImmuneCrit = false;

    ///<summary> Battle Start보다 먼저 호출, 매드 사이언티스트 연결, 패시브 처리 </summary>
    public void GolemInit(MadScientist ms)
    {
        this.ms = ms;
        LVL = ms.LVL;
        
        Skill skill;
        //궁극의 피조물 2세트 - 140 ~ 143 버프율 상승, 기초 과학자 3세트 - 140 ~ 143 버프율 상승
        float stat = 1 + ItemManager.GetSetData(11).Value[0] + ItemManager.GetSetData(12).Value[1];

        //140 골렘 경량화 - 속도 상승, 최대 체력 감소
        if (ms.HasSkill(140))
        {
            skill = SkillManager.GetSkill(ms.classIdx, 140);

            turnBuffs.Add(new Buff(BuffType.Stat, new BuffOrder(ms), skill.name, skill.effectObject[0], stat, skill.effectRate[0], skill.effectCalc[0], skill.effectTurn[0], skill.effectDispel[0], skill.effectVisible[0]));
            AddDebuff(ms, -1, skill, 1, 0);
        }
        //141 골렘 중량화 - 체력 상승, 속도 감소
        if (ms.HasSkill(141))
        {
            skill = SkillManager.GetSkill(ms.classIdx, 141);

            turnBuffs.Add(new Buff(BuffType.Stat, new BuffOrder(ms), skill.name, skill.effectObject[0], stat, skill.effectRate[0], skill.effectCalc[0], skill.effectTurn[0], skill.effectDispel[0], skill.effectVisible[0]));
            AddDebuff(ms, -1, skill, 1, 0);
        }
        //142 골렘 무기 강화 - 공격력 증가
        if (ms.HasSkill(142))
        {
            skill = SkillManager.GetSkill(ms.classIdx, 142);

            turnBuffs.Add(new Buff(BuffType.Stat, new BuffOrder(ms), skill.name, skill.effectObject[0], stat, skill.effectRate[0], skill.effectCalc[0], skill.effectTurn[0], skill.effectDispel[0], skill.effectVisible[0]));
        }
        //143 골렘 갑옷 강화 - 방어력 증가
        if (ms.HasSkill(143))
        {
            skill = SkillManager.GetSkill(ms.classIdx, 143);

            turnBuffs.Add(new Buff(BuffType.Stat, new BuffOrder(ms), skill.name, skill.effectObject[0], stat, skill.effectRate[0], skill.effectCalc[0], skill.effectTurn[0], skill.effectDispel[0], skill.effectVisible[0]));
        }
        
        isImmuneCrit = ms.HasSkill(163);
    }

    public override void OnBattleStart(BattleManager BM)
    {
        base.OnBattleStart(BM);
        StatUpdate_Turn();
    }
    public override void OnTurnStart()
    {
        base.OnTurnStart();

        if (!IsStun())
            if (skillQueue.Count <= 0)
            {
                ActiveSkill(9, new List<Unit>());
                SoundManager.Instance.PlaySFX(SkillManager.GetSkill(classIdx, 9).sfx);
            }
            else
                while (skillQueue.Count > 0)
                {
                    int skillIdx = skillQueue.Dequeue();
                    ActiveSkill(skillIdx, new List<Unit>());
                    if(skillQueue.Count <= 0)
                        SoundManager.Instance.PlaySFX(SkillManager.GetSkill(classIdx, skillIdx).sfx);
                }
    }
    public override void OnTurnEnd()
    {
        StatUpdate_Turn();
        skillQueue.Clear();
    }

    public override void ActiveSkill(int skillIdx, List<Unit> selects)
    { 
        //적중 성공 여부
        isAcc = true;
        //크리티컬 성공 여부
        isCrit = false;


        //skillDB에서 스킬 불러오기
        Skill skill = SkillManager.GetSkill(classIdx, skillIdx);

        skillBuffs.Clear();
        skillDebuffs.Clear();

        if (skill == null) return;

        LogManager.instance.AddLog($"{name}(이)가 {skill.name}(을)를 시전했습니다.");
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

        for (int i = 0; i < skill.effectCount; i++)
        {
            effectTargets = GetEffectTarget(selects, damaged, skill.effectTarget[i]);
            
            switch ((EffectType)skill.effectType[i])
            {
                //데미지 - 스킬 버프 계산 후 
                case EffectType.Damage:
                    {
                        StatUpdate_Skill(skill);

                        float dmg = GetEffectStat(effectTargets, skill.effectStat[i]) * skill.effectRate[i];

                        damaged.Clear();
                        foreach(Unit u in effectTargets)
                        {
                            if (!u.isActiveAndEnabled)
                                continue;

                            int acc = 20;
                            if (buffStat[(int)Obj.명중률] >= u.buffStat[(int)Obj.회피율])
                                acc = 6 * (buffStat[(int)Obj.명중률] - u.buffStat[(int)Obj.회피율]) / (u.LVL + 2);
                            else
                                acc = 6 * (buffStat[(int)Obj.명중률] - u.buffStat[(int)Obj.회피율]) / (LVL + 2);
                            
                            acc = Mathf.Max(20, acc);

                            //명중 시
                            if (Random.Range(0, 100) < acc)
                            {
                                isAcc = true;
                                isCrit = Random.Range(0, 100) < buffStat[(int)Obj.치명타율];

                                bool kill = u.GetDamage(this, dmg, buffStat[(int)Obj.방어력무시], isCrit ? buffStat[(int)Obj.치명타피해] : 100).Key;
                                damaged.Add(u);

                                Passive_SkillHit(skill);

                                if(ItemManager.GetSetData(11).Value[2] > 0 && kill)
                                    ms.GolemKills();
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
                    ActiveDefaultCase(skill, i, effectTargets, GetEffectStat(effectTargets, skill.effectStat[i]));
                    break;
            }
        }
    }
    
    public override KeyValuePair<bool, int> GetDamage(Unit caster, float dmg, int pen, int crb)
    {
        if (isImmuneCrit) crb = 100;
        return base.GetDamage(caster, dmg, pen, crb);
    }
    public void AddControl(int skillIdx) => skillQueue.Enqueue(skillIdx);

    ///<summary> 스텟 정보 불러오기, 치명타율, 치명타피해, 방어력 무시는 1.2배 </summary>
    public override void StatLoad()
    {
        dungeonStat[0] = 1;
        for (Obj i = Obj.체력; i <= Obj.속도; i++)
            if(Obj.치명타율 <= i && i <= Obj.방어력무시)
                dungeonStat[(int)i] = Mathf.RoundToInt(1.2f * ms.dungeonStat[(int)i]);
            else
                dungeonStat[(int)i] = ms.dungeonStat[(int)i];
    }
}
