using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LitJson;
using System.Linq;

public class EquipBluePrint
{
    ///<summary> 장비 인덱스 </summary>
    public int idx;
    ///<summary> 장비 이름 </summary>
    public string name;

    ///<summary> 장비 사용 클래스, 0 공용, 10 기계, 11 마법 </summary> 
    public int useClass;
    ///<summary> 장비 부위 </summary>
    public EquipPart part;
    ///<summary> 장비 카테고리, 드랍용 </summary>
    public int category;
    ///<summary> 장비 세트 </summary>
    public int set;
    ///<summary> 장비 요구 레벨 </summary>
    public int reqlvl;
    ///<summary> 장비 등급 </summary>
    public Rarity rarity;

    ///<summary> 제작에 필요한 재료
    ///<para> Key : 재료 idx, Value : 재료 요구 갯수 </para> </summary>
    public List<Pair<int, int>> requireResources = new List<Pair<int, int>>();
    ///<summary> 0이면 없음, 13이면 랜덤, 그 외엔 지정 </summary>
    public int[] commonStats;

    static JsonData equipJson = null;
    ///<summary> 자원 소비량 저장 json
    ///<para> 각 토큰마다 6개 항목(상중하 특수, 상중하 일반 순) </para> </summary>
    static JsonData resourceJson = null;

    static EquipBluePrint()
    {
        equipJson = JsonMapper.ToObject(Resources.Load<TextAsset>("Jsons/Items/Equip").text);
        resourceJson = JsonMapper.ToObject(Resources.Load<TextAsset>("Jsons/Items/EquipResource").text);
    }
    public EquipBluePrint() { }
    public EquipBluePrint(int jsonIdx)
    {
        this.idx = (int)equipJson[jsonIdx]["idx"];
        name = equipJson[jsonIdx]["name"].ToString();
        useClass = (int)equipJson[jsonIdx]["class"];
        part = (EquipPart)(int)equipJson[jsonIdx]["part"];
        category = (int)equipJson[jsonIdx]["category"];
        set = (int)equipJson[jsonIdx]["set"];
        reqlvl = (int)equipJson[jsonIdx]["reqlvl"];
        rarity = (Rarity)(int)equipJson[jsonIdx]["rarity"];

        commonStats = new int[3];
        for (int i = 0; i < 3; i++)
            commonStats[i] = (int)equipJson[jsonIdx]["commonStat"][i];


        int resourceIdx = (reqlvl / 2) * 5 + (rarity - Rarity.Common);
        int type = (part <= EquipPart.Weapon) ? 0 : (part <= EquipPart.Shoes ? 1 : 2);

        int amount;
        //특수 재화 소비량 읽기
        //idx 4 ~ 12, 상중하-무기,방어구,장신구 순
        for (int i = 0; i < 3; i++)
            if ((amount = (int)resourceJson[resourceIdx]["resource"][i]) != 0)
                requireResources.Add(new Pair<int, int>(4 + i + type * 3, amount));
        //기본 재화 소비량 읽기
        //idx 13 ~ 15, 상중하 순
        for (int i = 3; i < 6; i++)
            if ((amount = (int)resourceJson[resourceIdx]["resource"][i]) != 0)
                requireResources.Add(new Pair<int, int>(10 + i, amount));
    }
}

public class Equipment
{
    public EquipBluePrint ebp;

    ///<summary> 장비 성급, 1성 시작, 융합 시 최대 3성 </summary>
    public int star;

    ///<summary> 주 스텟 </summary>
    public Obj mainStat;
    ///<summary> 주 스텟 값 </summary>
    public int mainStatValue;
    ///<summary> 보조 스텟 </summary>
    public Obj subStat;
    ///<summary> 보조 스텟 값 </summary>
    public int subStatValue;
    ///<summary> 공통 옵션 값 </summary>
    public List<Pair<Obj, int>> commonStatValue = new List<Pair<Obj, int>>();

    #region StatPool
    ///<summary> 무기 보조 스텟 가능 풀 </summary>
    static readonly Obj[] weaponSubstatKindPool = new Obj[5];
    ///<summary> 목걸이 스텟 가능 풀, 장신구는 메인, 서브 스텟 풀 같음 </summary>
    static readonly Obj[] necklaceStatKindPool = new Obj[4];
    ///<summary> 반지 스텟 가능 풀, 장신구는 메인, 서브 스텟 풀 같음 </summary>
    static readonly Obj[] ringStatKindPool = new Obj[4];

