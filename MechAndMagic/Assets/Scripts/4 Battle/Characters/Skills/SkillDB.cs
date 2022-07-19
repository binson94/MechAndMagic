using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LitJson;

public class SkillDB
{
    ///<summary> 클래스 영문 이름, 데이터 로드 시 사용 </summary>
    string className;
    ///<summary> 관련 클래스 인덱스 ex) 암드파이터 = 1 </summary>
    public int classIdx;

    ///<summary> 해당 클래스 가장 처음 스킬의 인덱스 </summary>
    public int startIdx;
    ///<summary> 클래스 스킬 갯수 </summary>
    protected int skillCount;

    ///<summary> 스킬 데이터 </summary>
    public Skill[] skills = null;

    public SkillDB(string className, int classIdx)
    {
        this.className = className;
        this.classIdx = classIdx;
        LoadData();
    }

    ///<summary> Json 파일 읽어오기 </summary>
    void LoadData()
    {
        JsonData json = JsonMapper.ToObject(Resources.Load<TextAsset>($"Jsons/Skills/{className}Skill").text);

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

            //클래스 스킬에만 적용(몬스터 제외)
            if (classIdx != 10)
            {
                skills[i].script = json[i]["script"].ToString();
                skills[i].posScript = json[i]["script_pos"].ToString();
                skills[i].negScript = json[i]["script_neg"].ToString();
                skills[i].icon = (int)json[i]["icon"];
                skills[i].sfx = (int)json[i]["sfx"];
                skills[i].category = (int)json[i]["category"];
                skills[i].useType = (int)json[i]["usetype"];
                skills[i].reqLvl = (int)json[i]["reqlvl"];

                for (int j = 0; j < 3; j++)
                    skills[i].reqskills[j] = (int)json[i]["reqskill"][j];

                skills[i].apCost = (int)json[i]["apCost"];
                skills[i].cooldown = (int)json[i]["cool"];
                skills[i].targetSelect = (int)json[i]["targetSelect"];
                skills[i].targetSide = (int)json[i]["targetSide"];
                skills[i].targetCount = (int)json[i]["targetCount"];
            }

            skills[i].DataAssign((int)json[i]["effectCount"]);
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
