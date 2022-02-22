using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using LitJson;


public class ItemManager : MonoBehaviour
{
    static ItemData itemData;
    static SetOptionManager setManager;

    public const int EQUIP_COUNT = 567;
    
    //모든 장비 정보
    static EquipBluePrint[] bluePrints = new EquipBluePrint[EQUIP_COUNT];
    static Potion[] potions = new Potion[18];

    private void Awake()
    {
        for (int i = 0; i < EQUIP_COUNT; i++)
            bluePrints[i] = new EquipBluePrint(i);
        for(int i = 0; i < 18;i++)
            potions[i] = new Potion(i + 1);
        
        setManager = new SetOptionManager();
    }

    #region ItemDrop
    public static void ItemDrop(int classIdx, int category, float prob)
    {
        if (prob >= 1f)
        {
            for (float i = 0; i < prob; i += 1f)
                AddItem();
        }
        else if (Random.Range(0, 1f) < prob)
        {
            AddItem();
        }

        SaveData();

        void AddItem()
        {
            //기본 재료
            if (category <= 15)
                itemData.basicMaterials[category]++;
            //포션
            else if (category <= 18)
                NewPotion(category);
            //스킬북
            else if (category <= 23)
                NewSkillBook(category);
            //장비
            else if (category <= 86)
                NewEquip(classIdx, category);
            //제작법
            else if (category <= 149)
                NewEquipRecipe(classIdx, category);
        }
    }
    static void NewPotion(int idx) => itemData.potions[idx] = 1;
    static void NewEquip(int classIdx, int category)
    {
        int region = (classIdx < 5) ? 10 : 11;
        Debug.Log(string.Concat(classIdx, " ", category, " ", region));
        var possibleList = (from token in bluePrints
                            where (token.category == category) && (token.useClass == classIdx || token.useClass == region || token.useClass == 0)
                            select token);

        foreach(EquipBluePrint e in possibleList)
            Debug.Log(string.Concat(e.name, " ", e.category, " ", e.useClass));

        if (possibleList.Count() <= 0)
            return;

        EquipBluePrint ebp = possibleList.Skip(Random.Range(0, possibleList.Count())).Take(1).First();

        itemData.EquipDrop(ebp);
    }
    static void NewSkillBook(int category)
    {
        int lvl = 47 - 2 * category;

        var possibleList = (from token in itemData.skillbooks
                            where (lvl == 0 || lvl == token.lvl)
                            select token);

        if (possibleList.Count() <= 0)
            return;

        possibleList.Skip(Random.Range(0, possibleList.Count())).Take(1).First().count += 1;

        SaveData();
    }
    static void NewEquipRecipe(int classIdx, int category)
    {
        category -= 63;
        int region = (classIdx < 5) ? 10 : 11;
        var possibleList = (from token in bluePrints
                            where (token.category == category) && (token.useClass == classIdx || token.useClass == region || token.useClass == 0)
                            select token);

        if (possibleList.Count() <= 0)
            return;

        int idx = possibleList.Skip(Random.Range(0, possibleList.Count())).Take(1).First().idx;

        itemData.equipRecipes[idx] += 1;

        SaveData();
    }
    #endregion

    #region Smith
    public static bool CanSmith(int idx)
    {
        return itemData.CanSmith(bluePrints[idx]);
    }
    public static bool CanSwitchOption(EquipPart part, int idx)
    {
        return itemData.CanSwitchCommonStat(part, idx);
    }
    public static bool CanFusion(EquipPart part, int idx)
    {
        return itemData.CanFusion(part, idx);
    }

    public static void SmithEquipment(int idx)
    {
        itemData.Smith(bluePrints[idx]);
        SaveData();
    }
    public static void DisassembleEquipment(EquipPart part, int idx)
    {
        itemData.Disassemble(part, idx);
        SaveData();
    }
    public static void SwitchEquipOption(EquipPart part, int idx)
    {
        itemData.SwitchCommonStat(part, idx);
        SaveData();
    }
    public static void FusionEquipment(EquipPart part, int idx)
    {
        itemData.Fusion(part, idx);
        SaveData();
    }

    public static void SkillLearn(int idx)
    {
        itemData.SkillLearn(idx);
        SaveData();
    }
    #endregion

    #region Equip
    public static void Equip(EquipPart part, int idx) 
    {
        itemData.Equip(part, idx);
        ItemStatUpdate();
        setManager.SetComfirm(itemData);
        SaveData();
    }
    public static void UnEquip(EquipPart part)
    {
        itemData.UnEquip(part);
        ItemStatUpdate();
        setManager.SetComfirm(itemData);
        SaveData();
    }
    static void ItemStatUpdate()
    {
        int[] addPivots = new int[13];
        foreach (Equipment e in itemData.equipmentSlots)
            if (e != null)
            {
                addPivots[(int)e.mainStat] += e.mainStatValue;
                addPivots[(int)e.subStat] += e.subStatValue;

                for(int i = 0;i < e.commonStatValue.Count;i++)
                    addPivots[(int)e.commonStatValue[i].Key] += e.commonStatValue[i].Value;
            }

        for(int i = 1;i<13;i++)
        {
            GameManager.slotData.itemStats[i] = SlotData.baseStats[i] + addPivots[i];
        }
        GameManager.slotData.itemStats[1] = GameManager.slotData.itemStats[2];
        GameManager.slotData.itemStats[3] = GameManager.slotData.itemStats[4];
        GameManager.SaveSlotData();
    }
    