    static readonly JsonData weaponStatJson;
    static readonly JsonData armorStatJson;
    static readonly JsonData accessoryStatJson;
    static readonly JsonData commonStatJson;
    #endregion StatPool

    ///<summary> 장비 정렬을 위한 비교 함수 
    ///<para> idx major(오름차순), star minor(내림차순) </para> </summary>
    public int CompareTo(Equipment e)
    {
        int result = ebp.idx.CompareTo(e.ebp.idx);
        if(result == 0)
        {
            if(star > e.star)
                return -1;
            else if(star < e.star)
                return 1;
            else
                return 0;
        }
        return result;
    }
    ///<summary> 데이터 로드를 위한 빈 생성자 </summary>
    public Equipment() { }
    ///<summary> json 데이터 로드 </summary>
    static Equipment()
    {
        weaponSubstatKindPool[0] = Obj.행동력; weaponSubstatKindPool[1] = Obj.명중;
        weaponSubstatKindPool[2] = Obj.치명타율; weaponSubstatKindPool[3] = Obj.방어력무시; weaponSubstatKindPool[4] = Obj.속도;

        necklaceStatKindPool[0] = Obj.체력; necklaceStatKindPool[1] = Obj.방어력; necklaceStatKindPool[2] = Obj.회피; necklaceStatKindPool[3] = Obj.속도;
        ringStatKindPool[0] = Obj.공격력; ringStatKindPool[1] = Obj.명중; ringStatKindPool[2] = Obj.치명타율; ringStatKindPool[3] = Obj.방어력무시;

        weaponStatJson = JsonMapper.ToObject(Resources.Load<TextAsset>("Jsons/Items/WeaponStat").text);
        armorStatJson = JsonMapper.ToObject(Resources.Load<TextAsset>("Jsons/Items/ArmorStat").text);
        accessoryStatJson = JsonMapper.ToObject(Resources.Load<TextAsset>("Jsons/Items/AccessoryStat").text);
        commonStatJson = JsonMapper.ToObject(Resources.Load<TextAsset>("Jsons/Items/CommonStat").text);
    }
    ///<summary> 새로운 장비 생성 </summary>
    public Equipment(EquipBluePrint ebp)
    {
        this.ebp = ebp;
        star = 1;

        SetMainStatKind();
        SetSubStatKind();
        SetCommonStat();

        SetStatValue();
    }

    public void ReCreate()
    {
        SetMainStatKind();
        SetSubStatKind();
        SetCommonStat();

        SetStatValue();
    }

