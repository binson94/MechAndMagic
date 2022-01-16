using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LitJson;


public enum EquipPart
{
    None, Weapon, Top, Pants, Gloves, Shoes, Necklace, Ring
}
public enum ItemCategory
{
    None, Weapon, Armor, Accessory, Recipe, Skillbook, Resource, Potion
}
public enum Rarity
{
    None, Common, Uncommon, Rare, Unique, Legendary
}

public class EquipBluePrint
{
    public int idx;
    public string name;

    public int useClass;
    public EquipPart part;
    public string script;
    public int category;
    public int set;
    public int reqlvl;
    public Rarity rarity;
    public int[] equipOption;

    public List<KeyValuePair<int, int>> requireResources = new List<KeyValuePair<int, int>>();

    //0인 경우, 장비에 뜰 수 있는 범위 내 랜덤
    public Obj mainStat;
    public Obj subStat;

    static JsonData json = null;
    static JsonData resourceJson = null;

    static EquipBluePrint()
    {
        TextAsset loadStr = Resources.Load<TextAsset>("Jsons/Items/Equip");
        string txt = loadStr.text;
        json = JsonMapper.ToObject(txt);

        loadStr = Resources.Load<TextAsset>("Jsons/Items/EquipResource");
        txt = loadStr.text;
        resourceJson = JsonMapper.ToObject(txt);
    }
    public EquipBluePrint() { }
    public EquipBluePrint(int idx)
    {
        this.idx = idx;
        name = json[idx]["name"].ToString();
        useClass = (int)json[idx]["class"];
        part = (EquipPart)(int)json[idx]["part"];
        script = json[idx]["script"].ToString();
        category = (int)json[idx]["category"];
        set = (int)json[idx]["set"];
        reqlvl = (int)json[idx]["reqlvl"];
        rarity = (Rarity)(int)json[idx]["rarity"];
        mainStat = (Obj)(int)json[idx]["mainStat"];
        subStat = (Obj)(int)json[idx]["subStat"];

        equipOption = new int[4];
        for (int i = 0; i < 4; i++)
            equipOption[i] = (int)json[idx]["equipOption"][i];

        if (idx == 0)
            return;

        int pos = (reqlvl - 1) * 5 + (rarity - Rarity.Common);

        int type = (part <= EquipPart.Weapon) ? 0 : (part <= EquipPart.Shoes ? 1 : 2);
        int require;
        for (int i = 0; i < 3; i++)
            if ((require = (int)resourceJson[pos]["resource"][i]) != 0)
                requireResources.Add(new KeyValuePair<int, int>(4 + 3 * i + type, require));
        for (int i = 3; i < 6; i++)
            if ((require = (int)resourceJson[pos]["resource"][i]) != 0)
                requireResources.Add(new KeyValuePair<int, int>(10 + i, require));
    }
}

public class Equipment
{
    public readonly EquipBluePrint ebp;

    public int star;

    public Obj mainStat;
    public int mainStatValue;
    public Obj subStat;
    public int subStatValue;

    static readonly Obj[] weaponSubs;
    static readonly Obj[] rings;
    static readonly Obj[] necks;
    static Dictionary<Obj, int[]> weaponSubStats = new Dictionary<Obj, int[]>();
    static Dictionary<Obj, int[]> armorSubStats = new Dictionary<Obj, int[]>();
    static Dictionary<Obj, int[]> accessoryStats = new Dictionary<Obj, int[]>();

