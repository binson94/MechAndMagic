using UnityEngine;
using LitJson;

public class RoomInfo
{
    public int roomIdx;
    public int roomExp;

    public int monsterCount;
    public int[] monsterIdx;
    
    public int ItemCount;
    public int[] ItemIdx;
    public float[] ItemChance;

    static JsonData json;

    static RoomInfo()
    {
        TextAsset txt = Resources.Load<TextAsset>("Jsons/Dungeons/MonsterRoom");
        json = JsonMapper.ToObject(txt.text);
    }
    public RoomInfo(int roomIdx)
    {
        this.roomIdx = roomIdx;
        int jsonIdx = roomIdx - (int)json[0]["roomIdx"];

        monsterCount = (int)json[jsonIdx]["monsterCount"];
        monsterIdx = new int[monsterCount];
        for (int i = 0; i < monsterCount; i++)
            monsterIdx[i] = (int)json[jsonIdx]["monsterIdx"][i];

        roomExp = (int)json[jsonIdx]["roomExp"];

        ItemCount = (int)json[jsonIdx]["ItemCount"];
        ItemIdx = new int[ItemCount];
        ItemChance = new float[ItemCount];

        for (int i = 0; i < ItemCount; i++)
        {
            ItemIdx[i] = (int)json[jsonIdx]["ItemIdx"][i];
            ItemChance[i] = float.Parse(json[jsonIdx]["ItemChance"][i].ToString());
        }
    }
}
