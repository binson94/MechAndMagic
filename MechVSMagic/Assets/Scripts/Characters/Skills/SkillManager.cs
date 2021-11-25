using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkillManager : MonoBehaviour
{
    static SkillDB[] skillDB = new SkillDB[11];

    private void Awake()
    {
        MakeDB();
    }

    static void MakeDB()
    {
        skillDB[1] = new ArmedFighterSkillDB();
        skillDB[1].DataLoad();

        skillDB[10] = new MonsterSkillDB();
        skillDB[10].DataLoad();
    }

    static public Skill[] GetSkillData()
    {
        return skillDB[GameManager.GetCurrClass()].skills;
    }
    static public Skill GetSkillData(int classIdx, int skillIdx)
    {
        return skillDB[classIdx].skills[skillIdx];
    }
}