    public Equipment() { }
    static Equipment()
    {
        TextAsset jsonTxt;
        string jsonStr;
        JsonData json;

        weaponSubs = new Obj[5];
        weaponSubs[0] = Obj.ACC; weaponSubs[1] = Obj.SPD; weaponSubs[2] = Obj.AP; weaponSubs[3] = Obj.CRC; weaponSubs[4] = Obj.PEN;

        rings = new Obj[4];
        rings[0] = Obj.ATK; rings[1] = Obj.ACC; rings[2] = Obj.CRC; rings[3] = Obj.PEN;

        necks = new Obj[4];
        necks[0] = Obj.HP; necks[1] = Obj.DEF; necks[2] = Obj.DOG; necks[3] = Obj.SPD;

        #region DataLoad
        weaponSubStats.Add(Obj.ACC, new int[5]);
        weaponSubStats.Add(Obj.SPD, new int[5]);
        weaponSubStats.Add(Obj.AP, new int[5]);
        weaponSubStats.Add(Obj.CRC, new int[5]);
        weaponSubStats.Add(Obj.PEN, new int[5]);

        jsonTxt = Resources.Load<TextAsset>("Jsons/Items/WeaponStat");
        jsonStr = jsonTxt.text;
        json = JsonMapper.ToObject(jsonStr);
        for (int i = 0; i < 5; i++)
        {
            weaponSubStats[Obj.ACC][i] = (int)json[i]["ACC"];
            weaponSubStats[Obj.SPD][i] = (int)json[i]["SPD"];
            weaponSubStats[Obj.AP][i] = (int)json[i]["AP"];
            weaponSubStats[Obj.CRC][i] = (int)json[i]["CRC"];
            weaponSubStats[Obj.PEN][i] = (int)json[i]["PEN"];
        }

        armorSubStats.Add(Obj.HP, new int[5]);
        armorSubStats.Add(Obj.DOG, new int[5]);
        armorSubStats.Add(Obj.ACC, new int[5]);
        armorSubStats.Add(Obj.SPD, new int[5]);

        jsonTxt = Resources.Load<TextAsset>("Jsons/Items/ArmorStat");
        jsonStr = jsonTxt.text;
        json = JsonMapper.ToObject(jsonStr);
        for (int i = 0; i < 5; i++)
        {
            armorSubStats[Obj.HP][i] = (int)json[i]["HP"];
            armorSubStats[Obj.DOG][i] = (int)json[i]["DOG"];
            armorSubStats[Obj.ACC][i] = (int)json[i]["ACC"];
            armorSubStats[Obj.SPD][i] = (int)json[i]["SPD"];
        }

        accessoryStats.Add(Obj.ATK, new int[6]);
        accessoryStats.Add(Obj.ACC, new int[6]);
        accessoryStats.Add(Obj.CRC, new int[6]);
        accessoryStats.Add(Obj.PEN, new int[6]);
        accessoryStats.Add(Obj.HP, new int[6]);
        accessoryStats.Add(Obj.DEF, new int[6]);
        accessoryStats.Add(Obj.DOG, new int[6]);
        accessoryStats.Add(Obj.SPD, new int[6]);

        jsonTxt = Resources.Load<TextAsset>("Jsons/Items/AccessoryStat");
        jsonStr = jsonTxt.text;
        json = JsonMapper.ToObject(jsonStr);
        for (int i = 0; i < 6; i++)
        {
            accessoryStats[Obj.ATK][i] = (int)json[i]["ATK"];
            accessoryStats[Obj.ACC][i] = (int)json[i]["ACC"];
            accessoryStats[Obj.CRC][i] = (int)json[i]["CRC"];
            accessoryStats[Obj.PEN][i] = (int)json[i]["PEN"];
            accessoryStats[Obj.HP][i] = (int)json[i]["HP"];
            accessoryStats[Obj.DEF][i] = (int)json[i]["DEF"];
            accessoryStats[Obj.DOG][i] = (int)json[i]["DOG"];
            accessoryStats[Obj.SPD][i] = (int)json[i]["SPD"];
        }
        #endregion
    }

