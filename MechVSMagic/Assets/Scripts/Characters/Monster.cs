using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LitJson;
using System.Linq;

public class Monster : Unit
{
    static JsonData json = null;

    public bool isBoss;

    public string monsterName;
    public int monsterIdx;
    public int region;

    public int skillCount;
    public float[] skillChance;

    int currSkillIdx;
    int maxSkillIdx;
    public string pattern;
    Active UseSkill;

    public override void OnBattleStart(BattleManager BM)
    {
        this.BM = BM;
        StatLoad();
        StatUpdate_Turn();
        buffStat[(int)Obj.currHP] = buffStat[(int)Obj.HP];
    }
    public override void OnTurnStart()
    {
        base.OnTurnStart();

        if (!IsStun())
            UseSkill();

        if(HasSkill(31))
        {
            inbattleBuffList.RemoveAll(x => x.name == "빙산의 일각");
            AddBuff(SkillManager.GetSkill(classIdx, 31), 0, 0);
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
            if (inbattleBuffList.Any(x => x.name == "포탄"))
                ActiveSkill(16, new List<Unit>());
            else
            {
                bool reload = BM.ReloadBullet();
                //15 장전
                if (reload)
                    inbattleBuffList.Add(new Buff("포탄", 99, 0, 0, 0, 0, 0, 1));
                //17 공포탄
                else
                    ActiveSkill(17, new List<Unit>());
            }
        }
        else
        {
            ActiveSkill(activeIdxs[pattern[currSkillIdx] - '1'], new List<Unit>());
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

        inskillBuffList.Clear();
        if (skill == null)
        {
            Debug.LogError("skill is null");
            return;
        }

        Passive_SkillCast(skill.category);

        //16 발사 - 포탄 버프 소모
        if (skill.idx == 16)
        {
            Buff b = (from x in inbattleBuffList where x.name == "포탄" select x).First();
            inbattleBuffList.Remove(b);
        }
        //30 붕괴
        else if (skill.idx == 30)
        {
            int cnt = 0;
            List<Unit> u = BM.GetEffectTarget(6);
            foreach (Unit a in u)
            {
                cnt += a.inbattleBuffList.Count(x => x.objectIdx == (int)Obj.Cycle);
                a.inbattleBuffList.RemoveAll(x => x.objectIdx == (int)Obj.Cycle);
            }

            Skill tmp = SkillManager.GetSkill(classIdx, 30);
            inskillBuffList.Add(new Buff("", -1, 5, cnt, tmp.effectCalc[0], tmp.effectRate[0], 0, 0));
        }
        //32 만년설
        else if(skill.idx == 32)
        {
            Skill tmp = SkillManager.GetSkill(classIdx, 32);
            inskillBuffList.Add(new Buff("", -1, 5, shieldAmount, tmp.effectCalc[0], tmp.effectRate[0], 0, 0));
        }
        //85 돈키호테
        else if(skill.idx == 85)
        {
            BM.Quixote();
        }

        Active_Effect(skill, selects);

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
            inskillBuffList.Add(new Buff("", -1, 5, effectTargets[0].inbattleDebuffList.Count(x => x.objectIdx == (int)Obj.Curse), tmp.effectCalc[0], tmp.effectRate[0], 0, 0));
            effectTargets[0].inbattleDebuffList.RemoveAll(x => x.objectIdx == (int)Obj.Curse);
            damaged.Add(effectTargets[0]);
        }

        for (int i = 0; i < skill.effectCount; i++)
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
                else if (skill.effectStat[i] == (int)Obj.GetDmg)
                    rate = dmgs[3];
                //전 턴 가한 피해
                else if (skill.effectStat[i] == (int)Obj.GiveDmg)
                    rate = dmgs[1];
                //타겟 잃은 체력 비율
                else if (skill.effectStat[i] == (int)Obj.LossPer)
                    rate = 1 - ((float)selects[0].buffStat[(int)Obj.currHP] / selects[0].buffStat[(int)Obj.HP]);
                //타겟 현재 체력 비율
                else if (skill.effectStat[i] == (int)Obj.CurrPer)
                    rate = (float)selects[0].buffStat[(int)Obj.currHP] / selects[0].buffStat[(int)Obj.HP];
                else if (skill.effectStat[i] == (int)Obj.BuffCnt)
                    rate = inbattleBuffList.Count;
                else if (skill.effectStat[i] == (int)Obj.DebuffCnt)
                    rate = selects[0].inbattleDebuffList.Count;
                else if (skill.effectStat[i] == (int)Obj.MaxHP)
                    rate = selects[0].buffStat[(int)Obj.HP];
                else if (skill.effectStat[i] == (int)Obj.Bleed)
                    rate = buffStat[(int)Obj.ATK] * 0.15f;
                else if (skill.effectStat[i] == (int)Obj.Burn)
                    rate = buffStat[(int)Obj.ATK] * 0.7f;
                else if (skill.effectStat[i] == (int)Obj.Posion)
                    rate = buffStat[(int)Obj.ATK] * 0.1f;
            }

