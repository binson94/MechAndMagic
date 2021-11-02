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

    public int eventCount;
    public int[] eventIdx;

    public DungeonBluePrint(int id)
    {
        TextAsset txtAsset;
        string loadStr;
        JsonData json;

        txtAsset = Resources.Load<TextAsset>("Jsons/Dungeon");
        loadStr = txtAsset.text;
        json = JsonMapper.ToObject(loadStr);

        _name = json[id]["name"].ToString();
        idx = int.Parse(json[id]["idx"].ToString());
        chapter = int.Parse(json[id]["chapter"].ToString());
        region = int.Parse(json[id]["region"].ToString());
        reclvl = int.Parse(json[id]["reclvl"].ToString());
        aboutScript = json[id]["aboutScript"].ToString();
        rewardScript = json[id]["rewardScript"].ToString();


        reqlvl = int.Parse(json[id]["reqlvl"].ToString());
        request = int.Parse(json[id]["request"].ToString());


        floorMinMax[0] = int.Parse(json[id]["minFloor"].ToString());
        floorMinMax[1] = int.Parse(json[id]["maxFloor"].ToString());
        roomMinMax[0] = int.Parse(json[id]["minRoom"].ToString());
        roomMinMax[1] = int.Parse(json[id]["maxRoom"].ToString());

        roomKindChances[0] = float.Parse(json[id]["emptyChance"].ToString());
        roomKindChances[1] = float.Parse(json[id]["monsterChance"].ToString());
        roomKindChances[2] = float.Parse(json[id]["posChance"].ToString());
        roomKindChances[3] = float.Parse(json[id]["neuChance"].ToString());
        roomKindChances[4] = float.Parse(json[id]["negChance"].ToString());
        roomKindChances[5] = float.Parse(json[id]["questChance"].ToString());
        openChance = float.Parse(json[id]["openChance"].ToString());

        monRoomCount = int.Parse(json[id]["monRoomCount"].ToString());
        monRoomChance = new float[monRoomCount];
        monRoomIdx = new int[monRoomCount];
        for (int i = 0; i < monRoomCount; i++)
        {
            monRoomIdx[i] = int.Parse(json[id]["monRoomIdx"][i].ToString());
            monRoomChance[i] = float.Parse(json[id]["monRoomChance"][i].ToString());
        }

        eventCount = int.Parse(json[id]["eventCount"].ToString());
        eventIdx = new int[eventCount];
        for (int i = 0; i < eventCount; i++)
            eventIdx[i] = int.Parse(json[id]["eventIdx"][i].ToString());
    }
}
