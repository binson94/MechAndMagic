using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class ItemData
{
    //1~3 : 스킬 재화, 4~12 : 아이템 특수 재화(상중하 우선, 무기,방어구,장신구)
    //13~15 : 아이템 공통 재화
    public int[] basicMaterials;
    public int[] equipRecipes;
    public Skillbook[] skillbooks;

    public List<Equipment> weapons;
    public List<Equipment> armors;
    public List<Equipment> accessorys;
    public List<Potion> potions;
    int Compare(Equipment e1, Equipment e2)
    {
        if (e1.ebp.idx > e2.ebp.idx)
            return 1;
        else if (e1.ebp.idx < e2.ebp.idx)
            return -1;
        else
            return 0;
    }

    //0무기, 1상, 2하, 3장, 4신, 5반, 6목
    public Equipment[] equipmentSlots = new Equipment[8];


    public bool CanSmith(int idx)
    {
        return equipRecipes[idx] > 0;
    }
    public void Smith(EquipBluePrint ebp)
    {
        EquipDrop(ebp);
    }
    public void EquipDrop(EquipBluePrint ebp)
    {
        Equipment tmp = new Equipment(ebp);

        switch (ebp.part)
        {
            case EquipPart.Weapon:
                weapons.Add(tmp);
                weapons.Sort(Compare);
                break;
            case EquipPart.Top:
            case EquipPart.Pants:
            case EquipPart.Gloves:
            case EquipPart.Shoes:
                armors.Add(tmp);
                armors.Sort(Compare);
                break;
            case EquipPart.Ring:
            case EquipPart.Necklace:
                accessorys.Add(tmp);
                accessorys.Sort(Compare);
                break;
        }
    }
    public void Disassemble(EquipPart part, int idx)
    {
        Rarity rarity = Rarity.None;
        switch (part)
        {
            case EquipPart.Weapon:
                rarity = weapons[idx].ebp.rarity;
                weapons.RemoveAt(idx);
                break;
            case EquipPart.Top:
            case EquipPart.Pants:
            case EquipPart.Gloves:
            case EquipPart.Shoes:
                rarity = armors[idx].ebp.rarity;
                armors.RemoveAt(idx);
                break;
            case EquipPart.Ring:
            case EquipPart.Necklace:
                rarity = armors[idx].ebp.rarity;
                armors.RemoveAt(idx);
                break;
        }
    }
    public void Equip(ItemCategory category, int idx)
    {
        EquipPart part = EquipPart.Weapon;
        switch (category)
        {
            case ItemCategory.Armor:
                part = armors[idx].ebp.part;
                break;
            case ItemCategory.Accessory:
                part = accessorys[idx].ebp.part;
                break;
        }

        if(equipmentSlots[(int)part] != null)
            UnEquip(part);

        Equipment tmp;
        switch(part)
        {
            case EquipPart.Weapon:
                tmp = weapons[idx];
                weapons.RemoveAt(idx);
                break;
            case EquipPart.Top:
            case EquipPart.Pants:
            case EquipPart.Gloves:
            case EquipPart.Shoes:
                tmp = armors[idx];
                armors.RemoveAt(idx);
                break;
            case EquipPart.Ring:
            case EquipPart.Necklace:
                tmp = accessorys[idx];
                accessorys.RemoveAt(idx);
                break;
            default:
                tmp = null;
                break;
        }

        equipmentSlots[(int)part] = tmp;
    }
    public void UnEquip(EquipPart part)
    {
        if(equipmentSlots[(int)part] != null)
        {
            switch(part)
            {
                case EquipPart.Weapon:
                    weapons.Add(equipmentSlots[(int)part]);
                    weapons.Sort(Compare);
                    break;
                case EquipPart.Top:
                case EquipPart.Pants:
                case EquipPart.Gloves:
                case EquipPart.Shoes:
                    armors.Add(equipmentSlots[(int)part]);
                    armors.Sort(Compare);
                    break;
                case EquipPart.Ring:
                case EquipPart.Necklace:
                    accessorys.Add(equipmentSlots[(int)part]);
                    accessorys.Sort(Compare);
                    break;
            }
            equipmentSlots[(int)part] = null;
        }
    }

    public ItemData()
    {
        basicMaterials = new int[16];
        equipRecipes = new int[ItemManager.EQUIP_COUNT];

        Skill[] s = SkillManager.GetSkillData();
        skillbooks = new Skillbook[s.Length];
        for (int i = 0; i < s.Length; i++)
        {
            skillbooks[i] = new Skillbook();
            skillbooks[i].idx = s[i].idx;
            skillbooks[i].lvl = s[i].reqLvl;
            skillbooks[i].type = s[i].useType;
            skillbooks[i].count = 0;
        }

        weapons = new List<Equipment>();
        armors = new List<Equipment>();
        accessorys = new List<Equipment>();
        potions = new List<Potion>();
    }
}