            switch ((SkillType)skill.effectType[i])
            {
                //데미지 - 스킬 버프 계산 후 
                case SkillType.Damage:
                    {
                        StatUpdate_Skill(skill.category);

                        int dmg = Mathf.CeilToInt(buffStat[skill.effectStat[i]] * skill.effectRate[i]);

                        damaged.Clear();
                        foreach (Unit u in effectTargets)
                        {
                            if (!u.isActiveAndEnabled)
                                continue;

                            //명중 연산 - 최소 명중률 10%
                            int acc = Mathf.Max(buffStat[(int)Obj.ACC] - u.buffStat[(int)Obj.DOG], 10);
                            //명중 시
                            if (Random.Range(0, 100) < acc)
                            {
                                isAcc = true;
                                //크리티컬 연산 - dmg * CRB
                                if (Random.Range(0, 100) < buffStat[(int)Obj.CRC])
                                {
                                    isCrit = true;
                                    dmg = Mathf.CeilToInt(dmg * (buffStat[(int)Obj.CRB] / 100f));
                                }
                                else
                                    isCrit = false;

                                u.GetDamage(this, dmg, buffStat[(int)Obj.PEN]);
                                damaged.Add(u);

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
                        //저주 지속 시간 증가
                        foreach (Unit u in damaged)
                            foreach (Buff b in u.inbattleDebuffList)
                                if (b.objectIdx == (int)Obj.Curse)
                                    b.duration++;
                        break;
                    }
                case SkillType.CharSpecial2:
                    {
                        //저주 있는 대상 저주 한번 더
                        foreach (Unit u in damaged)
                            if (u.inbattleDebuffList.Any(x => x.objectIdx == (int)Obj.Curse))
                                u.AddDebuff(skill, 1, 0);

                        break;
                    }
                default:
                    break;
            }
        }
    }


