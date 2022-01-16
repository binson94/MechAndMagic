using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using LitJson;


public class ItemManager : MonoBehaviour
{
    static ItemData itemData;

    public const int EQUIP_COUNT = 19;
    
    //모든 장비 정보
    static EquipBluePrint[] bluePrints = new EquipBluePrint[EQUIP_COUNT];

    private void Awake()
    {
        for (int i = 0; i < EQUIP_COUNT; i++)
            bluePrints[i] = new EquipBluePrint(i);
    }

    #region ItemDrop
    public static void ItemDrop(int classIdx, int category, float prob)
    {
        if(prob >= 1f)
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
            //경험치
            else
                Debug.Log("Exp");
        }
    }
    static void NewPotion(int category)
    {

    }
    static void NewEquip(int classIdx, int category)
    {
        int region = (classIdx < 5) ? 10 : 11;
        Debug.Log(string.Concat(classIdx, " ", category, " ", region));
        var possibleList = (from token in bluePrints
                            where (token.category == category) && (token.useClass == classIdx || token.useClass == region || token.useClass == 0)
                            select token);

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
        return itemData.CanSwitchOption(part, idx);
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
        itemData.SwitchOption(part, idx);
        SaveData();
    }
    public static void FusionEquipment(EquipPart part, int idx)
    {
        itemData.Fusion(part, idx);
        SaveData();
    }

    public static void Equip(EquipPart part, int idx) 
    {
        itemData.Equip(part, idx);
        ItemStatUpdate();
        SaveData();
    }
    public static void UnEquip(EquipPart part)
    {
        itemData.UnEquip(part);
        ItemStatUpdate();
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
            }

        for(int i = 1;i<13;i++)
        {
            GameManager.slotData.itemStats[i] = SlotData.baseStats[i] + addPivots[i];
        }
        GameManager.slotData.itemStats[1] = GameManager.slotData.itemStats[2];
        GameManager.slotData.itemStats[3] = GameManager.slotData.itemStats[4];
        GameManager.SaveSlotData();
    }

    public static void SkillLearn(int idx)
    {
        itemData.SkillLearn(idx);
        SaveData();
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
    #endregion

    public static void LoadData()
    {
        if (PlayerPrefs.HasKey(string.Concat("Item", GameManager.currSlot)))
        {
            itemData = JsonMapper.ToObject<ItemData>(PlayerPrefs.GetString(string.Concat("Item", GameManager.currSlot)));
            foreach (Equipment e in itemData.weapons)
                e.ebp.name = bluePrints[e.ebp.idx].name;
            foreach (Equipment e in itemData.armors)
                e.ebp.name = bluePrints[e.ebp.idx].name;
            foreach (Equipment e in itemData.accessorys)
                e.ebp.name = bluePrints[e.ebp.idx].name;
        }
        else
            itemData = new ItemData();
    }

    static void SaveData()
    {
        PlayerPrefs.SetString(string.Concat("Item", GameManager.currSlot), JsonMapper.ToJson(itemData));
    }
}
