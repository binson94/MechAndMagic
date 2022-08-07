using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LitJson;
using System.Linq;

public class Monster : Unit
{
    static JsonData json = null;

    ///<summary> 보스 여부 </summary>
    public bool isBoss;
    ///<summary> 몬스터 인덱스 </summary>
    public int monsterIdx;

    ///<summary> 스킬 갯수 </summary>
    public int skillCount;
    ///<summary> 각 스킬 별 확률 </summary>
    public float[] skillChance;

    ///<summary> 패턴 유닛인 경우, 현재 패턴 번호 </summary>
    int currSkillIdx;
    ///<summary> 패턴 길이 </summary>
    int pattenLength;
    ///<summary> 패턴 문자열 </summary>
    public string pattern;
    Active UseSkill;

    public override void OnTurnStart()
    {
        base.OnTurnStart();

        if (!IsStun())
            UseSkill();

        //31 빙산의 일각
        if(HasSkill(31))
            AddBuff(this, orderIdx, SkillManager.GetSkill(classIdx, 31), 0, 0);
    }

    void UseSkillByProb()
    {
        float rand = Random.Range(0, 1f);
        float prob = skillChance[0];

        int slotIdx;
        for (slotIdx = 0; rand > prob && slotIdx < skillCount - 1; prob += skillChance[++slotIdx]);

        ActiveSkill(activeIdxs[slotIdx], new List<Unit>());
    }
    void UseSkillByPattern()
    {
        //포병
        if (monsterIdx == 10 || monsterIdx == 11)
        {
            //16 발사
            if (turnBuffs.buffs.Any(x => x.name == "포탄"))
                ActiveSkill(monsterIdx == 10 ? 16 : 18, new List<Unit>());
            else
            {
                bool reload = BM.ReloadBullet();
                //15 장전
                if (reload)
                    turnBuffs.Add(new Buff(BuffType.None, new BuffOrder(this), "포탄", (int)Obj.Cannon, 0, 0, 0, 99, 0, 1));
                //17 공포탄
                else
                    ActiveSkill(17, new List<Unit>());
            }
        }
        else
        {
            ActiveSkill(activeIdxs[pattern[currSkillIdx] - '1'], new List<Unit>());
            currSkillIdx = (currSkillIdx + 1) % pattenLength;
        }
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

        //16 발사 - 포탄 버프 소모
        if (skill.idx == 16 || skill.idx == 18)
            turnBuffs.buffs.RemoveAll(x => x.name == "포탄");
        //30 붕괴
        else if (skill.idx == 30)
        {
            int cnt = 0;
            List<Unit> u = BM.GetEffectTarget(6);
            foreach (Unit a in u)
            {
                cnt += a.turnBuffs.buffs.Count(x => x.objectIdx[0] == (int)Obj.순환);
                a.turnBuffs.buffs.RemoveAll(x => x.objectIdx[0] == (int)Obj.순환);
            }

            Skill tmp = SkillManager.GetSkill(classIdx, 30);
            skillBuffs.Add(new Buff(BuffType.Stat, BuffOrder.Default, "", (int)Obj.공격력, cnt, tmp.effectRate[0], tmp.effectCalc[0], -1));
        }
        //32 만년설
        else if(skill.idx == 32)
        {
            Skill tmp = SkillManager.GetSkill(classIdx, 32);
            skillBuffs.Add(new Buff(BuffType.Stat, BuffOrder.Default, tmp.name, tmp.effectObject[0], shieldAmount, tmp.effectRate[0], tmp.effectCalc[0], tmp.effectTurn[0]));
        }
        //44 플레임
        else if(skill.idx == 44)
            turnBuffs.Add(new Buff(BuffType.None, BuffOrder.Default, skill.name, (int)Obj.Immune, 0, 0, 0, 1, 0, 1));
        //85 돈키호테
        else if(skill.idx == 85)
            BM.Quixote();
        //90 전류 방출
        else if (skill.idx == 90)
            turnBuffs.Add(new Buff(BuffType.None, BuffOrder.Default, skill.name, (int)Obj.Niddle, buffStat[skill.effectStat[1]], skill.effectRate[1], 0, 2, 1, 1));
        

        Active_Effect(skill, selects);
        if(skill.sfx > 0)
            SoundManager.Instance.PlaySFX(skill.sfx);

        //57 사격 진행
        if(skill.idx == 57)
            BM.ShotCommand();

        orderIdx++;
        buffStat[(int)Obj.currAP] -= GetSkillCost(skill);
    }
    protected override void Active_Effect(Skill skill, List<Unit> selects)
    {
        List<Unit> effectTargets;
        List<Unit> damaged = new List<Unit>();

        if(skill.idx == 78)
        {
            effectTargets = BM.GetEffectTarget(2);

            Skill tmp = SkillManager.GetSkill(classIdx, 78);
            skillBuffs.Add(new Buff(BuffType.Stat, BuffOrder.Default, "", tmp.effectObject[0], effectTargets[0].turnDebuffs.buffs.Count(x => x.objectIdx[0] == (int)Obj.저주), tmp.effectRate[0], tmp.effectCalc[0], -1));
            effectTargets[0].turnDebuffs.buffs.RemoveAll(x => x.objectIdx[0] == (int)Obj.저주);
            damaged.Add(effectTargets[0]);
        }

        for (int i = 0; i < skill.effectCount; i++)
        {
            effectTargets = GetEffectTarget(selects, damaged, skill.effectTarget[i]);

            switch ((EffectType)skill.effectType[i])
            {
                //데미지 - 스킬 버프 계산 후 
                case EffectType.Damage:
                    {
                        //78 파멸의 공백
                        if(skill.idx == 78)
                            skillBuffs.Add(new Buff(BuffType.Stat, BuffOrder.Default, string.Empty, (int)Obj.공격력, effectTargets[0].turnBuffs.buffs.Count(x => x.objectIdx.Any(y => y == (int)Obj.저주)), skill.effectRate[0], skill.effectCalc[0], -1));

                        StatUpdate_Skill(skill);

                        float dmg = GetEffectStat(effectTargets, skill.effectStat[i]) * skill.effectRate[i];

                        damaged.Clear();
                        foreach (Unit u in effectTargets)
                        {
                            if (!u.isActiveAndEnabled)
                                continue;

                            //명중 연산 - 최소 명중 10%
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
                case EffectType.CharSpecial1:
                    {
                        //저주 지속 시간 증가
                        foreach (Unit u in effectTargets)
                            foreach (Buff b in u.turnDebuffs.buffs)
                                if (b.objectIdx[0] == (int)Obj.저주)
                                    b.duration++;
                        break;
                    }
                case EffectType.CharSpecial2:
                    {
                        //저주 있는 대상 저주 한번 더
                        foreach (Unit u in effectTargets)
                            if (u.turnDebuffs.buffs.Any(x => x.objectIdx[0] == (int)Obj.저주))
                                u.AddDebuff(this, orderIdx, skill, 1, 0);

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
        //90 전류 방출
        var elec = from token in turnBuffs.buffs where token.objectIdx.Any(x => x== (int)Obj.Niddle) select token;
        if(elec.Count() > 0)
            foreach(Buff buff in elec)
                caster.GetDamage(this, buff.buffRate[0], buffStat[(int)Obj.방어력무시], 100);
                
        //44 플레임
        if(turnBuffs.buffs.Any(x=>x.name == SkillManager.GetSkill(classIdx, 44).name))
        {
            turnBuffs.buffs.RemoveAll(x => x.name == SkillManager.GetSkill(classIdx, 44).name);
            LogManager.instance.AddLog("플레임 효과로 피해를 무시합니다.");
            return new KeyValuePair<bool, int>(false, 0);
        }

        float finalDEF = Mathf.Max(0, buffStat[(int)Obj.방어력] * (100 - pen) / 100f);
        int finalDmg = Mathf.RoundToInt(dmg / (1 + 0.1f * finalDEF) * crb / 100);

        if (shieldAmount - finalDmg >= 0)
            shieldAmount -= finalDmg;
        else
        {
            buffStat[(int)Obj.currHP] -= finalDmg - shieldAmount;
            shieldAmount = 0;
        }
        dmgs[2] += finalDmg;
        caster.dmgs[0] += finalDmg;
        //피격 시 차감되는 버프 처리

        if(crb <= 100)
            LogManager.instance.AddLog($"{name}(이)가 피해 {finalDmg}를 입었습니다.");
        else
            LogManager.instance.AddLog($"{name}(이)가 치명타 피해 {finalDmg}를 입었습니다.");


        bool killed = false;
        if (buffStat[(int)Obj.currHP] <= 0)
        {
            killed = true;

            //61 분열, 62 파상풍 분열, 87 코드네임 : 레드, 100 
            bool[] dito = new bool[] { HasSkill(61), HasSkill(62), HasSkill(87), HasSkill(100) };
            if (dito.Any(x => x))
            {
                int skillIdx = dito[0] ? 61 : dito[1] ? 62 : dito[2] ? 87 : 100;
                Skill skill = SkillManager.GetSkill(10, skillIdx);
                LogManager.instance.AddLog($"{name}(이)가 {skill.name}(을)를 시전했습니다.");
                SoundManager.Instance.PlaySFX(skill.sfx);

                StatUpdate_Skill(skill);

                float ret = buffStat[skill.effectStat[0]] * skill.effectRate[0];

                List<Unit> effectTargets = BM.GetEffectTarget(skill.effectTarget[0]);
                foreach (Unit u in effectTargets)
                {
                    if (!u.isActiveAndEnabled)
                        continue;

                    int acc = 20;
                    if (buffStat[(int)Obj.명중] >= u.buffStat[(int)Obj.회피])
                        acc = 60 + 6 * (buffStat[(int)Obj.명중] - u.buffStat[(int)Obj.회피]) / (u.LVL + 2);
                    else
                        acc = Mathf.Max(acc, 60 + 6 * (buffStat[(int)Obj.명중] - u.buffStat[(int)Obj.회피]) / (LVL + 2));
                    acc = Mathf.Max(20, acc);
                    
                    //명중 시
                    if (Random.Range(0, 100) < acc)
                    {
                        isAcc = true;
                        isCrit = Random.Range(0, 100) < buffStat[(int)Obj.치명타율];

                        u.GetDamage(this, dmg, buffStat[(int)Obj.방어력무시], isCrit ? buffStat[(int)Obj.치명타피해] : 100);
                        if(dito[1])
                            u.AddDebuff(this, -1, skill, 1, 0);

                        Passive_SkillHit(skill);
                    }
                    else
                    {
                        isAcc = false;
                        LogManager.instance.AddLog($"{u.name}(이)가 스킬을 회피하였습니다.");
                    }
                }
            }

            //임플란트 봄 존재 시 폭발
            if(implantBomb != null)
            {
                implantBomb.Bomb(BM, this);
                implantBomb = null;
            }
        }

        return new KeyValuePair<bool, int>(killed, finalDmg);
    }
    public override void StatLoad()
    {
        if (json == null)
        {
            TextAsset jsonTxt = Resources.Load<TextAsset>("Jsons/Stats/MonsterStat");
            string loadStr = jsonTxt.text;
            json = JsonMapper.ToObject(loadStr);
        }

        int jsonIdx = monsterIdx - (int)json[0]["idx"];
        isBoss = monsterIdx >= 90;

        name = json[jsonIdx]["name"].ToString();
        LVL = (int)json[jsonIdx]["lvl"];
        dungeonStat[0] = 1;
        dungeonStat[(int)Obj.currHP] = dungeonStat[(int)Obj.체력] = (int)json[jsonIdx]["HP"];
        dungeonStat[(int)Obj.공격력] = (int)json[jsonIdx]["ATK"];
        dungeonStat[(int)Obj.방어력] = (int)json[jsonIdx]["DEF"];
        dungeonStat[(int)Obj.명중] = (int)json[jsonIdx]["ACC"];
        dungeonStat[(int)Obj.회피] = (int)json[jsonIdx]["DOG"];
        dungeonStat[(int)Obj.치명타율] = (int)json[jsonIdx]["CRC"];
        dungeonStat[(int)Obj.치명타피해] = (int)json[jsonIdx]["CRB"];
        dungeonStat[(int)Obj.방어력무시] = (int)json[jsonIdx]["PEN"];
        dungeonStat[(int)Obj.속도] = (int)json[jsonIdx]["SPD"];

        pattern = json[jsonIdx]["pattern"].ToString();
        if (pattern[0] == '0')
            UseSkill = UseSkillByProb;
        else
        {
            UseSkill = UseSkillByPattern;
            currSkillIdx = 0;
            pattenLength = pattern.Length;
        }

        skillCount = 0;
        activeIdxs = new int[4];
        skillChance = new float[4];
        for (int i = 0; i < 4; i++)
        {
            int tmp;
            if((tmp = (int)json[jsonIdx]["skillIdx"][i]) > 0)
            {
                activeIdxs[i] = tmp;
                skillChance[i] = float.Parse(json[jsonIdx]["skillChance"][i].ToString());
                skillCount++;
            }
        }

        for(int i = 0;i<dungeonStat.Length;i++)
            buffStat[i] = dungeonStat[i];
    }
    public override bool IsBoss() => isBoss;

    delegate void Active();
}
