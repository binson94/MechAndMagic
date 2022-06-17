using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LitJson;
using System.Linq;


public enum EquipPart
{
    None, Weapon, Top, Pants, Gloves, Shoes, Ring, Necklace
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
        this.idx = (int)json[idx]["idx"];
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

        int pos = (reqlvl / 2) * 5 + (rarity - Rarity.Common);

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

    public List<KeyValuePair<Obj, int>> commonStatValue = new List<KeyValuePair<Obj, int>>();

    /* #region Static Part */
    static readonly Obj[] weaponObjs = new Obj[6];
    static readonly Obj[] neckObjs = new Obj[4];
    static readonly Obj[] ringObjs = new Obj[4];

    static readonly Dictionary<KeyValuePair<EquipPart, Obj>, int> statIdx = new Dictionary<KeyValuePair<EquipPart, Obj>, int>();
    static readonly Dictionary<EquipPart, Dictionary<KeyValuePair<int, Rarity>, int[]>> equipStats = new Dictionary<EquipPart, Dictionary<KeyValuePair<int, Rarity>, int[]>>();
    static readonly Dictionary<int, int[]> allCommonStats = new Dictionary<int, int[]>();
    /* #endregion Static Part */

    public Equipment() { }
    static Equipment()
    {
        TextAsset jsonTxt;
        string loadStr;
        JsonData wepJson;
        JsonData amrJson;
        JsonData acceJson;

        weaponObjs[0] = Obj.AP; weaponObjs[1] = Obj.ATK; weaponObjs[2] = Obj.ACC;
        weaponObjs[3] = Obj.CRC; weaponObjs[4] = Obj.PEN; weaponObjs[5] = Obj.SPD;

        neckObjs[0] = Obj.HP; neckObjs[1] = Obj.DEF; neckObjs[2] = Obj.DOG; neckObjs[3] = Obj.SPD;
        ringObjs[0] = Obj.ATK; ringObjs[1] = Obj.ACC; ringObjs[2] = Obj.CRC; ringObjs[3] = Obj.PEN;

        for (int i = 0; i < 6; i++)
        {
            statIdx.Add(new KeyValuePair<EquipPart, Obj>(EquipPart.Weapon, weaponObjs[i]), i);

            if (i < 4)
            {
                statIdx.Add(new KeyValuePair<EquipPart, Obj>(EquipPart.Necklace, neckObjs[i]), i);
                statIdx.Add(new KeyValuePair<EquipPart, Obj>(EquipPart.Ring, ringObjs[i]), i);
            }
        }

        equipStats.Add(EquipPart.Weapon, new Dictionary<KeyValuePair<int, Rarity>, int[]>());
        equipStats.Add(EquipPart.Top, new Dictionary<KeyValuePair<int, Rarity>, int[]>());
        equipStats.Add(EquipPart.Pants, new Dictionary<KeyValuePair<int, Rarity>, int[]>());
        equipStats.Add(EquipPart.Gloves, new Dictionary<KeyValuePair<int, Rarity>, int[]>());
        equipStats.Add(EquipPart.Shoes, new Dictionary<KeyValuePair<int, Rarity>, int[]>());
        equipStats.Add(EquipPart.Necklace, new Dictionary<KeyValuePair<int, Rarity>, int[]>());
        equipStats.Add(EquipPart.Ring, new Dictionary<KeyValuePair<int, Rarity>, int[]>());

        for (int lvl = 1; lvl <= 9; lvl += 2)
        {
            Rarity maxR = (Rarity)Mathf.Min(lvl + 1, 5);

            for (Rarity r = Rarity.Common; r <= maxR; r++)
            {
                equipStats[EquipPart.Weapon].Add(new KeyValuePair<int, Rarity>(lvl, r), new int[6]);

                equipStats[EquipPart.Top].Add(new KeyValuePair<int, Rarity>(lvl, r), new int[2]);
                equipStats[EquipPart.Pants].Add(new KeyValuePair<int, Rarity>(lvl, r), new int[2]);
                equipStats[EquipPart.Gloves].Add(new KeyValuePair<int, Rarity>(lvl, r), new int[2]);
                equipStats[EquipPart.Shoes].Add(new KeyValuePair<int, Rarity>(lvl, r), new int[2]);

                equipStats[EquipPart.Ring].Add(new KeyValuePair<int, Rarity>(lvl, r), new int[4]);
                equipStats[EquipPart.Necklace].Add(new KeyValuePair<int, Rarity>(lvl, r), new int[4]);
            }
        }

        jsonTxt = Resources.Load<TextAsset>("Jsons/Items/WeaponStat");
        loadStr = jsonTxt.text;
        wepJson = JsonMapper.ToObject(loadStr);

        jsonTxt = Resources.Load<TextAsset>("Jsons/Items/ArmorStat");
        loadStr = jsonTxt.text;
        amrJson = JsonMapper.ToObject(loadStr);

        jsonTxt = Resources.Load<TextAsset>("Jsons/Items/AccessoryStat");
        loadStr = jsonTxt.text;
        acceJson = JsonMapper.ToObject(loadStr);

        for (int lvl = 1; lvl <= 9; lvl += 2)
        {
            Rarity maxR = (Rarity)Mathf.Min(lvl + 1, 5);

            for (Rarity r = Rarity.Common; r <= maxR; r++)
            {
                for (int i = 0; i < 6; i++)
                    equipStats[EquipPart.Weapon][new KeyValuePair<int, Rarity>(lvl, r)][i] = (int)wepJson[(lvl - 1) / 2 * 5 + (int)(r - Rarity.Common)]["stat"][i];

                for (int i = 0; i < 2; i++)
                {
                    equipStats[EquipPart.Top][new KeyValuePair<int, Rarity>(lvl, r)][i] = (int)amrJson[(lvl - 1) / 2 * 5 + (int)(r - Rarity.Common)]["stat"][i];
                    equipStats[EquipPart.Pants][new KeyValuePair<int, Rarity>(lvl, r)][i] = (int)amrJson[25 + (lvl - 1) / 2 * 5 + (int)(r - Rarity.Common)]["stat"][i];
                    equipStats[EquipPart.Gloves][new KeyValuePair<int, Rarity>(lvl, r)][i] = (int)amrJson[50 + (lvl - 1) / 2 * 5 + (int)(r - Rarity.Common)]["stat"][i];
                    equipStats[EquipPart.Shoes][new KeyValuePair<int, Rarity>(lvl, r)][i] = (int)amrJson[75 + (lvl - 1) / 2 * 5 + (int)(r - Rarity.Common)]["stat"][i];
                }

                for (int i = 0; i < 4; i++)
                {
                    equipStats[EquipPart.Ring][new KeyValuePair<int, Rarity>(lvl, r)][i] = (int)acceJson[(lvl - 1) / 2 * 5 + (int)(r - Rarity.Common)]["stat"][i];
                    equipStats[EquipPart.Necklace][new KeyValuePair<int, Rarity>(lvl, r)][i] = (int)acceJson[25 + (lvl - 1) / 2 * 5 + (int)(r - Rarity.Common)]["stat"][i];
                }
            }
        }
    }


