using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LitJson;

public class Enemy : Character
{
    public string monsterName;
    public int idx;
    public int region;

    public int skillCount;
    public float[] skillChance;

    public int pattern;

    public override void StatLoad()
    {
        TextAsset txtAsset;
        string loadStr;
        JsonData json;

        txtAsset = Resources.Load<TextAsset>("Jsons/Stats/MonsterStat");
        loadStr = txtAsset.text;
        json = JsonMapper.ToObject(loadStr);

        monsterName = json[idx]["name"].ToString();
        region = int.Parse(json[idx]["region"].ToString());
        LVL = int.Parse(json[idx]["lvl"].ToString());
        basicStat[(int)StatName.currHP].value = basicStat[(int)StatName.HP].value = int.Parse(json[idx]["HP"].ToString());
        basicStat[(int)StatName.ATK].value = int.Parse(json[idx]["ATK"].ToString());
        basicStat[(int)StatName.DEF].value = int.Parse(json[idx]["DEF"].ToString());
        basicStat[(int)StatName.ACC].value = int.Parse(json[idx]["ACC"].ToString());
        basicStat[(int)StatName.DOG].value = int.Parse(json[idx]["DOG"].ToString());
        basicStat[(int)StatName.CRC].value = int.Parse(json[idx]["CRC"].ToString());
        basicStat[(int)StatName.CRB].value = int.Parse(json[idx]["CRB"].ToString());
        basicStat[(int)StatName.PEN].value = int.Parse(json[idx]["PEN"].ToString());
        basicStat[(int)StatName.SPD].value = int.Parse(json[idx]["SPD"].ToString());

        pattern = int.Parse(json[idx]["pattern"].ToString());

        skillCount = 8;
        activeSkills = new int[skillCount];
        skillChance = new float[skillCount];
        for (int i = 0; i < 8; i++)
        {
            activeSkills[i] = int.Parse(json[idx]["skillIdx"][i].ToString());
            skillChance[i] = float.Parse(json[idx]["skillChance"][i].ToString());
        }
    }
}
