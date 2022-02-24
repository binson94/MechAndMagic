using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LitJson;

public enum QuestType
{
    Kill = 1, Battle, Event, Outbreak, Room, Dungeon, Level, Diehard
}

public class QuestBlueprint
{
    //퀘스트 정보
    public int idx;
    public string script;

    //퀘스트 목표
    public QuestType type;
    public int objectIdx;
    public int objectAmt;

    //퀘스트 보상
    public int rewardCount;
    public int[] rewardIdx;
    public int[] rewardAmt;

    static JsonData questJson;
    static JsonData outbreakJson;

    static QuestBlueprint()
    {
        TextAsset jsonTxt = Resources.Load<TextAsset>("Jsons/Quests/Quest");
        questJson = JsonMapper.ToObject(jsonTxt.text);
        jsonTxt = Resources.Load<TextAsset>("Jsons/Quests/Outbreak");
        outbreakJson = JsonMapper.ToObject(jsonTxt.text);
    }
    public QuestBlueprint(bool isOutbreak, int idx)
    {
        JsonData json = isOutbreak ? outbreakJson : questJson;

        //퀘스트 정보
        this.idx = idx;
        script = json[idx]["script"].ToString();

        //퀘스트 목표
        type = (QuestType)(int)json[idx]["type"];
        objectIdx = (int)json[idx]["objectIdx"];
        objectAmt = (int)json[idx]["objectAmt"];

        //퀘스트 보상
        if(isOutbreak)
        {
            rewardCount = 1;
            rewardIdx = new int[1]; rewardAmt = new int[1];
            rewardIdx[0] = (int)json[idx]["rewardIdx"];
            rewardAmt[0] = (int)json[idx]["rewardAmt"];
        }
        else
        {
            rewardCount = (int)json[idx]["rewardCount"];
            rewardIdx = new int[rewardCount]; rewardAmt = new int[rewardCount];
            for (int i = 0; i < rewardCount; i++)
            {
                rewardIdx[i] = (int)json[idx]["rewardIdx"][i];
                rewardAmt[i] = (int)json[idx]["rewardAmt"][i];
            }
        }
    }
}