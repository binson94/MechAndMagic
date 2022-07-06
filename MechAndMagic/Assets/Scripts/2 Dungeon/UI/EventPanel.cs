using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using LitJson;

public class EventPanel : MonoBehaviour
{
    ///<summary> 이벤트 정보 로드 </summary>
    EventInfo eventInfo;
    ///<summary> 광고 시청 여부 </summary>
    bool isWatch;

    ///<summary> 이벤트 설명 텍스트 </summary>
    [Header("Event Info")]
    [SerializeField] Text eventTxt;
    ///<summary> 긍정, 중립, 부정 표기 이미지 </summary>
    [SerializeField] Image eventIcon;
    ///<summary> 0 긍정, 1 중립, 2 부정 스프라이트 </summary>
    [SerializeField] Sprite[] iconSprites;

    ///<summary> 긍정, 중립 이벤트인 경우 닫기 버튼만 표시 </summary>
    [Header("Action")]
    [SerializeField] GameObject posBtns;
    ///<summary> 부정 이벤트인 경우 광고, 닫기 버튼 표시 </summary>
    [SerializeField] GameObject negBtns;
    ///<summary> 광고 시청 시 버튼 알파값 감소 </summary>
    [SerializeField] Image adBtnImage;
    ///<summary> 광고 시청 시 버튼 텍스트 알파값 감소 </summary>
    [SerializeField] Text adTxt;

    ///<summary> 이벤트 방 도달 시 호출 </summary>
    public void OnEventRoom()
    {
        //이벤트 정보 불러오기
        eventInfo = new EventInfo(GameManager.instance.slotData.dungeonData.currRoomEvent);
        //설명 및 아이콘 설정
        eventTxt.text = eventInfo.script;
        eventIcon.sprite = iconSprites[eventInfo.eventType - 2];

        if(eventInfo.eventType != 4)
            EventEffect();

        posBtns.SetActive(eventInfo.eventType != 4);
        negBtns.SetActive(eventInfo.eventType == 4);
        adBtnImage.color = new Color(1, 1, 1, 1);
        adTxt.color = new Color(1, 1, 1, 1);
    }

    ///<summary> 광고보고 부정적 효과 제거 </summary>
    public void Btn_RemoveNegEffect()
    {
        if(isWatch) return;

        Debug.Log("show ad");
        isWatch = true; // 광고 끝까지 시청 여부 받아옴

        adBtnImage.color = new Color(1, 1, 1, 100f / 255);
        adTxt.color = new Color(1, 1, 1, 100f / 255);

    }
    ///<summary> 부정적 효과 그냥 받기 </summary>
    public void Btn_ClosePanel()
    {
        if(eventInfo.eventType == 4 && isWatch)
            EventEffect();
        gameObject.SetActive(false);
    }

    void EventEffect()
    {
        for (int i = 0; i < eventInfo.typeCount; i++)
        {
            switch ((EventType)eventInfo.type[i])
            {
                case EventType.GetEXP:
                    GameManager.instance.EventGetExp(eventInfo.typeRate[i]);
                    break;
                case EventType.LossExp:
                    GameManager.instance.EventLoseExp(eventInfo.typeRate[i]);
                    break;
                case EventType.GetItem:
                    int category = 0, amt;
                    amt = eventInfo.typeRate[i] > 0 ? Mathf.RoundToInt(eventInfo.typeRate[i]) : GameManager.instance.slotData.lvl;
                    switch ((EventItem)eventInfo.typeObj[i])
                    {
                        //19, 20, 21, 22, 23 - 97531
                        case EventItem.Skillbook:
                            category = 23 - (GameManager.instance.slotData.lvl - 1) / 2;
                            break;
                        //13, 14, 15 - 상중하
                        case EventItem.CommonEquipMaterial:
                            category = 15 - GameManager.instance.slotData.lvl / 4;
                            break;
                        //1, 2, 3 - 상중하
                        case EventItem.CommonSkillMaterial:
                            category = 3 - GameManager.instance.slotData.lvl / 4;
                            break;
                        case EventItem.Recipe:
                            switch (GameManager.instance.slotData.lvl)
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
                            category = 10 - GameManager.instance.slotData.lvl / 4 * 3 + Random.Range(0, 3);
                            break;
                    }
                    ItemManager.ItemDrop(category, amt);
                    break;
                case EventType.Heal:
                    GameManager.instance.EventGetHeal(eventInfo.typeRate[i]);
                    break;
                case EventType.Damage:
                    GameManager.instance.EventGetDamage(eventInfo.typeRate[i]);
                    break;
                case EventType.Buff:
                    GameManager.instance.EventAddBuff(new DungeonBuff(eventInfo.name, eventInfo.typeObj[i], eventInfo.typeRate[i]));
                    break;
                case EventType.Debuff:
                    GameManager.instance.EventAddDebuff(new DungeonBuff(eventInfo.name, eventInfo.typeObj[i], eventInfo.typeRate[i]));
                    break;
            }
        }
    }

    enum EventType
    {
        GetEXP = 1, LossExp, GetItem, Heal, Damage, Buff, Debuff
    }
    enum EventItem
    {
        Skillbook, CommonEquipMaterial, CommonSkillMaterial, Recipe, SpecialEquipMaterial
    }
}
public class EventInfo
{
    ///<summary> 이벤트 인덱스 </summary>
    public int idx;
    ///<summary> 이벤트 이름 </summary>
    public string name;
    ///<summary> 이벤트 설명 </summary>
    public string script;
    ///<summary> 이벤트 종류
    ///<para> 2 긍정, 3 중립, 4 부정 </para> </summary>
    public int eventType;

    ///<summary> 효과 갯수 </summary>
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
        eventType = (int)json[idx]["event"];

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
public class DungeonBuff
{
    public int count;

    public string name;
    public int objIdx;
    public double rate;

    public DungeonBuff() { }
    public DungeonBuff(string n, int obj, float r)
    {
        name = n;
        objIdx = obj;
        rate = (double)r;
        count = 2;
    }
}

