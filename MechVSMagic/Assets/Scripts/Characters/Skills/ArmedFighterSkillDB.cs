using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LitJson;

public class ArmedFighterSkillDB : SkillDB
{
    public override void DataLoad()
    {
        className = "ArmedFighter";
        classIdx = 1;
        skillCount = 42;
        skills = new Skill[skillCount];

        for (int i = 0; i < skillCount; i++)
            skills[i] = new Skill();

        TextAsset txtAsset;
        string loadStr;
        JsonData json;

        txtAsset = Resources.Load<TextAsset>("Jsons/Skills/ArmedFighterSkill");
        loadStr = txtAsset.text;
        json = JsonMapper.ToObject(loadStr);

        //Skill Data Load
        for (int i = 0; i < skillCount; i++)
        {
            skills[i].skillName = json[i]["name"].ToString();
            skills[i].idx = int.Parse(json[i]["idx"].ToString());
            skills[i].useclass = 1;
            skills[i].category = int.Parse(json[i]["category"].ToString());
            skills[i].useType = int.Parse(json[i]["usetype"].ToString());
            skills[i].reqLvl = int.Parse(json[i]["reqlvl"].ToString());
            
            for (int j = 0; j < 5; j++)
                skills[i].reqskills[j] = int.Parse(json[i]["reqskill"][j].ToString());

            skills[i].apCost = int.Parse(json[i]["apCost"].ToString());
            skills[i].cooldown = int.Parse(json[i]["cool"].ToString());
            skills[i].targetSelect = int.Parse(json[i]["targetSelect"].ToString());
            skills[i].targetSide = int.Parse(json[i]["targetSide"].ToString());
            skills[i].targetCount = int.Parse(json[i]["targetCount"].ToString());

            skills[i].combo = int.Parse(json[i]["combo"].ToString());

            skills[i].effectCount = int.Parse(json[i]["effectCount"].ToString());
            skills[i].DataAssign();
            for (int j = 0; j < skills[i].effectCount; j++)
            {
                skills[i].effectType[j] = int.Parse(json[i]["effectType"][j].ToString());
                skills[i].effectCond[j] = int.Parse(json[i]["effectCond"][j].ToString());
                skills[i].effectTarget[j] = int.Parse(json[i]["effectTarget"][j].ToString());
                skills[i].effectObject[j] = int.Parse(json[i]["effectObject"][j].ToString());
                skills[i].effectStat[j] = int.Parse(json[i]["effectStat"][j].ToString());
                skills[i].effectRate[j] = float.Parse(json[i]["effectRate"][j].ToString());
                skills[i].effectCalc[j] = int.Parse(json[i]["effectCalc"][j].ToString());
                skills[i].effectTurn[j] = int.Parse(json[i]["effectTurn"][j].ToString());
                skills[i].effectDispel[j] = int.Parse(json[i]["effectDispel"][j].ToString());
                skills[i].effectVisible[j] = int.Parse(json[i]["effectVisible"][j].ToString());
            }
        }
    }
}
