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

    public DungeonBluePrint(int dungeonIdx)
    {
        JsonData json = JsonMapper.ToObject(Resources.Load<TextAsset>("Jsons/Dungeons/Dungeon").text);
        int jsonIdx = dungeonIdx - (int)json[0]["idx"];

        idx = dungeonIdx;
        name = json[jsonIdx]["name"].ToString();
        chapter = (int)json[jsonIdx]["chapter"];
        region = (int)json[jsonIdx]["region"];
        reclvl = (int)json[jsonIdx]["reclvl"];
        aboutScript = json[jsonIdx]["aboutScript"].ToString();
        rewardScript = json[jsonIdx]["rewardScript"].ToString();


        reqlvl = (int)json[jsonIdx]["reqlvl"];
        request = (int)json[jsonIdx]["request"];


        floorMinMax[0] = (int)json[jsonIdx]["minFloor"];
        floorMinMax[1] = (int)json[jsonIdx]["maxFloor"];
        roomMinMax[0] = (int)json[jsonIdx]["minRoom"];
        roomMinMax[1] = (int)json[jsonIdx]["maxRoom"];

        roomKindChances[0] = 0;
        roomKindChances[1] = float.Parse(json[jsonIdx]["monsterChance"].ToString());
        roomKindChances[2] = float.Parse(json[jsonIdx]["posChance"].ToString());
        roomKindChances[3] = float.Parse(json[jsonIdx]["neuChance"].ToString());
        roomKindChances[4] = float.Parse(json[jsonIdx]["negChance"].ToString());
        roomKindChances[5] = float.Parse(json[jsonIdx]["questChance"].ToString());
        openChance = float.Parse(json[jsonIdx]["openChance"].ToString());

        monRoomCount = (int)json[jsonIdx]["monRoomCount"];
        monRoomChance = new float[monRoomCount];
        monRoomIdx = new int[monRoomCount];
        for (int i = 0; i < monRoomCount; i++)
        {
            monRoomIdx[i] = (int)json[jsonIdx]["monRoomIdx"][i];
            monRoomChance[i] = float.Parse(json[jsonIdx]["monRoomChance"][i].ToString());
        }
        bossRoomIdx = (int)json[jsonIdx]["bossRoomIdx"];

        questCount = (int)json[jsonIdx]["questCount"];
        questIdx = new int[questCount];
        for (int i = 0; i < questCount; i++)
            questIdx[i] = (int)json[jsonIdx]["questIdx"][i];
    }
}
