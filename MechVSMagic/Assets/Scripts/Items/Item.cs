using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LitJson;

public enum EquipPart
{
    None, Weapon, Top, Pants, Gloves, Shoes, Ring, Necklace
}
public enum ItemCategory
{
    None, Weapon, Armor, Accessory, Recipe, Skillbook, Resource
}
public enum Rarity
{
    None, Common, Uncommon, Rare, Unique, Legendary
}

[System.Serializable]
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

    //0인 경우, 장비에 뜰 수 있는 범위 내 랜덤
    public StatName mainStat;
    public StatName subStat;
    
    static JsonData json = null;

    public EquipBluePrint() { }
    public EquipBluePrint(int idx)
    {
        if (json == null)
        {
            TextAsset loadStr = Resources.Load<TextAsset>("Jsons/Items/Equip");
            string txt = loadStr.text;
            json = JsonMapper.ToObject(txt);
        }

        this.idx = idx;
        name = json[idx]["name"].ToString();
        useClass = (int)json[idx]["class"];
        part = (EquipPart)(int)json[idx]["part"];
        script = json[idx]["script"].ToString();
        category = (int)json[idx]["category"];
        set = (int)json[idx]["set"];
        reqlvl = (int)json[idx]["reqlvl"];
        rarity = (Rarity)(int)json[idx]["rarity"];
        mainStat = (StatName)(int)json[idx]["mainStat"];
        subStat = (StatName)(int)json[idx]["subStat"];

        equipOption = new int[4];
        for (int i = 0; i < 4; i++)
            equipOption[i] = (int)json[idx]["equipOption"][i];
    }
}

[System.Serializable]
public class Equipment
{
    public readonly EquipBluePrint ebp;

    public StatName mainStat;
    public int mainStatValue;
    public StatName subStat;
    public int subStatValue;

    static readonly StatName[] weaponSubs;
    static readonly StatName[] rings;
    static readonly StatName[] necks;
    static Dictionary<StatName, int[]> weaponSubStats = new Dictionary<StatName, int[]>();
    static Dictionary<StatName, int[]> armorSubStats = new Dictionary<StatName, int[]>();
    static Dictionary<StatName, int[]> accessoryStats = new Dictionary<StatName, int[]>();

    public Equipment() { }
    static Equipment()
    {
        TextAsset jsonTxt;
        string jsonStr;
        JsonData json;

        weaponSubs = new StatName[5];
        weaponSubs[0] = StatName.ACC; weaponSubs[1] = StatName.SPD; weaponSubs[2] = StatName.AP; weaponSubs[3] = StatName.CRC; weaponSubs[4] = StatName.PEN;

        rings = new StatName[4];
        rings[0] = StatName.ATK; rings[1] = StatName.ACC; rings[2] = StatName.CRC; rings[3] = StatName.PEN;

        necks = new StatName[4];
        necks[0] = StatName.HP; rings[1] = StatName.DEF; rings[2] = StatName.DOG; rings[3] = StatName.SPD;

        #region DataLoad
        weaponSubStats.Add(StatName.ACC, new int[5]);
        weaponSubStats.Add(StatName.SPD, new int[5]);
        weaponSubStats.Add(StatName.AP, new int[5]);
        weaponSubStats.Add(StatName.CRC, new int[5]);
        weaponSubStats.Add(StatName.PEN, new int[5]);

        jsonTxt = Resources.Load<TextAsset>("Jsons/Items/WeaponStat");
        jsonStr = jsonTxt.text;
        json = JsonMapper.ToObject(jsonStr);
        for (int i = 0; i < 5; i++)
        {
            weaponSubStats[StatName.ACC][i] = (int)json[i]["ACC"];
            weaponSubStats[StatName.SPD][i] = (int)json[i]["SPD"];
            weaponSubStats[StatName.AP][i] = (int)json[i]["AP"];
            weaponSubStats[StatName.CRC][i] = (int)json[i]["CRC"];
            weaponSubStats[StatName.PEN][i] = (int)json[i]["PEN"];
        }

        armorSubStats.Add(StatName.HP, new int[5]);
        armorSubStats.Add(StatName.DOG, new int[5]);
        armorSubStats.Add(StatName.ACC, new int[5]);
        armorSubStats.Add(StatName.SPD, new int[5]);

        jsonTxt = Resources.Load<TextAsset>("Jsons/Items/ArmorStat");
        jsonStr = jsonTxt.text;
        json = JsonMapper.ToObject(jsonStr);
        for (int i = 0; i < 5; i++)
        {
            armorSubStats[StatName.HP][i] = (int)json[i]["HP"];
            armorSubStats[StatName.DOG][i] = (int)json[i]["DOG"];
            armorSubStats[StatName.ACC][i] = (int)json[i]["ACC"];
            armorSubStats[StatName.SPD][i] = (int)json[i]["SPD"];
        }

        accessoryStats.Add(StatName.ATK, new int[6]);
        accessoryStats.Add(StatName.ACC, new int[6]);
        accessoryStats.Add(StatName.CRC, new int[6]);
        accessoryStats.Add(StatName.PEN, new int[6]);
        accessoryStats.Add(StatName.HP, new int[6]);
        accessoryStats.Add(StatName.DEF, new int[6]);
        accessoryStats.Add(StatName.DOG, new int[6]);
        accessoryStats.Add(StatName.SPD, new int[6]);

        jsonTxt = Resources.Load<TextAsset>("Jsons/Items/AccessoryStat");
        jsonStr = jsonTxt.text;
        json = JsonMapper.ToObject(jsonStr);
        for (int i = 0; i < 6; i++)
        {
            accessoryStats[StatName.ATK][i] = (int)json[i]["ATK"];
            accessoryStats[StatName.ACC][i] = (int)json[i]["ACC"];
            accessoryStats[StatName.CRC][i] = (int)json[i]["CRC"];
            accessoryStats[StatName.PEN][i] = (int)json[i]["PEN"];
            accessoryStats[StatName.HP][i] = (int)json[i]["HP"];
            accessoryStats[StatName.DEF][i] = (int)json[i]["DEF"];
            accessoryStats[StatName.DOG][i] = (int)json[i]["DOG"];
            accessoryStats[StatName.SPD][i] = (int)json[i]["SPD"];
        }
        #endregion
    }

