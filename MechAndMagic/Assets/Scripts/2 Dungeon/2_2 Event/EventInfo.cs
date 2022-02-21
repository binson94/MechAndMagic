using UnityEngine;
using LitJson;

public class EventInfo
{
    public string name;
    public int idx;
    public string script;

    public int typeCount;
    public int[] type;

    public EventInfo(int idx)
    {
        TextAsset jsonTxt = Resources.Load<TextAsset>("Jsons/Dungeons/Event");
        string loadStr = jsonTxt.text;
        JsonData json = JsonMapper.ToObject(loadStr);

        name = json[idx]["name"].ToString();
        this.idx = idx;
        script = json[idx]["script"].ToString();

        typeCount = (int)json[idx]["typeCount"];
        type = new int[typeCount];
        for (int i = 0; i < typeCount; i++)
            type[i] = (int)json[idx]["type"][i];
    }
}