    ///<summary> 메인 스텟 종류 결정
    ///<para> 무기, 방어구는 결정되어있음, 장신구는 풀에서 랜덤 결정 </para> </summary>
    void SetMainStatKind()
    {
        switch (ebp.part)
        {
            case EquipPart.Weapon:
                mainStat = Obj.공격력;
                break;
            case EquipPart.Top:
            case EquipPart.Pants:
            case EquipPart.Gloves:
            case EquipPart.Shoes:
                mainStat = Obj.방어력;
                break;
            case EquipPart.Ring:
                mainStat = ringStatKindPool[Random.Range(0, 4)];
                break;
            case EquipPart.Necklace:
                mainStat = necklaceStatKindPool[Random.Range(0, 4)];
                break;
        }
    }
    ///<summary> 서브 스텟 종류 결정
    ///<para> 언커먼 이상에서만 작동, 방어구는 결정되어 있음, 장신구는 메인 스텟과 겹치지 않음 </summary>
    void SetSubStatKind()
    {
        if (ebp.rarity >= Rarity.Uncommon)
        {
            switch (ebp.part)
            {
                case EquipPart.Weapon:
                    subStat = weaponSubstatKindPool[Random.Range(0, 5)];
                    break;
                case EquipPart.Top:
                    subStat = Obj.체력;
                    break;
                case EquipPart.Pants:
                    subStat = Obj.회피;
                    break;
                case EquipPart.Gloves:
                    subStat = Obj.행동력;
                    break;
                case EquipPart.Shoes:
                    subStat = Obj.속도;
                    break;
                case EquipPart.Ring:
                    do
                        subStat = ringStatKindPool[Random.Range(0, 4)];
                    while (mainStat == subStat);
                    break;
                case EquipPart.Necklace:
                    do
                        subStat = necklaceStatKindPool[Random.Range(0, 4)];
                    while (mainStat == subStat);
                    break;
            }
        }
    }
    ///<summary> 공통 옵션 종류 결정
    ///<para> 레어 이상에서만 작동 </summary>
    void SetCommonStat()
    {
        if (ebp.rarity < Rarity.Rare) return;
        
        commonStatValue.Clear();
        int[] commonStatObjs = new int[ebp.commonStats.Count(x => x > 0)];
        ResetCommonStat();

        for (int i = 0; i < commonStatObjs.Length; i++)
            commonStatValue.Add(new Pair<Obj, int>((Obj)commonStatObjs[i], (int)commonStatJson[ebp.reqlvl / 2]["stat"][commonStatObjs[i]]));

        void ResetCommonStat()
        {
            for (int i = 0; i < commonStatObjs.Length; i++)
            {
                if(ebp.commonStats[i] != 13)
                    commonStatObjs[i] = ebp.commonStats[i];
                else
                {
                    bool isOverlap;
                    do
                    {
                        commonStatObjs[i] = Random.Range(1, 13);
                        isOverlap = commonStatObjs[i] == 1 || commonStatObjs[i] == 3;
                        for(int j = 0; !isOverlap && j < i;j++)
                            isOverlap |= commonStatObjs[i] == commonStatObjs[j];
                    } while(isOverlap);
                }
            }
        }
    }

    ///<summary> 아이템 메인 및 서브 스텟 값 불러오기 </summary>
    void SetStatValue()
    {
        JsonData json = ebp.part <= EquipPart.Weapon ? weaponStatJson : accessoryStatJson;

        if (EquipPart.Top <= ebp.part && ebp.part <= EquipPart.Shoes)
        {
            mainStatValue = (int)armorStatJson[ebp.reqlvl / 2 * 5 + (ebp.rarity - Rarity.Common)][$"{ebp.part}"][0];
            if (subStat != Obj.None) subStatValue = (int)armorStatJson[ebp.reqlvl / 2 * 5 + (ebp.rarity - Rarity.Common)][$"{ebp.part}"][1];
        }
        else 
        {
            mainStatValue = (int)json[ebp.reqlvl / 2 * 5 + (ebp.rarity - Rarity.Common)][$"{mainStat}"];
            if(subStat != Obj.None) subStatValue = (int)json[ebp.reqlvl / 2 * 5 + (ebp.rarity - Rarity.Common)][$"{subStat}"];
        }

        float pivot = star <= 1 ? 1 : star <= 2 ? 1.2f : 1.5f;
        mainStatValue = Mathf.RoundToInt(mainStatValue * pivot);
        subStatValue = Mathf.RoundToInt(subStatValue * pivot);
    }

    ///<summary> 옵션 변환 가능 여부 - 레어 이상 장비 </summary>
    public bool CanSwitchCommonStat() => ebp.rarity >= Rarity.Rare;
    ///<summary> 공통 옵션 변환 </summary>
    public void SwitchCommonStat()
    {
        if (!CanSwitchCommonStat())
            return;

        SetCommonStat();
    }
    ///<summary> 장비 융합 </summary>
    public void Merge()
    {
        star = Mathf.Min(star + 1, 3);
        SetStatValue();
    }
}

public class Skillbook
{
    public int idx;
    public int count;

    public Skillbook() { }
    public Skillbook(int idx, int count)
    {
        this.idx = idx; this.count = count;
    }
}

public class Potion
{
    static JsonData json;

    public int idx;
    public string name;
    public string script;

    static Potion()
    {
        json = JsonMapper.ToObject(Resources.Load<TextAsset>("Jsons/Items/Potion").text);
    }
    public Potion(int potionIdx)
    {
        idx = potionIdx;
        name = json[potionIdx - (int)json[0]["idx"]]["name"].ToString();
        script = json[potionIdx - (int)json[0]["idx"]]["script"].ToString();
    }
}