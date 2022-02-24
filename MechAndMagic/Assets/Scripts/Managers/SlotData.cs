﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum SceneKind
{
    Town, Dungeon, Event, Outbreak, Battle
}

public class SlotData
{
    public static readonly int[] baseStats = new int[13];
    public static readonly int[] reqExp = new int[9];

    public int slotClass;

    public int lvl;
    public int exp;
    public int[] itemStats = new int[13];

    public int[] activeSkills = new int[6];
    public int[] passiveSkills = new int[4];
    public int[] potionSlot = new int[2];

    public SceneKind nowScene;
    
    public int dungeonIdx;
    public DungeonState dungeonState;

    static SlotData()
    {
        baseStats[0] = 1;
        baseStats[1] = baseStats[2] = 15;
        baseStats[3] = baseStats[4] = 6;
        baseStats[5] = 5;
        baseStats[7] = 70;

        baseStats[10] = 150;
        baseStats[12] = 5;

        reqExp[0] = 100;
        reqExp[1] = 200;
        reqExp[2] = 400;
        reqExp[3] = 600;
        reqExp[4] = 900;
        reqExp[5] = 1200;
        reqExp[6] = 1600;
        reqExp[7] = 2000;
        reqExp[8] = 2500;
    }
    public SlotData() { }

    public SlotData(int classIdx)
    {
        lvl = 1;
        slotClass = classIdx;

        for (int i = 0; i <= 12; i++)
            itemStats[i] = baseStats[i];
        activeSkills[0] = SkillManager.GetSkillData(classIdx)[0].idx;

        nowScene = SceneKind.Town;
    }
}
public class DungeonState
{
    public double scroll;

    public int currRoomEvent;
    public int[] currPos;
    public Dungeon currDungeon;

    public int currHP;
    public int golemHP;

    public int druidRevive;
    public bool[] potionUse = new bool[2];

    public List<KeyValuePair<int, Buff>> dungeonBuffs = new List<KeyValuePair<int, Buff>>();
    public List<KeyValuePair<int, Buff>> dungeonDebuffs = new List<KeyValuePair<int, Buff>>();

    public DungeonState() {}
    public DungeonState(DungeonBluePrint dbp)
    {
        currDungeon = new Dungeon();
        currDungeon.DungeonInstantiate(dbp);
        scroll = 0;

        currHP = -1; druidRevive = 0; golemHP = GameManager.slotData.slotClass == 4 ? 0 : -1;
        potionUse[0] = potionUse[1] = false;
        currPos = new int[2] {0, 0};
    }

    public Room GetCurrRoom()
    {
        return currDungeon.GetRoom(currPos[0], currPos[1]);
    }
}

public class ItemData
{
    //1~3 : 스킬 재화, 4~12 : 아이템 특수 재화(상중하 우선, 무기,방어구,장신구)
    //13~15 : 아이템 공통 재화
    public int[] basicMaterials;
    public int[] equipRecipes;

    public Skillbook[] skillbooks;
    public int[] potions;

    int startIdx;
    public bool[] skillLearned;

    public List<Equipment> weapons;
    public List<Equipment> armors;
    public List<Equipment> accessories;
    int Compare(Equipment e1, Equipment e2) {
        int ret = e1.ebp.idx.CompareTo(e2.ebp.idx);
        if (ret == 0)
            return e2.star.CompareTo(e1.star);
        else
            return ret;
    }
    //0무기, 1상, 2하, 3장, 4신, 5반, 6목
    public Equipment[] equipmentSlots = new Equipment[8];


    public bool CanSmith(EquipBluePrint ebp)
    {
        for (int i = 0; i < ebp.requireResources.Count; i++)
            if (basicMaterials[ebp.requireResources[i].Key] < ebp.requireResources[i].Value)
                return false;
        return equipRecipes[ebp.idx] > 0;
    }
    public bool CanSwitchCommonStat(EquipPart part, int idx)
    {
        Equipment tmp = null;
        if (part <= EquipPart.Weapon)
            tmp = weapons[idx];
        else if (part <= EquipPart.Shoes)
            tmp = armors[idx];
        else if (part <= EquipPart.Ring)
            tmp = accessories[idx];

        return tmp.CanSwitchCommonStat();
    }
    public bool CanFusion(EquipPart part, int idx)
    {
        Equipment tmp = null;
        int stuff = -1;
        List<Equipment> eList = weapons;
        if (part <= EquipPart.Weapon)
            tmp = weapons[idx];
        else if (part <= EquipPart.Shoes)
        { tmp = armors[idx]; eList = armors; }
        else if (part <= EquipPart.Ring)
        { tmp = accessories[idx]; eList = accessories; }

        for (int i = 0; i < eList.Count; i++)
            if (i != idx && eList[i].ebp.idx == tmp.ebp.idx && eList[i].star == tmp.star)
            {
                stuff = i;
                break;
            }

        return tmp.star < 3 && stuff > 0;
    }

