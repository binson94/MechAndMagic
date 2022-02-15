using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LitJson;

public class SkillDB
{
    public string className;        //관련 클래스 이름
    public int classIdx;            //관련 클래스 인덱스 ex) 암드파이터 = 1

    public int startIdx;
    protected int skillCount;       //스킬 갯수
    public Skill[] skills = null;

    public SkillDB(string className, int classIdx)
    {
        this.className = className;
        this.classIdx = classIdx;
        JsonLoad();
    }

    protected void JsonLoad()
    {
        TextAsset jsonTxt = Resources.Load<TextAsset>(string.Concat("Jsons/Skills/", className, "Skill"));
        string loadStr = jsonTxt.text;
        JsonData json = JsonMapper.ToObject(loadStr);

        skillCount = json.Count;
        skills = new Skill[skillCount];
        for (int i = 0; i < skillCount; i++)
            skills[i] = new Skill();

        startIdx = (int)json[0]["idx"];
        //Skill Data Load
        for (int i = 0; i < skillCount; i++)
        {
            skills[i].name = json[i]["name"].ToString();
            skills[i].idx = (int)json[i]["idx"];
            skills[i].useclass = classIdx;
            skills[i].category = (int)json[i]["category"];
            skills[i].useType = (int)json[i]["usetype"];
            skills[i].reqLvl = (int)json[i]["reqlvl"];

            for (int j = 0; j < 5; j++)
                skills[i].reqskills[j] = (int)json[i]["reqskill"][j];

            skills[i].apCost = (int)json[i]["apCost"];
            skills[i].cooldown = (int)json[i]["cool"];
            skills[i].targetSelect = (int)json[i]["targetSelect"];
            skills[i].targetSide = (int)json[i]["targetSide"];
            skills[i].targetCount = (int)json[i]["targetCount"];

            skills[i].effectCount = (int)json[i]["effectCount"];
            skills[i].DataAssign();
            for (int j = 0; j < skills[i].effectCount; j++)
            {
                skills[i].effectType[j] = (int)json[i]["effectType"][j];
                skills[i].effectCond[j] = (int)json[i]["effectCond"][j];
                skills[i].effectTarget[j] = (int)json[i]["effectTarget"][j];
                skills[i].effectObject[j] = (int)json[i]["effectObject"][j];
                skills[i].effectStat[j] = (int)json[i]["effectStat"][j];
                skills[i].effectRate[j] = float.Parse(json[i]["effectRate"][j].ToString());
                skills[i].effectCalc[j] = (int)json[i]["effectCalc"][j];
                skills[i].effectTurn[j] = (int)json[i]["effectTurn"][j];
                skills[i].effectDispel[j] = (int)json[i]["effectDispel"][j];
                skills[i].effectVisible[j] = (int)json[i]["effectVisible"][j];
            }
        }
    }
}
