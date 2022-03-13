using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum SceneKind
{
    Town, Dungeon, Event, Outbreak, Battle
}
public enum DropType
{
    Material, Equip, Skillbook, Recipe, EXP
}
public class Triplet<T1, T2, T3>
{
    public T1 first;
    public T2 second;
    public T3 third;

    public Triplet() {}
    public Triplet(T1 a, T2 b, T3 c)
    {
        first = a; second = b; third = c;
    }
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
    public ItemData itemData;
    public QuestSlot questData;

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

        itemData = new ItemData(slotClass);
        questData = new QuestSlot(slotClass);
    }
    public void DropSave(DropType type, int idx, int amt = 1)
    {
        if(dungeonState == null)
            return;
            
        if(dungeonState.dropList.Count <= 0)
        {
            dungeonState.dropList.Add(new Triplet<DropType, int, int>(type, idx, amt));
        }
        else
        {
            int left = 0, right = dungeonState.dropList.Count - 1;

            int middle = (left + right) / 2;
            while(left <= middle && middle <= right)
            {
                middle = (left + right) / 2;

                int compare = Compare(type, idx, dungeonState.dropList[middle]);
                if(compare == 0)
                {
                    dungeonState.dropList[middle].third += amt;
                    return;
                }
                else if(compare < 0)
                    right = middle - 1;
                else
                    left = middle + 1;
            }

            if(middle < 0)
                dungeonState.dropList.Insert(0, new Triplet<DropType, int, int>(type, idx, amt));
            else if (middle >= dungeonState.dropList.Count)
                dungeonState.dropList.Add(new Triplet<DropType, int, int>(type, idx, amt));
            else
                dungeonState.dropList.Insert(middle, new Triplet<DropType, int, int>(type, idx, amt));
        }

        int Compare(DropType type, int idx, Triplet<DropType, int, int> o2)
        {
            int ret = type.CompareTo(o2.first);
            if(ret == 0)
                ret = idx.CompareTo(o2.second);
            
            return ret;
        }
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

    public List<DungeonBuff> dungeonBuffs = new List<DungeonBuff>();
    public List<DungeonBuff> dungeonDebuffs = new List<DungeonBuff>();

    public List<Triplet<DropType, int, int>> dropList = new List<Triplet<DropType, int, int>>();

    public DungeonState() { }
    public DungeonState(int dungeonIdx)
    {
        currDungeon = new Dungeon();
        currDungeon.DungeonInstantiate(dungeonIdx);
        scroll = 0;

        currHP = -1; druidRevive = 0; golemHP = GameManager.slotData.slotClass == 4 ? 0 : -1;
        potionUse[0] = potionUse[1] = false;
        currPos = new int[2] { 0, 0 };
    }

    public Room GetCurrRoom() => currDungeon.GetRoom(currPos[0], currPos[1]);
}

public class ItemData
{
    //1~3 : 스킬 재화, 4~12 : 아이템 특수 재화(상중하 우선, 무기,방어구,장신구)
    //13~15 : 아이템 공통 재화
    public int[] basicMaterials;
    public int[] equipRecipes;

    public Skillbook[] skillbooks;
    public int[] potions;

    public int startIdx;
    public bool[] skillLearned;

    public List<Equipment> weapons;
    public List<Equipment> armors;
    public List<Equipment> accessories;
    int Compare(Equipment e1, Equipment e2)
    {
        int ret = e1.ebp.idx.CompareTo(e2.ebp.idx);
        if (ret == 0)
            return e2.star.CompareTo(e1.star);
        else
            return ret;
    }
    //0무기, 1상, 2하, 3장, 4신, 5반, 6목
    public Equipment[] equipmentSlots = new Equipment[8];

    #region Smith
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
        else if (part <= EquipPart.Necklace)
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
        else if (part <= EquipPart.Necklace)
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
                GetResource(weapons[idx]);
                weapons.RemoveAt(idx);
                break;
            case EquipPart.Top:
            case EquipPart.Pants:
            case EquipPart.Gloves:
            case EquipPart.Shoes:
                GetResource(armors[idx]);
                armors.RemoveAt(idx);
                break;
            case EquipPart.Ring:
            case EquipPart.Necklace:
                GetResource(accessories[idx]);
                accessories.RemoveAt(idx);
                break;
        }

        void GetResource(Equipment e)
        {
            for (int i = 0; i < e.ebp.requireResources.Count; i++)
                basicMaterials[e.ebp.requireResources[i].Key] += Mathf.RoundToInt(Mathf.Pow(2, e.star) * 0.2f * e.ebp.requireResources[i].Value);
        }
    }
    public void SwitchCommonStat(EquipPart part, int idx)
    {
        Equipment tmp = weapons[idx];
        switch (part)
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
        Equipment selectEquip = null;
        int stuff = -1;
        List<Equipment> eList = weapons;
        switch (part)
        {
            case EquipPart.Weapon:
                selectEquip = weapons[idx];
                break;
            case EquipPart.Top:
            case EquipPart.Pants:
            case EquipPart.Gloves:
            case EquipPart.Shoes:
                selectEquip = armors[idx];
                eList = armors;
                break;
            case EquipPart.Ring:
            case EquipPart.Necklace:
                selectEquip = accessories[idx];
                eList = accessories;
                break;
        }

        for (int i = 0; i < eList.Count; i++)
            if (i != idx && eList[i].ebp.idx == selectEquip.ebp.idx && eList[i].star == selectEquip.star)
            {
                stuff = i;
                break;
            }

        if (stuff > 0)
        {
            selectEquip.Fusion();
            eList.RemoveAt(stuff);
            eList.Sort(Compare);
        }
        else
            Debug.Log("there is no stuff");
    }
    #endregion Smith
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
        if (equipmentSlots[(int)part - 1] != null)
        {
            switch (part)
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

    #region Skill
    public bool IsLearned(int idx)
    {
        if (idx < startIdx)
            return true;
        return skillLearned[idx - startIdx];
    }
    public void SkillLearn(int idx)
    {
        skillLearned[idx - startIdx] = true;
    }
    public void DisassembleSkillbook(int idx)
    {
        skillbooks[idx - startIdx].count--;
    }
    #endregion Skill

    public ItemData() {}
    public ItemData(int currClass)
    {
        basicMaterials = new int[16];
        equipRecipes = new int[ItemManager.EQUIP_COUNT + 1];

        Skill[] s = SkillManager.GetSkillData(currClass);
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