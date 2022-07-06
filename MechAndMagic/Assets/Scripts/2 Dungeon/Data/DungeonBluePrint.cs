using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LitJson;

public class DungeonBluePrint
{
    ///<summary> 던전 이름 </summary>
    public string name;
    ///<summary> 던전 인덱스 </summary>
    public int idx;
    ///<summary> 던전 챕터 </summary>
    public int chapter;
    ///<summary> 던전 진영 </summary>
    public int region;
    ///<summary> 던전 추천 레벨 </summary>
    public int reclvl;
    ///<summary> 던전 설명 텍스트 </summary>
    public string aboutScript;
    ///<summary> 던전 보상 텍스트 </summary>
    public string rewardScript;

    ///<summary> 던전 입장 레벨 </summary>
    public int reqlvl;
    ///<summary> 던전 입장 조건 퀘스트 </summary>
    public int request;

    ///<summary> 층 당 방 최소/최대 갯수 </summary>
    public int[] roomMinMax = new int[2];
    ///<summary> 최소/최대 층 수 </summary>
    public int[] floorMinMax = new int[2];

    ///<summary> 0 empty, 1 monster, 2 pos, 3 neu, 4 neg, 5 quest </summary>
    public float[] roomKindChances = new float[6];
    ///<summary> 방 공개 확률, quest는 관계없이 항상 숨김 </summary>
    public float openChance;

    ///<summary> 몬스터 방 종류 수 </summary>
    public int monRoomCount;
    ///<summary> 몬스터 방 인덱스들 </summary>
    public int[] monRoomIdx;
    ///<summary> 몬스터 방 각 확률 </summary>
    public float[] monRoomChance;
    ///<summary> 보스방 인덱스 </summary>
    public int bossRoomIdx;

    ///<summary> 돌발퀘스트 종류 수 </summary>
    public int questCount;
    ///<summary> 돌발퀘스트 인덱스들 </summary>
    public int[] questIdx;

    public DungeonBluePrint(int id)
    {
        TextAsset txtAsset;
        string loadStr;
        JsonData json;

        txtAsset = Resources.Load<TextAsset>("Jsons/Dungeons/Dungeon");
        loadStr = txtAsset.text;
        json = JsonMapper.ToObject(loadStr);

        name = json[id]["name"].ToString();
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

        roomKindChances[0] = 0;
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

        questCount = (int)json[id]["questCount"];
        questIdx = new int[questCount];
        for (int i = 0; i < questCount; i++)
            questIdx[i] = (int)json[id]["questIdx"][i];
    }
}
