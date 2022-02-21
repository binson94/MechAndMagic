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
    public RoomInfo(int idx)
    {
        roomIdx = idx;
        monsterCount = (int)json[idx]["monsterCount"];
        monsterIdx = new int[monsterCount];
        for (int i = 0; i < monsterCount; i++)
            monsterIdx[i] = (int)json[idx]["monsterIdx"][i];

        roomExp = (int)json[idx]["roomExp"];

        ItemCount = (int)json[idx]["ItemCount"];
        ItemIdx = new int[ItemCount];
        ItemChance = new float[ItemCount];
        for (int i = 0; i < ItemCount; i++)
        {
            ItemIdx[i] = (int)json[idx]["ItemIdx"][i];
            ItemChance[i] = float.Parse(json[idx]["ItemChance"][i].ToString());
        }
    }
}
