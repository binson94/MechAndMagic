using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkillManager : MonoBehaviour
{
    public static SkillManager instance = null;

    SkillDB[] skillDB = new SkillDB[11];

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            MakeDB();
            DontDestroyOnLoad(gameObject);
        }
        else
            Destroy(gameObject);
    }

    void MakeDB()
    {
        skillDB[1] = new ArmedFighterSkillDB();
        skillDB[1].DataLoad();

        skillDB[10] = new MonsterSkillDB();
        skillDB[10].DataLoad();
    }

    public Skill GetSkillData(int classIdx, int skillIdx)
    {
        return skillDB[classIdx].skills[skillIdx];
    }
}
