using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LitJson;

public enum QuestType
{
    Kill = 1
}

public class QuestData
{
    public int idx;
    public int region;
    public int chapter;
    public string script;

    public int lvl;
    public int requestIdx;

    public QuestType type;
    public int objectCount;
    public int[] objectIdx;
    public int[] objectAmt;

    public int rewardCount;
    public int[] rewardIdx;
    public int[] rewardAmt;

    public QuestData(bool isOutbreak, int idx)
    {
        if (isOutbreak)
            LoadOutbreak(idx);
        else
            LoadQuest(idx);
    }

    public List<int> GetObjectIdx(int idx)
    {
        List<int> idxs = new List<int>();

        for (int i = 0; i < objectCount; i++)
            if (objectIdx[i] == idx || objectIdx[i] == 0)
                idxs.Add(i);

        return idxs;
    }

    void LoadOutbreak(int idx)
    {
        TextAsset textAsset = Resources.Load<TextAsset>("Jsons/Dungeons/Outbreak");
        string loadStr = textAsset.text;
        JsonData json = JsonMapper.ToObject(loadStr);

        this.idx = idx;
        region = (int)json[idx]["region"];
        chapter = (int)json[idx]["chapter"];
        script = json[idx]["script"].ToString();

        type = (QuestType)(int)json[idx]["type"];
        objectCount = (int)json[idx]["objectCount"];
        objectIdx = new int[objectCount];
        objectAmt = new int[objectCount];
        for (int i = 0; i < objectCount; i++)
        {
            objectIdx[i] = (int)json[idx]["objectIdx"][i];
            objectAmt[i] = (int)json[idx]["objectAmt"][i];
        }

        rewardCount = (int)json[idx]["rewardCount"];
        rewardIdx = new int[rewardCount];
        rewardAmt = new int[rewardCount];
        for (int i = 0; i < rewardCount; i++)
        {
            rewardIdx[i] = (int)json[idx]["rewardIdx"][i];
            rewardAmt[i] = (int)json[idx]["rewardAmt"][i];
        }
    }
    void LoadQuest(int idx)
    {
        TextAsset textAsset = Resources.Load<TextAsset>("Jsons/Quests/Quest");
        string loadStr = textAsset.text;
        JsonData json = JsonMapper.ToObject(loadStr);

        this.idx = idx;
        region = (int)json[idx]["region"];
        chapter = (int)json[idx]["chapter"];
        script = json[idx]["script"].ToString();

        lvl = (int)json[idx]["lvl"];
        requestIdx = (int)json[idx]["requestIdx"];
        type = (QuestType)(int)json[idx]["type"];
        objectCount = (int)json[idx]["objectCount"];
        objectIdx = new int[objectCount];
        objectAmt = new int[objectCount];
        for (int i = 0; i < objectCount; i++)
        {
            objectIdx[i] = (int)json[idx]["objectIdx"][i];
            objectAmt[i] = (int)json[idx]["objectAmt"][i];
        }

        rewardCount = (int)json[idx]["rewardCount"];
        rewardIdx = new int[rewardCount];
        rewardAmt = new int[rewardCount];
        for (int i = 0; i < rewardCount; i++)
        {
            rewardIdx[i] = (int)json[idx]["rewardIdx"][i];
            rewardAmt[i] = (int)json[idx]["rewardAmt"][i];
        }
    }
}