using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkillManager : MonoBehaviour
{
    static SkillDB[] skillDB = new SkillDB[13];

    private void Awake() => MakeDB();

    static void MakeDB()
    {
        if (skillDB[1] != null)
            return;

        skillDB[1] = new SkillDB("ArmedFighter", 1);
        skillDB[2] = new SkillDB("MetalKnight", 2);
        skillDB[3] = new SkillDB("Blaster", 3);
        skillDB[4] = new SkillDB("MadScientist", 4);
        skillDB[5] = new SkillDB("ElementalController", 5);
        skillDB[6] = new SkillDB("Druid", 6);
        skillDB[7] = new SkillDB("VisionMaster", 7);
        skillDB[8] = new SkillDB("MagicalRogue", 8);

        skillDB[10] = new SkillDB("Monster", 10);
        skillDB[11] = new SkillDB("Elemental", 11);
        skillDB[12] = new SkillDB("Golem", 12);
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