    public override bool GetDamage(Unit caster, int dmg, int pen)
    {
        if(inbattleBuffList.Any(x=>x.name == "전류 방출"))
        {
            caster.GetDamage(this, buffStat[(int)Obj.ATK], buffStat[(int)Obj.PEN]);
        }

        if(inbattleBuffList.Any(x=>x.name == "플레임"))
        {
            inbattleBuffList.RemoveAll(x => x.name == "플레임");
            LogManager.instance.AddLog("Block");
            return false;
        }

        int finalDEF = Mathf.Max(0, buffStat[(int)Obj.DEF] - pen);
        int finalDmg = Mathf.Min(-1, -dmg + finalDEF);

        if (shieldAmount >= finalDmg)
            shieldAmount -= finalDmg;
        else
        {
            finalDmg -= shieldAmount;
            shieldAmount = 0;
            buffStat[(int)Obj.currHP] += finalDmg;
        }
        dmgs[2] -= finalDmg;
        caster.dmgs[0] -= finalDmg;
        //피격 시 차감되는 버프 처리

        LogManager.instance.AddLog(string.Concat(caster.name, " damages ", name, ", ", finalDmg));


        bool killed = false;
        if (buffStat[(int)Obj.currHP] <= 0)
        {
            killed = true;

            if (HasSkill(38))
            {
                Skill skill = SkillManager.GetSkill(10, 38);

                StatUpdate_Skill(0);

                int ret = Mathf.CeilToInt(buffStat[skill.effectStat[0]] * skill.effectRate[0]);

                List<Unit> effectTargets = BM.GetEffectTarget(skill.effectTarget[0]);
                foreach (Unit u in effectTargets)
                {
                    if (!u.isActiveAndEnabled)
                        continue;

                    //명중 연산 - 최소 명중률 10%
                    int acc = Mathf.Max(buffStat[(int)Obj.ACC] - u.buffStat[(int)Obj.DOG], 10);
                    //명중 시
                    if (Random.Range(0, 100) < acc)
                    {
                        isAcc = true;
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
            }
            else if (HasSkill(39))
            {
                Skill skill = SkillManager.GetSkill(10, 38);

                StatUpdate_Skill(0);

                int ret = Mathf.CeilToInt(buffStat[skill.effectStat[0]] * skill.effectRate[0]);

                List<Unit> effectTargets = BM.GetEffectTarget(skill.effectTarget[0]);
                foreach (Unit u in effectTargets)
                {
                    if (!u.isActiveAndEnabled)
                        continue;

                    //명중 연산 - 최소 명중률 10%
                    int acc = Mathf.Max(buffStat[(int)Obj.ACC] - u.buffStat[(int)Obj.DOG], 10);
                    //명중 시
                    if (Random.Range(0, 100) < acc)
                    {
                        isAcc = true;
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
            }
            else if (HasSkill(100))
            {
                Skill skill = SkillManager.GetSkill(10, 100);

                StatUpdate_Skill(0);

                int ret = Mathf.CeilToInt(buffStat[skill.effectStat[0]] * skill.effectRate[0]);

                List<Unit> effectTargets = BM.GetEffectTarget(skill.effectTarget[0]);
                foreach (Unit u in effectTargets)
                {
                    if (!u.isActiveAndEnabled)
                        continue;

                    //명중 연산 - 최소 명중률 10%
                    int acc = Mathf.Max(buffStat[(int)Obj.ACC] - u.buffStat[(int)Obj.DOG], 10);
                    //명중 시
                    if (Random.Range(0, 100) < acc)
                    {
                        isAcc = true;
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
            }

            gameObject.SetActive(false);
        }

        return killed;
    }
    public override void StatLoad()
    {
        if(json == null)
        {
            TextAsset jsonTxt = Resources.Load<TextAsset>("Jsons/Stats/MonsterStat");
            string loadStr = jsonTxt.text;
            json = JsonMapper.ToObject(loadStr);
        }

        monsterName = json[monsterIdx]["name"].ToString();
        region = (int)json[monsterIdx]["region"];
        LVL = (int)json[monsterIdx]["lvl"];
        dungeonStat[(int)Obj.currHP] = dungeonStat[(int)Obj.HP] = basicStat[(int)Obj.currHP] = basicStat[(int)Obj.HP] = (int)json[monsterIdx]["HP"];
        dungeonStat[(int)Obj.ATK] = basicStat[(int)Obj.ATK] = (int)json[monsterIdx]["ATK"];
        dungeonStat[(int)Obj.DEF] = basicStat[(int)Obj.DEF] = (int)json[monsterIdx]["DEF"];
        dungeonStat[(int)Obj.ACC] = basicStat[(int)Obj.ACC] = (int)json[monsterIdx]["ACC"];
        dungeonStat[(int)Obj.DOG] = basicStat[(int)Obj.DOG] = (int)json[monsterIdx]["DOG"];
        dungeonStat[(int)Obj.CRC] = basicStat[(int)Obj.CRC] = (int)json[monsterIdx]["CRC"];
        dungeonStat[(int)Obj.CRB] = basicStat[(int)Obj.CRB] = (int)json[monsterIdx]["CRB"];
        dungeonStat[(int)Obj.PEN] = basicStat[(int)Obj.PEN] = (int)json[monsterIdx]["PEN"];
        dungeonStat[(int)Obj.SPD] = basicStat[(int)Obj.SPD] = (int)json[monsterIdx]["SPD"];

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
    }
    public override bool IsBoss() => isBoss;

    delegate void Active();
}
