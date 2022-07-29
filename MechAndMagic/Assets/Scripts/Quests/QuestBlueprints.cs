using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LitJson;

///<summary> 퀘스트 목표
///<para> Kill : 적 처치, Battle : 전투 승리, Event : 이벤트 수행, Outbreak : 돌발퀘 수행 </para>
///<para> Room : 방 클리어, Dungeon : 던전 클리어, Level : 레벨 달성, Diehard : 체력 유지 </para>
///</summary>
public enum QuestType
{
    Kill = 1, Battle, Event, Outbreak, Room, Dungeon, Level, Diehard_Over, Diehard_Under
}

public class QuestBlueprint
{
    //퀘스트 정보
    public int idx;
    public string name;
    public string script;
    public string getScript;
    public string doneScript;

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
    public QuestBlueprint() {}
    public QuestBlueprint(bool isOutbreak, int questIdx)
    {
        JsonData json = isOutbreak ? outbreakJson : questJson;

        //퀘스트 정보
        this.idx = questIdx;
        int jsonIdx = questIdx - (int)json[0]["idx"];

        name = json[jsonIdx]["name"].ToString();
        script = json[jsonIdx]["script"].ToString();

        //퀘스트 목표
        type = (QuestType)(int)json[jsonIdx]["type"];
        objectIdx = (int)json[jsonIdx]["objectIdx"];
        objectAmt = (int)json[jsonIdx]["objectAmt"];

        if(isOutbreak)
        {
            rewardCount = 1;
            rewardIdx = new int[1]; rewardIdx[0] = (int)json[jsonIdx]["rewardIdx"];
            rewardAmt = new int[1]; rewardAmt[0] = (int)json[jsonIdx]["rewardAmt"];

            getScript = json[jsonIdx]["getScript"].ToString();
            doneScript = json[jsonIdx]["doneScript"].ToString();
        }
        else
        {
            rewardCount = (int)json[jsonIdx]["rewardCount"];
            rewardIdx = new int[rewardCount]; rewardAmt = new int[rewardCount];

            for (int i = 0; i < rewardCount; i++)
            {
                rewardIdx[i] = (int)json[jsonIdx]["rewardIdx"][i];
                rewardAmt[i] = (int)json[jsonIdx]["rewardAmt"][i];
            }
        }
    }
}