    public Equipment(EquipBluePrint ebp)
    {
        this.ebp = ebp;
        star = 1;

        SetMainStat();
        SetSubStat();
        SetCommonStat();

        StatValueSet();
    }

    public int CompareTo(Equipment e)
    {
        int ret = ebp.idx.CompareTo(ebp.idx);
        if (ret == 0)
            return star.CompareTo(star);

        return ret;
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
                    mainStat = ringObjs[Random.Range(0, 4)];
                    break;
                case EquipPart.Necklace:
                    mainStat = neckObjs[Random.Range(0, 4)];
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
                        do
                            subStat = weaponObjs[Random.Range(0, 6)];
                        while (subStat == Obj.ATK);
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
                            subStat = ringObjs[Random.Range(0, 4)];
                        while (mainStat == subStat);
                        break;
                    case EquipPart.Necklace:
                        do

                            subStat = neckObjs[Random.Range(0, 4)];
                        while (mainStat == subStat);
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
    void SetCommonStat()
    {
        commonStatValue.Clear();
        if (ebp.rarity < Rarity.Rare)
        {
            return;
        }

        int[] objs = new int[ebp.equipOption.Count(x => x > 0)];
        ResetCommonStat();

        for (int i = 0; i < objs.Length; i++)
            commonStatValue.Add(new KeyValuePair<Obj, int>((Obj)objs[i], allCommonStats[ebp.reqlvl][objs[i]]));

        void ResetCommonStat()
        {
            if (ebp.equipOption[0] != 13)
                objs[0] = ebp.equipOption[0];
            else
            {
                do
                    objs[0] = Random.Range(1, 13);
                while (objs[0] == 1 || objs[0] == 3);
            }

            if (objs.Length >= 2)
                if (ebp.equipOption[1] != 13)
                    objs[1] = ebp.equipOption[1];
                else
                {
                    do
                        objs[1] = Random.Range(1, 13);
                    while (objs[1] == 1 || objs[1] == 3 || objs[1] == objs[0]);
                }

            if(objs.Length >= 3)
                if (ebp.equipOption[2] != 13)
                    objs[2] = ebp.equipOption[2];
                else
                {
                    do
                        objs[2] = Random.Range(1, 13);
                    while (objs[2] == 1 || objs[2] == 3 || objs[2] == objs[0] || objs[2] == objs[1]);
                }
        }
    }


    void StatValueSet()
    {
        if (EquipPart.Top <= ebp.part && ebp.part <= EquipPart.Shoes)
        {
            mainStatValue = star * equipStats[ebp.part][new KeyValuePair<int, Rarity>(ebp.reqlvl, ebp.rarity)][0];
            if (subStat != Obj.None) subStatValue = star * equipStats[ebp.part][new KeyValuePair<int, Rarity>(ebp.reqlvl, ebp.rarity)][1];
        }
        else
        {
            mainStatValue = star *equipStats[ebp.part][new KeyValuePair<int, Rarity>(ebp.reqlvl, ebp.rarity)][statIdx[new KeyValuePair<EquipPart, Obj>(ebp.part, mainStat)]];
            if (subStat != Obj.None) subStatValue = star * equipStats[ebp.part][new KeyValuePair<int, Rarity>(ebp.reqlvl, ebp.rarity)][statIdx[new KeyValuePair<EquipPart, Obj>(ebp.part, subStat)]];
        }
    }

    public bool CanSwitchCommonStat() => ebp.rarity >= Rarity.Rare;
    public void SwitchCommonStat()
    {
        if (!CanSwitchCommonStat())
            return;

        SetCommonStat();
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
    public int idx;
    public string name;
    public int count;
    public float[] rate;

    static JsonData json;
    static int startIdx;

    static Potion()
    {
        TextAsset jsonTxt = Resources.Load<TextAsset>("Jsons/Items/Potion");
        json = JsonMapper.ToObject(jsonTxt.text);
        startIdx = (int)json[0]["idx"];
    }
    public Potion(){}
    public Potion(int idx)
    {
        this.idx = idx;
        name = json[idx - startIdx]["name"].ToString();
        count = (int)json[idx - startIdx]["count"];
        rate = new float[count];
        for(int i = 0;i < count;i++)
            rate[i] = float.Parse(json[idx - startIdx]["rate"][i].ToString());
    }
}