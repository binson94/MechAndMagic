using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LitJson;
using System.Linq;

public class Monster : Unit
{
    static JsonData json = null;

    public bool isBoss;
    public int monsterIdx;
    public int region;

    public int skillCount;
    public float[] skillChance;

    int currSkillIdx;
    int maxSkillIdx;
    public string pattern;
    Active UseSkill;

    public override void OnTurnStart()
    {
        base.OnTurnStart();

        if (!IsStun())
            UseSkill();

        //31 빙산의 일각
        if(HasSkill(31))
        {
            turnBuffs.buffs.RemoveAll(x => x.name == SkillManager.GetSkill(classIdx, 31).name);
            AddBuff(this, orderIdx, SkillManager.GetSkill(classIdx, 31), 0, 0);
        }
    }

    void UseSkillByProb()
    {
        float rand = Random.Range(0, 1);
        float curr = skillChance[0];
        int idx;
        for (idx = 0; rand > curr && idx < 7; idx++)
            curr += skillChance[idx + 1];

        ActiveSkill(idx, new List<Unit>());
    }
    void UseSkillByPattern()
    {
        //포병
        if (monsterIdx == 12 || monsterIdx == 13)
        {
            //16 발사
            if (turnBuffs.buffs.Any(x => x.name == "포탄"))
                ActiveSkill(16, new List<Unit>());
            else
            {
                bool reload = BM.ReloadBullet();
                //15 장전
                if (reload)
                    turnBuffs.Add(new Buff(BuffType.None, LVL, new BuffOrder(this, -3), "포탄", 0, 0, 0, 0, 99, 0, 1));
                //17 공포탄
                else
                    ActiveSkill(17, new List<Unit>());
            }
        }
        else
        {
            ActiveSkill(pattern[currSkillIdx] - '1', new List<Unit>());
            currSkillIdx = (currSkillIdx + 1) % maxSkillIdx;
        }
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

        //16 발사 - 포탄 버프 소모
        if (skill.idx == 16)
        {
            Buff b = (from x in turnBuffs.buffs where x.name == "포탄" select x).First();
            turnBuffs.buffs.Remove(b);
        }
        //30 붕괴
        else if (skill.idx == 30)
        {
            int cnt = 0;
            List<Unit> u = BM.GetEffectTarget(6);
            foreach (Unit a in u)
            {
                cnt += a.turnBuffs.buffs.Count(x => x.objectIdx[0] == (int)Obj.Cycle);
                a.turnBuffs.buffs.RemoveAll(x => x.objectIdx[0] == (int)Obj.Cycle);
            }

            Skill tmp = SkillManager.GetSkill(classIdx, 30);
            skillBuffs.Add(new Buff(BuffType.Stat, LVL, new BuffOrder(), "", (int)Obj.공격력, cnt, tmp.effectRate[0], tmp.effectCalc[0], -1));
        }
        //32 만년설
        else if(skill.idx == 32)
        {
            Skill tmp = SkillManager.GetSkill(classIdx, 32);
            skillBuffs.Add(new Buff(BuffType.Stat, LVL, new BuffOrder(), tmp.name, tmp.effectObject[0], shieldAmount, tmp.effectRate[0], tmp.effectCalc[0], tmp.effectTurn[0]));
        }
        //85 돈키호테
        else if(skill.idx == 85)
        {
            BM.Quixote();
        }

        Active_Effect(skill, selects);

        orderIdx++;
        buffStat[(int)Obj.currAP] -= GetSkillCost(skill);
        cooldowns[idx] = skill.cooldown;
    }
    protected override void Active_Effect(Skill skill, List<Unit> selects)
    {
        List<Unit> effectTargets;
        List<Unit> damaged = new List<Unit>();
        float rate = 0;

        if(skill.idx == 78)
        {
            effectTargets = BM.GetEffectTarget(2);

            Skill tmp = SkillManager.GetSkill(classIdx, 78);
            skillBuffs.Add(new Buff(BuffType.Stat, LVL, new BuffOrder(), "", tmp.effectObject[0], effectTargets[0].turnDebuffs.buffs.Count(x => x.objectIdx[0] == (int)Obj.Curse), tmp.effectRate[0], tmp.effectCalc[0], -1));
            effectTargets[0].turnDebuffs.buffs.RemoveAll(x => x.objectIdx[0] == (int)Obj.Curse);
            damaged.Add(effectTargets[0]);
        }

        for (int i = 0; i < skill.effectCount; i++)
        {
            effectTargets = GetEffectTarget(selects, damaged, skill.effectTarget[i]);
            rate = GetEffectStat(selects, skill.effectStat[i]);

            switch ((SkillType)skill.effectType[i])
            {
                //데미지 - 스킬 버프 계산 후 
                case SkillType.Damage:
                    {
                        StatUpdate_Skill(skill);

                        float dmg = buffStat[skill.effectStat[i]] * skill.effectRate[i];

                        damaged.Clear();
                        foreach (Unit u in effectTargets)
                        {
                            if (!u.isActiveAndEnabled)
                                continue;

                            //명중 연산 - 최소 명중률 10%
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

                                u.GetDamage(this, dmg, buffStat[(int)Obj.PEN], isCrit ? buffStat[(int)Obj.CRC] : 100);
                                damaged.Add(u);

                                Passive_SkillHit(skill);
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
                            foreach(Unit u in effectTargets)
                                u.AddBuff(this, orderIdx, skill, i, rate);
                        break;
                    }
                case SkillType.Active_Debuff:
                    {
                        if (skill.effectCond[i] == 0 || skill.effectCond[i] == 1 && isAcc || skill.effectCond[i] == 2 && isCrit)
                            foreach (Unit u in effectTargets)
                                u.AddDebuff(this, orderIdx, skill, i, rate);
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
                        //저주 지속 시간 증가
                        foreach (Unit u in damaged)
                            foreach (Buff b in u.turnDebuffs.buffs)
                                if (b.objectIdx[0] == (int)Obj.Curse)
                                    b.duration++;
                        break;
                    }
                case SkillType.CharSpecial2:
                    {
                        //저주 있는 대상 저주 한번 더
                        foreach (Unit u in damaged)
                            if (u.turnDebuffs.buffs.Any(x => x.objectIdx[0] == (int)Obj.Curse))
                                u.AddDebuff(this, orderIdx, skill, 1, 0);

                        break;
                    }
                default:
                    break;
            }
        }
    }

    public override KeyValuePair<bool, int> GetDamage(Unit caster, float dmg, int pen, int crb)
    {
        //90 전류 방출
        if (turnBuffs.buffs.Any(x => x.name == SkillManager.GetSkill(classIdx, 90).name)) caster.GetDamage(this, buffStat[(int)Obj.공격력], buffStat[(int)Obj.PEN], 100);
        //44 플레임
        if(turnBuffs.buffs.Any(x=>x.name == SkillManager.GetSkill(classIdx, 44).name))
        {
            turnBuffs.buffs.RemoveAll(x => x.name == SkillManager.GetSkill(classIdx, 44).name);
            LogManager.instance.AddLog("Block");
            return new KeyValuePair<bool, int>(false, 0);
        }

        float finalDEF = Mathf.Max(0, buffStat[(int)Obj.DEF] * (100 - pen) / 100f);
        int finalDmg = Mathf.RoundToInt(-dmg / (1 + 0.1f * finalDEF) * crb / 100);

        if (shieldAmount + finalDmg >= 0)
            shieldAmount += finalDmg;
        else
        {
            finalDmg += shieldAmount;
            shieldAmount = 0;
            buffStat[(int)Obj.currHP] += finalDmg;
        }
        dmgs[2] -= finalDmg;
        caster.dmgs[0] -= finalDmg;
        //피격 시 차감되는 버프 처리

        LogManager.instance.AddLog(string.Concat(caster.name, "의 공격, ", name, "에게 ", finalDmg, "만큼 피해"));


        bool killed = false;
        if (buffStat[(int)Obj.currHP] <= 0)
        {
            killed = true;

            if (HasSkill(38))
            {
                Skill skill = SkillManager.GetSkill(10, 38);

                StatUpdate_Skill(skill);

                float ret = buffStat[skill.effectStat[0]] * skill.effectRate[0];

                List<Unit> effectTargets = BM.GetEffectTarget(skill.effectTarget[0]);
                foreach (Unit u in effectTargets)
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

                        u.GetDamage(this, dmg, buffStat[(int)Obj.PEN], isCrit ? buffStat[(int)Obj.CRB] : 100);

                        Passive_SkillHit(skill);
                    }
                    else
                    {
                        isAcc = false;
                        LogManager.instance.AddLog("Dodge");
                    }
                }
            }
            else if (HasSkill(39))
            {
                Skill skill = SkillManager.GetSkill(10, 39);

                StatUpdate_Skill(skill);

                float ret = buffStat[skill.effectStat[0]] * skill.effectRate[0];

                List<Unit> effectTargets = BM.GetEffectTarget(skill.effectTarget[0]);
                foreach (Unit u in effectTargets)
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

                        u.GetDamage(this, dmg, buffStat[(int)Obj.PEN], isCrit ? buffStat[(int)Obj.CRB] : 100);

                        Passive_SkillHit(skill);
                    }
                    else
                    {
                        isAcc = false;
                        LogManager.instance.AddLog("Dodge");
                    }
                }
            }
            else if (HasSkill(100))
            {
                Skill skill = SkillManager.GetSkill(10, 100);

                StatUpdate_Skill(skill);

                float ret = buffStat[skill.effectStat[0]] * skill.effectRate[0];

                List<Unit> effectTargets = BM.GetEffectTarget(skill.effectTarget[0]);
                foreach (Unit u in effectTargets)
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

                        u.GetDamage(this, dmg, buffStat[(int)Obj.PEN], isCrit ? buffStat[(int)Obj.CRB] : 100);

                        Passive_SkillHit(skill);
                    }
                    else
                    {
                        isAcc = false;
                        LogManager.instance.AddLog("Dodge");
                    }
                }
            }

            QuestManager.QuestUpdate(QuestType.Kill, monsterIdx, 1);
            gameObject.SetActive(false);
        }

        return new KeyValuePair<bool, int>(killed, -finalDmg);
    }
    public override void StatLoad()
    {
        if (json == null)
        {
            TextAsset jsonTxt = Resources.Load<TextAsset>("Jsons/Stats/MonsterStat");
            string loadStr = jsonTxt.text;
            json = JsonMapper.ToObject(loadStr);
        }

        name = json[monsterIdx]["name"].ToString();
        region = (int)json[monsterIdx]["region"];
        LVL = (int)json[monsterIdx]["lvl"];
        dungeonStat[(int)Obj.currHP] = dungeonStat[(int)Obj.HP] = (int)json[monsterIdx]["HP"];
        dungeonStat[(int)Obj.공격력] = (int)json[monsterIdx]["ATK"];
        dungeonStat[(int)Obj.DEF] = (int)json[monsterIdx]["DEF"];
        dungeonStat[(int)Obj.ACC] = (int)json[monsterIdx]["ACC"];
        dungeonStat[(int)Obj.DOG] = (int)json[monsterIdx]["DOG"];
        dungeonStat[(int)Obj.CRC] = (int)json[monsterIdx]["CRC"];
        dungeonStat[(int)Obj.CRB] = (int)json[monsterIdx]["CRB"];
        dungeonStat[(int)Obj.PEN] = (int)json[monsterIdx]["PEN"];
        dungeonStat[(int)Obj.SPD] = (int)json[monsterIdx]["SPD"];

        pattern = json[monsterIdx]["pattern"].ToString();
        if (pattern == "0")
            UseSkill = UseSkillByProb;
        else
        {
            UseSkill = UseSkillByPattern;
            currSkillIdx = 0;
            maxSkillIdx = pattern.Length;
        }

        skillCount = 8;
        activeIdxs = new int[skillCount];
        skillChance = new float[skillCount];
        for (int i = 0; i < 8; i++)
        {
            activeIdxs[i] = (int)json[monsterIdx]["skillIdx"][i];
            skillChance[i] = float.Parse(json[monsterIdx]["skillChance"][i].ToString());
        }

        for(int i = 0;i<dungeonStat.Length;i++)
            buffStat[i] = dungeonStat[i];
    }
    public override bool IsBoss() => isBoss;

    delegate void Active();
}