    public void Smith(EquipBluePrint ebp)
    {
        for (int i = 0; i < ebp.requireResources.Count; i++)
            basicMaterials[ebp.requireResources[i].Key] -= ebp.requireResources[i].Value;
        EquipDrop(ebp);
    }
    public void Disassemble(EquipPart part, int idx)
    {
        switch (part)
        {
            case EquipPart.Weapon:
                GetResource(weapons[idx].ebp);
                weapons.RemoveAt(idx);
                break;
            case EquipPart.Top:
            case EquipPart.Pants:
            case EquipPart.Gloves:
            case EquipPart.Shoes:
                GetResource(armors[idx].ebp);
                armors.RemoveAt(idx);
                break;
            case EquipPart.Ring:
            case EquipPart.Necklace:
                GetResource(accessories[idx].ebp);
                accessories.RemoveAt(idx);
                break;
        }

        void GetResource(EquipBluePrint ebp)
        {
            for (int i = 0; i < ebp.requireResources.Count; i++)
                basicMaterials[ebp.requireResources[i].Key] += Mathf.RoundToInt(0.4f * ebp.requireResources[i].Value);
        }
    }
    public void SwitchCommonStat(EquipPart part, int idx)
    {
        Equipment tmp = weapons[idx];
        switch(part)
        {
            case EquipPart.Top:
            case EquipPart.Pants:
            case EquipPart.Gloves:
            case EquipPart.Shoes:
                tmp = armors[idx];
                break;
            case EquipPart.Ring:
            case EquipPart.Necklace:
                tmp = accessories[idx];
                break;
        }

        tmp.SwitchCommonStat();
    }
    public void Fusion(EquipPart part, int idx)
    {
        Equipment tmp = weapons[idx];
        int stuff = -1;
        List<Equipment> eList = weapons;
        switch (part)
        {
            case EquipPart.Top:
            case EquipPart.Pants:
            case EquipPart.Gloves:
            case EquipPart.Shoes:
                tmp = armors[idx];
                eList = armors;
                break;
            case EquipPart.Ring:
            case EquipPart.Necklace:
                tmp = accessories[idx];
                eList = accessories;
                break;
        }

        for (int i = 0; i < eList.Count; i++)
            if (i != idx && eList[i].ebp.idx == tmp.ebp.idx && eList[i].star == tmp.star)
            {
                stuff = i;
                break;
            }

        if (stuff > 0)
        {
            tmp.Fusion();
            eList.RemoveAt(stuff);
            eList.Sort(Compare);
        }
        else
            Debug.Log("there is no stuff");
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
                accessories.Add(tmp);
                accessories.Sort(Compare);
                break;
        }
    }

    public void Equip(EquipPart part, int idx)
    {
        bool sort = false;
        List<Equipment> eList = weapons;
        switch (part)
        {
            case EquipPart.Top:
            case EquipPart.Pants:
            case EquipPart.Gloves:
            case EquipPart.Shoes:
                eList = armors;
                break;
            case EquipPart.Ring:
            case EquipPart.Necklace:
                eList = accessories;
                break;
        }

        if (equipmentSlots[(int)part - 1] != null)
        {
            sort = true;
            eList.Add(equipmentSlots[(int)part - 1]);
            equipmentSlots[(int)part - 1] = null;
        }

        equipmentSlots[(int)part - 1] = eList[idx];
        eList.RemoveAt(idx);

        if (sort) eList.Sort(Compare);
    }
    public void UnEquip(EquipPart part)
    {
        if(equipmentSlots[(int)part - 1] != null)
        {
            switch(part)
            {
                case EquipPart.Weapon:
                    weapons.Add(equipmentSlots[(int)part - 1]);
                    weapons.Sort(Compare);
                    break;
                case EquipPart.Top:
                case EquipPart.Pants:
                case EquipPart.Gloves:
                case EquipPart.Shoes:
                    armors.Add(equipmentSlots[(int)part - 1]);
                    armors.Sort(Compare);
                    break;
                case EquipPart.Ring:
                case EquipPart.Necklace:
                    accessories.Add(equipmentSlots[(int)part - 1]);
                    accessories.Sort(Compare);
                    break;
            }
            equipmentSlots[(int)part - 1] = null;
        }
    }


    public void SkillLearn(int idx)
    {
        skillLearned[idx - startIdx] = true;
    }
    public bool IsLearned(int idx)
    {
        if (idx < startIdx)
            return true;
        return skillLearned[idx - startIdx];
    }

    public ItemData()
    {
        basicMaterials = new int[16];
        equipRecipes = new int[ItemManager.EQUIP_COUNT + 1];

        Skill[] s = SkillManager.GetSkillData(GameManager.slotData.slotClass);
        skillbooks = new Skillbook[s.Length];

        startIdx = s[0].idx;
        skillLearned = new bool[s.Length];
        for (int i = 0; i < s.Length; i++)
        {
            skillbooks[i] = new Skillbook();
            skillbooks[i].idx = s[i].idx;
            skillbooks[i].lvl = s[i].reqLvl;
            skillbooks[i].type = s[i].useType;
            skillbooks[i].count = 0;
        }

        potions = new int[18];

        skillLearned[0] = skillLearned[1] = true;

        weapons = new List<Equipment>();
        armors = new List<Equipment>();
        accessories = new List<Equipment>();
    }
}

public class SkillData
{
    public int currClass;
    public bool[] learned;

    public SkillData() { }
    public SkillData(int c)
    {
        currClass = c;
        learned = new bool[SkillManager.GetSkillData(c).Length];
        learned[0] = true;
    }

    public void Learn(int idx)
    {
        learned[idx] = true;
    }
}