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
        pattern = pattern % 2;
    }


    void StatLoad(ElementalController ec)
    {
        for (int i = 1; i <= 12; i++)
            dungeonStat[i] = basicStat[i] = Mathf.RoundToInt((isUpgraded ? 0.7f : 0.4f) * ec.dungeonStat[i]);
        dungeonStat[1] = basicStat[1] = dungeonStat[2];

        dungeonStat[(int)Obj.SPD] = basicStat[(int)Obj.SPD] = 50;
    }
    void SkillBuff(ElementalController ec)
    {
        //106 정령 힘 부여
        if (ec.HasSkill(106))
            AddBuff(SkillManager.GetSkill(5, 106), 0, 0);
        //107 정령 생명 부여
        if (ec.HasSkill(107))
            AddBuff(SkillManager.GetSkill(5, 107), 0, 0);
        if (ec.HasSkill(114) && type == 1007)
            AddBuff(SkillManager.GetSkill(5, 114), 0, 0);
        if (ec.HasSkill(115) && type == 1008)
            AddBuff(SkillManager.GetSkill(5, 115), 0, 0);
        if (ec.HasSkill(116) && type == 1009)
            AddBuff(SkillManager.GetSkill(5, 116), 0, 0);
    }
}
