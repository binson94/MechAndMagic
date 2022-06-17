using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using LitJson;

public class EventManager : MonoBehaviour
{
    [SerializeField] Text eventText;
    EventInfo eventInfo;

    private void Start()
    {
        SoundManager.instance.PlayBGM(BGM.Battle1);
        eventInfo = new EventInfo(GameManager.slotData.dungeonState.currRoomEvent);
        eventText.text = eventInfo.script;

        EventEffect();
    }

    void EventEffect()
    {
        for(int i =0;i<eventInfo.typeCount;i++)
        {
            switch ((EventType)eventInfo.type[i])
            {
                case EventType.GetEXP:
                    GameManager.EventGetExp(eventInfo.typeRate[i]);
                    break;
                case EventType.LossExp:
                    GameManager.EventLoseExp(eventInfo.typeRate[i]);
                    break;
                case EventType.GetItem:
                    int category = 0, amt;
                    amt = eventInfo.typeRate[i] > 0 ? Mathf.RoundToInt(eventInfo.typeRate[i]) : GameManager.slotData.lvl;
                    switch ((EventItem)eventInfo.typeObj[i])
                    {
                        //19, 20, 21, 22, 23 - 97531
                        case EventItem.Skillbook:
                            category = 23 - (GameManager.slotData.lvl - 1) / 2;
                            break;
                        //13, 14, 15 - 상중하
                        case EventItem.CommonEquipMaterial:
                            category = 15 - GameManager.slotData.lvl / 4;
                            break;
                        //1, 2, 3 - 상중하
                        case EventItem.CommonSkillMaterial:
                            category = 3 - GameManager.slotData.lvl / 4;
                            break;
                        case EventItem.Recipe:
                            switch(GameManager.slotData.lvl)
                            {
                                case 1:
                                case 2:
                                    category = Random.Range(81, 84);
                                    break;
                                case 3:
                                case 4:
                                    category = Random.Range(132, 141);
                                    break;
                                case 5:
                                case 6:
                                    category = Random.Range(120, 129);
                                    break;
                                case 7:
                                case 8:
                                    category = Random.Range(105, 114);
                                    break;
                                case 9:
                                case 10:
                                    category = Random.Range(90, 99);
                                    break;
                            }
                            break;
                        //4, 5, 6, 7, 8, 9, 10, 11, 12 - 상무상방상장 중무중방중장 하무하방하장
                        case EventItem.SpecialEquipMaterial:
                            category = 10 - GameManager.slotData.lvl / 4 * 3 + Random.Range(0, 3);
                            break;
                    }
                    ItemManager.ItemDrop(category, amt);
                    break;
                case EventType.Heal:
                    GameManager.EventGetHeal(eventInfo.typeRate[i]);
                    break;
                case EventType.Damage:
                    GameManager.EventGetDamage(eventInfo.typeRate[i]);
                    break;
                case EventType.Buff:
                    GameManager.EventAddBuff(new DungeonBuff(eventInfo.name, eventInfo.typeObj[i], eventInfo.typeRate[i]));
                    break;
                case EventType.Debuff:
                    GameManager.EventAddDebuff(new DungeonBuff(eventInfo.name, eventInfo.typeObj[i], eventInfo.typeRate[i]));
                    break;
            }
        }
    }

    public void Btn_BackToMap()
    {
        GameManager.SwitchSceneData(SceneKind.Dungeon);
        QuestManager.QuestUpdate(QuestType.Event, 0, 1);
        SceneManager.LoadScene("2_0 Dungeon");
    }

    enum EventType
    {
        GetEXP = 1, LossExp, GetItem, Heal, Damage, Buff, Debuff
    }
    enum EventItem
    {
        Skillbook, CommonEquipMaterial, CommonSkillMaterial, Recipe, SpecialEquipMaterial
    }
    class EventInfo
{
    public int idx;
    public string name;
    public string script;

    public int typeCount;
    public int[] type;
    public int[] typeObj;
    public float[] typeRate;

    static JsonData json;

    static EventInfo() => json = JsonMapper.ToObject(Resources.Load<TextAsset>("Jsons/Dungeons/Event").text);

    public EventInfo(int idx)
    {
        this.idx = idx;
        name = json[idx]["name"].ToString();
        script = json[idx]["script"].ToString();

        typeCount = (int)json[idx]["typeCount"];
        type = new int[typeCount];
        typeObj = new int[typeCount];
        typeRate = new float[typeCount];

        for (int i = 0; i < typeCount; i++)
        {
            type[i] = (int)json[idx]["type"][i];
            typeObj[i] = (int)json[idx]["typeObj"][i];
            typeRate[i] = float.Parse(json[idx]["typeRate"][i].ToString());
        }
    }
}
}
public class DungeonBuff
{
    public int count;

    public string name;    
    public int objIdx;
    public double rate;

    public DungeonBuff() {}
    public DungeonBuff(string n, int obj, float r)
    {
        name = n;
        objIdx = obj;
        rate = (double)r;
        count = 2;
    }
}

