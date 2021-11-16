using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LitJson;

public class DungeonBluePrint
{
    public string _name;
    public int idx;
    public int chapter;
    public int region;
    public int reclvl;
    public string aboutScript;
    public string rewardScript;
    public int reqlvl;
    public int request;
    public int[] roomMinMax = new int[2];
    public int[] floorMinMax = new int[2];

    //0empty, 1monster, 2pos, 3neu, 4neg, 5quest
    public float[] roomKindChances = new float[6];
    public float openChance;

    public int monRoomCount;
    public int[] monRoomIdx;
    public float[] monRoomChance;
    public int bossRoomIdx;

    public int eventCount;
    public int[] eventIdx;

    public int questCount;
    public int[] questIdx;

    public DungeonBluePrint(int id)
    {
        TextAsset txtAsset;
        string loadStr;
        JsonData json;

        txtAsset = Resources.Load<TextAsset>("Jsons/Dungeons/Dungeon");
        loadStr = txtAsset.text;
        json = JsonMapper.ToObject(loadStr);

        _name = json[id]["name"].ToString();
        idx = id;
        chapter = (int)json[id]["chapter"];
        region = (int)json[id]["region"];
        reclvl = (int)json[id]["reclvl"];
        aboutScript = json[id]["aboutScript"].ToString();
        rewardScript = json[id]["rewardScript"].ToString();


        reqlvl = (int)json[id]["reqlvl"];
        request = (int)json[id]["request"];


        floorMinMax[0] = (int)json[id]["minFloor"];
        floorMinMax[1] = (int)json[id]["maxFloor"];
        roomMinMax[0] = (int)json[id]["minRoom"];
        roomMinMax[1] = (int)json[id]["maxRoom"];

        roomKindChances[0] = float.Parse(json[id]["emptyChance"].ToString());
        roomKindChances[1] = float.Parse(json[id]["monsterChance"].ToString());
        roomKindChances[2] = float.Parse(json[id]["posChance"].ToString());
        roomKindChances[3] = float.Parse(json[id]["neuChance"].ToString());
        roomKindChances[4] = float.Parse(json[id]["negChance"].ToString());
        roomKindChances[5] = float.Parse(json[id]["questChance"].ToString());
        openChance = float.Parse(json[id]["openChance"].ToString());

        monRoomCount = (int)json[id]["monRoomCount"];
        monRoomChance = new float[monRoomCount];
        monRoomIdx = new int[monRoomCount];
        for (int i = 0; i < monRoomCount; i++)
        {
            monRoomIdx[i] = (int)json[id]["monRoomIdx"][i];
            monRoomChance[i] = float.Parse(json[id]["monRoomChance"][i].ToString());
        }
        bossRoomIdx = (int)json[id]["bossRoomIdx"];

        eventCount = (int)json[id]["eventCount"];
        eventIdx = new int[eventCount];
        for (int i = 0; i < eventCount; i++)
            eventIdx[i] = (int)json[id]["eventIdx"][i];

        questCount = (int)json[id]["questCount"];
        questIdx = new int[questCount];
        for (int i = 0; i < questCount; i++)
            questIdx[i] = (int)json[id]["questIdx"][i];
    }
}
