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

    public RoomInfo(int idx)
    {
        TextAsset txt = Resources.Load<TextAsset>("Jsons/Dungeons/MonsterRoom");
        string loadStr = txt.text;
        JsonData json = JsonMapper.ToObject(loadStr);

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