    public Equipment(EquipBluePrint ebp)
    {
        this.ebp = ebp;

        SetMainStat();
        SetSubStat();
        
        StatValueSet();
    }
    void SetMainStat()
    {
        //메인 스텟 결정
        if (ebp.mainStat == Obj.None)
        {
            switch (ebp.part)
            {
                case EquipPart.Weapon:
                    mainStat = Obj.ATK;
                    break;
                case EquipPart.Top:
                case EquipPart.Pants:
                case EquipPart.Gloves:
                case EquipPart.Shoes:
                    mainStat = Obj.DEF;
                    break;
                case EquipPart.Ring:
                    mainStat = rings[Random.Range(0, 4)];
                    break;
                case EquipPart.Necklace:
                    mainStat = necks[Random.Range(0, 4)];
                    break;
            }
        }
        else
        {
            mainStat = ebp.mainStat;
        }
    }
    void SetSubStat()
    {
        //언커먼 이상 장비일 경우, 서브 스텟 결정
        if (ebp.rarity >= Rarity.Uncommon)
        {
            if (ebp.subStat == Obj.None)
            {
                switch (ebp.part)
                {
                    case EquipPart.Weapon:
                        subStat = weaponSubs[Random.Range(0, 5)];
                        break;
                    case EquipPart.Top:
                        subStat = Obj.HP;
                        break;
                    case EquipPart.Pants:
                        subStat = Obj.DOG;
                        break;
                    case EquipPart.Gloves:
                        subStat = Obj.ACC;
                        break;
                    case EquipPart.Shoes:
                        subStat = Obj.SPD;
                        break;
                    case EquipPart.Ring:
                        do
                        {
                            subStat = rings[Random.Range(0, 4)];
                        } while (mainStat == subStat);
                        break;
                    case EquipPart.Necklace:
                        do
                        {
                            subStat = necks[Random.Range(0, 4)];
                        } while (mainStat == subStat);
                        break;
                }
            }
            else
            {
                subStat = ebp.subStat;
            }
        }
        else
        {
            subStat = Obj.None;
        }
    }
    void StatValueSet()
    {
        float multi = (1 + star * 0.5f);
        #region mainStat
        float mainLvl = ebp.reqlvl + ((int)ebp.rarity - 1) * 0.5f;

        switch (ebp.part)
        {
            case EquipPart.Weapon:
                mainStatValue = Mathf.RoundToInt(multi * Mathf.Pow(2, (mainLvl + 5) / 2));
                break;
            case EquipPart.Top:
            case EquipPart.Pants:
                mainStatValue = Mathf.RoundToInt(multi * Mathf.Pow(2, (mainLvl + 1) / 2));
                break;
            case EquipPart.Gloves:
            case EquipPart.Shoes:
                mainStatValue = Mathf.RoundToInt(multi * Mathf.Pow(2, (mainLvl - 1) / 2));
                break;
            case EquipPart.Ring:
            case EquipPart.Necklace:
                mainStatValue = Mathf.RoundToInt(multi * (accessoryStats[mainStat][(int)(mainLvl / 2)] + 
                    (accessoryStats[mainStat][Mathf.CeilToInt(mainLvl / 2)] - accessoryStats[mainStat][(int)(mainLvl / 2)]) * (mainLvl - (int)mainLvl)));
                break;
        }
        #endregion

        #region subStat
        if (subStat != Obj.None)
        {
            float div = 1;
            switch (ebp.rarity)
            {
                case Rarity.Uncommon:
                    div = 3f;
                    break;
                case Rarity.Rare:
                    div = 2f;
                    break;
                case Rarity.Unique:
                    div = 1.5f;
                    break;
            }

            switch (ebp.part)
            {
                case EquipPart.Weapon:
                    subStatValue = Mathf.RoundToInt(multi * weaponSubStats[subStat][ebp.reqlvl / 2] / div);
                    break;
                case EquipPart.Top:
                case EquipPart.Pants:
                case EquipPart.Gloves:
                case EquipPart.Shoes:
                    subStatValue = Mathf.RoundToInt(multi * armorSubStats[subStat][ebp.reqlvl / 2] / div);
                    break;
                case EquipPart.Ring:
                case EquipPart.Necklace:
                    subStatValue = Mathf.RoundToInt(multi * accessoryStats[subStat][ebp.reqlvl / 2] / div);
                    break;
            }
        }
        #endregion
    }

    public bool CanSwitchMainStat()
    {
        return (ebp.mainStat == Obj.None && ebp.part > EquipPart.Shoes);
    }
    public void SwitchMainStat()
    {
        if (!CanSwitchMainStat())
            return;

        Obj currOption = mainStat;

        while (mainStat == currOption)
            SetMainStat();

        StatValueSet();
    }
    public bool CanSwitchSubStat()
    {
        return (ebp.rarity > Rarity.Common && ebp.subStat == Obj.None && (ebp.part == EquipPart.Weapon || ebp.part == EquipPart.Ring || ebp.part == EquipPart.Necklace));
    }
    public void SwitchSubStat()
    {
        if (!CanSwitchSubStat())
            return;

        Obj currStat = subStat;

        while (subStat == currStat)
            SetSubStat();

        StatValueSet();
    }
    public void Fusion()
    {
        star = Mathf.Min(star + 1, 3);
        StatValueSet();
    }
}

public class Recipe
{
    public int idx;
    public int category;

    public int count;
}

public class Skillbook
{
    public int idx;
    public int lvl;
    public int type;        //0 : active, 1 : passive

    public int count;
}

public class Potion
{

}