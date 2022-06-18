using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Elemental : Character
{
    public int type;
    public bool isUpgraded;

    int pattern = 0;

    public void Summon(BattleManager bm, ElementalController ec, int type, bool upgrade = false)
    {
        BM = bm;
        isUpgraded = upgrade;
        this.type = type;
        StatLoad(ec);
        SkillBuff(ec);
        StatUpdate_Turn();
        buffStat[(int)Obj.currHP] = buffStat[(int)Obj.HP];

        SkillSet();
    }

    public override void OnTurnStart()
    {
        base.OnTurnStart();
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
        pattern = pattern & 1;
    }


    void StatLoad(ElementalController ec)
    {
        for (int i = 1; i <= 12; i++)
            dungeonStat[i] = Mathf.RoundToInt((isUpgraded ? 0.7f : 0.4f) * ec.dungeonStat[i]);
        dungeonStat[1] = dungeonStat[2];

        dungeonStat[(int)Obj.SPD] = 50;
    }
    void SkillBuff(ElementalController ec)
    {
        //정령의 대리인 2세트 - 정령 힘, 생명 부여 강화
        float rate = 1 + ItemManager.GetSetData(13).Value[0];

        //194 정령 힘 부여
        if (ec.HasSkill(194))
        {
            Skill s= SkillManager.GetSkill(5, 194);
            turnBuffs.Add(new Buff(BuffType.Stat, ec.LVL, new BuffOrder(ec, -1), s.name, s.effectObject[0], s.effectStat[0], s.effectRate[0] * rate, s.effectCalc[0], s.effectTurn[0], s.effectDispel[0], s.effectVisible[0]));
        }
        //195 정령 생명 부여
        if (ec.HasSkill(195))
        {
            Skill s= SkillManager.GetSkill(5, 195);
            turnBuffs.Add(new Buff(BuffType.Stat, ec.LVL, new BuffOrder(ec, -1), s.name, s.effectObject[0], s.effectStat[0], s.effectRate[0] * rate, s.effectCalc[0], s.effectTurn[0], s.effectDispel[0], s.effectVisible[0]));
        }
        if (ec.HasSkill(202) && type == 1007)
            AddBuff(ec, -2, SkillManager.GetSkill(5, 114), 0, 0);
        if (ec.HasSkill(203) && type == 1008)
            AddBuff(ec, -2, SkillManager.GetSkill(5, 115), 0, 0);
        if (ec.HasSkill(204) && type == 1009)
            AddBuff(ec, -2, SkillManager.GetSkill(5, 116), 0, 0);
    }
}
