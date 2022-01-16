using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkillManager : MonoBehaviour
{
    static SkillDB[] skillDB = new SkillDB[12];

    private void Awake()
    {
        MakeDB();
    }

    static void MakeDB()
    {
        skillDB[1] = new ArmedFighterSkillDB();
        skillDB[1].DataLoad();

        skillDB[2] = new MetalKnightSkillDB();
        skillDB[2].DataLoad();

        skillDB[5] = new ElementalControllerSkillDB();
        skillDB[5].DataLoad();

        skillDB[6] = new DruidSkillDB();
        skillDB[6].DataLoad();

        skillDB[10] = new MonsterSkillDB();
        skillDB[10].DataLoad();

        skillDB[11] = new ElementalSkillDB();
        skillDB[11].DataLoad();
    }

    static public Skill[] GetSkillData(int classIdx)
    {
        return skillDB[classIdx].skills;
    }
    static public Skill GetSkill(int classIdx, int idx)
    {
        if (idx == 0)
            return new Skill();

        return skillDB[classIdx].skills[idx - skillDB[classIdx].startIdx];
    }
}