    public Equipment(EquipBluePrint ebp)
    {
        this.ebp = ebp;
        //메인 스텟 결정
        if(ebp.mainStat == StatName.None)
        {
            switch(ebp.part)
            {
                case EquipPart.Weapon:
                    mainStat = StatName.ATK;
                    break;
                case EquipPart.Top:
                case EquipPart.Pants:
                case EquipPart.Gloves:
                case EquipPart.Shoes:
                    mainStat = StatName.DEF;
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
        //언커먼 이상 장비 - subStat
        if (ebp.rarity >= Rarity.Uncommon)
        {
            if (ebp.subStat == StatName.None)
            {
                switch (ebp.part)
                {
                    case EquipPart.Weapon:
                        subStat = weaponSubs[Random.Range(0, 5)];
                        break;
                    case EquipPart.Top:
                        subStat = StatName.HP;
                        break;
                    case EquipPart.Pants:
                        subStat = StatName.DOG;
                        break;
                    case EquipPart.Gloves:
                        subStat = StatName.ACC;
                        break;
                    case EquipPart.Shoes:
                        subStat = StatName.SPD;
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
            subStat = StatName.None;
        }

        StatValueSet();

        void StatValueSet()
        {
            #region mainStat
            float mainLvl = ebp.reqlvl + ((int)ebp.rarity - 1) * 0.5f;

            switch(ebp.part)
            {
                case EquipPart.Weapon:
                    mainStatValue = Mathf.RoundToInt(Mathf.Pow(2, (mainLvl + 5) / 2));
                    break;
                case EquipPart.Top:
                case EquipPart.Pants:
                    mainStatValue = Mathf.RoundToInt(Mathf.Pow(2, (mainLvl + 1) / 2));
                    break;
                case EquipPart.Gloves:
                case EquipPart.Shoes:
                    mainStatValue = Mathf.RoundToInt(Mathf.Pow(2, (mainLvl - 1) / 2));
                    break;
                case EquipPart.Ring:
                case EquipPart.Necklace:
                    mainStatValue = Mathf.RoundToInt(accessoryStats[mainStat][(int)(mainLvl / 2)] + (accessoryStats[mainStat][Mathf.CeilToInt(mainLvl / 2)] - accessoryStats[mainStat][(int)(mainLvl / 2)]) * (mainLvl - (int)mainLvl));
                    break;
            }
            #endregion

            #region subStat
            if (subStat != StatName.None)
            {
                float div = 1;
                switch(ebp.rarity)
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

                switch(ebp.part)
                {
                    case EquipPart.Weapon:
                        subStatValue = Mathf.RoundToInt(weaponSubStats[subStat][ebp.reqlvl / 2] / div);
                        break;
                    case EquipPart.Top:
                    case EquipPart.Pants:
                    case EquipPart.Gloves:
                    case EquipPart.Shoes:
                        subStatValue = Mathf.RoundToInt(armorSubStats[subStat][ebp.reqlvl / 2] / div);
                        break;
                    case EquipPart.Ring:
                    case EquipPart.Necklace:
                        subStatValue = Mathf.RoundToInt(accessoryStats[subStat][ebp.reqlvl / 2] / div);
                        break;
                }
            }
            #endregion
        }
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

