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

    private void Start()
    {
        //debug
        LoadData();
        ItemDrop(1, 84, 1);
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
        List<EquipBluePrint> possibleList = (from token in bluePrints
                                             where (token.category == category) && (token.useClass == classIdx)
                                             select token).ToList();
        EquipBluePrint ebp = possibleList.Skip(Random.Range(0, possibleList.Count)).Take(1).First();

        itemData.EquipDrop(ebp);
    }

    static void NewSkillBook(int category)
    {
        int lvl = 47 - 2 * category;

        var possibleList = (from token in itemData.skillbooks
                            where (lvl == 0 || lvl == token.lvl)
                            select token);

       possibleList.Skip(Random.Range(0, possibleList.Count())).Take(1).First().count += 1;

        SaveData();
    }

    static void NewEquipRecipe(int classIdx, int category)
    {
        category -= 63;
        var possibleList = (from token in bluePrints
                            where token.useClass == classIdx && token.category == category
                            select token);

        int idx = possibleList.Skip(Random.Range(0, possibleList.Count())).Take(1).First().idx;

        itemData.equipRecipes[idx] += 1;

        SaveData();
    }
    #endregion

    #region Smith
    static public void SmithEquipment(int idx)
    {
        itemData.Smith(bluePrints[idx]);
        SaveData();
    }
    static public void DisassembleEquipment(EquipPart part, int idx)
    {
        itemData.Disassemble(part, idx);
        SaveData();
    }

    static public void Equip(ItemCategory category, int idx) 
    {
        itemData.Equip(category, idx);
        SaveData();
    }
    static public void UnEquip(EquipPart part)
    {
        itemData.UnEquip(part);
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
    public static int[] GetResourceData(ItemCategory category)
    {
        if (category == ItemCategory.Recipe)
            return itemData.equipRecipes;
        else
            return itemData.basicMaterials;
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