    public static void EquipPotion(int slot, int idx)
    {
        GameManager.slotData.potionSlot[slot] = idx;
        if(GameManager.slotData.potionSlot[(slot + 1) % 2] == idx)
            GameManager.slotData.potionSlot[(slot + 1) % 2] = 0;
    }
    #endregion

    #region Show
    //SmithManager에게 보유한 장비, 스킬북, 자원 정보 주는 함수
    public static List<Equipment> GetEquipData(ItemCategory category, Rarity rarity, int lvl)
    {
        List<Equipment> tmp;
        switch(category)
        {
            case ItemCategory.Weapon:
                tmp = itemData.weapons;
                break;
            case ItemCategory.Armor:
                tmp = itemData.armors;
                break;
            case ItemCategory.Accessory:
                tmp = itemData.accessorys;
                break;
            default:
                return null;
        }
        return (from x in tmp
                where (rarity == Rarity.None || (x.ebp.rarity == rarity)) && (lvl == 0 || (x.ebp.reqlvl == lvl))
                select x).ToList();
    }
    public static List<Skillbook> GetSkillbookData(int skillType, int lvl)
    {
        return (from x in itemData.skillbooks
                where ((x.count > 0) && (skillType == -1 || x.type == skillType) && (lvl == 0 || x.lvl == lvl))
                select x).ToList();
    }
    public static List<EquipBluePrint> GetRecipeData(Rarity rarity, int lvl)
    {
        int region = (GameManager.slotData.slotClass < 5) ? 10 : 11;
        List<EquipBluePrint> ebps = new List<EquipBluePrint>();

        for (int i = 0; i < bluePrints.Length; i++)
        {
            if ((itemData.equipRecipes[i] > 0) &&
                (bluePrints[i].useClass == 0 || bluePrints[i].useClass == GameManager.slotData.slotClass || bluePrints[i].useClass == region) &&
                (rarity == Rarity.None || bluePrints[i].rarity == rarity) &&
                (lvl == 0 || bluePrints[i].reqlvl == lvl))
                ebps.Add(bluePrints[i]);
        }

        return ebps;
    }
    public static int[] GetResourceData(ItemCategory category)
    {
        if (category == ItemCategory.Recipe)
            return itemData.equipRecipes;
        else
            return itemData.basicMaterials;
    }

    public static Equipment GetEquipment(EquipPart p)
    {
        return itemData.equipmentSlots[(int)p - 1];
    }
    public static int[] GetSkillbookData()
    {
        return (from a in itemData.skillbooks
               select a.count).ToArray();
    }
    public static bool IsLearned(int idx)
    {
        return itemData.IsLearned(idx);
    }
    
    public static KeyValuePair<string, float[]> GetSetData(int set) => setManager.GetSetData(set);
    public static Potion GetPotion(int potionIdx) => potions[Mathf.Max(0, potionIdx - 1)];
    #endregion

    public static void LoadData()
    {
        if (PlayerPrefs.HasKey(string.Concat("Item", GameManager.currSlot)))
        {
            itemData = GameManager.HexToObj<ItemData>(PlayerPrefs.GetString(string.Concat("Item", GameManager.currSlot)));

            foreach(Equipment e in itemData.weapons)
                Debug.Log(e.ebp.name);

            setManager.SetComfirm(itemData);
        }
        else
            itemData = new ItemData();
    }
    static void SaveData()
    {
        PlayerPrefs.SetString(string.Concat("Item", GameManager.currSlot), GameManager.ObjToHex(itemData));
    }
}

public class SetOptionManager
{

    Dictionary<int, int> setList =new Dictionary<int, int>();
    SetOption[] options;
    public SetOptionManager()
    {
        TextAsset jsonTxt = Resources.Load<TextAsset>("Jsons/Items/SetOption");
        JsonData json = JsonMapper.ToObject(jsonTxt.text);

        options = new SetOption[json.Count];

        for (int i = 0; i < options.Length; i++)
        {
            options[i] = new SetOption();

            options[i].name = json[i]["name"].ToString();
            options[i].setIdx = (int)json[i]["set"];
            options[i].count = (int)json[i]["count"];

            options[i].rate = new float[options[i].count];
            options[i].reqPart = new int[options[i].count];
            for (int j = 0; j < options[i].count; j++)
            {
                options[i].reqPart[j] = (int)json[i]["reqPart"][j];
                options[i].rate[j] = float.Parse(json[i]["rate"][j].ToString());
            }
        }
    }
    public void SetComfirm(ItemData itemData)
    {
        setList.Clear();
        Dictionary<int, int> count = new Dictionary<int, int>();

        for (int i = 0; i < itemData.equipmentSlots.Length; i++)
        {
            if (itemData.equipmentSlots[i] != null)
            {
                int set = itemData.equipmentSlots[i].ebp.set;
                if (set != 0)
                    if (count.ContainsKey(set))
                        count[set]++;
                    else
                        count.Add(set, 1);
            }
        }

        foreach (KeyValuePair<int, int> token in count)
            if (token.Value >= 2)
                setList.Add(token.Key, token.Value);
    }
    public KeyValuePair<string, float[]> GetSetData(int set)
    {
        set--;
        float[] tmp = new float[options[set].count];

        if (setList.ContainsKey(set))
            for (int i = 0; i < options[set].count; i++)
                tmp[i] = setList[set] >= options[set].reqPart[i] ? options[set].rate[i] : 0;

        return new KeyValuePair<string, float[]>(options[set].name, tmp);
    }

    class SetOption
    {
        public string name;
        public int setIdx;
        public int count;
        public int[] reqPart;
        public float[] rate;
    }
}