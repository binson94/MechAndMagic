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

    public Triplet() { }
    public Triplet(T1 a, T2 b, T3 c)
    {
        first = a; second = b; third = c;
    }
}

///<summary> 슬롯 데이터 저장용 </summary>
public class SlotData
{
    ///<summary> 불변 기본 스텟 </summary>
    public static readonly int[] baseStats = new int[13];
    ///<summary> 불변 경험치 요구량 </summary>
    public static readonly int[] reqExp = new int[9];

    ///<summary> 현재 슬롯의 직업 </summary>
    public int slotClass;
    public int lvl;
    public int exp;
    ///<summary> 기본 스텟 + 아이템에 의한 스텟 </summary>
    public int[] itemStats = new int[13];


    ///<summary> 장착한 액티브 스킬 </summary>
    public int[] activeSkills = new int[6];
    ///<summary> 장착한 패시브 스킬 </summary>
    public int[] passiveSkills = new int[4];
    ///<summary> 장착한 포션 </summary>
    public int[] potionSlot = new int[2];

    ///<summary> 재시작 시 로드할 씬 </summary>
    public SceneKind nowScene;

    ///<summary> 현재 진행 중인 던전 인덱스 </summary>
    public int dungeonIdx;
    ///<summary> 현재 진행 중인 던전 정보 </summary>
    public DungeonState dungeonState;

    ///<summary> 획득한 아이템들 정보 </summary>
    public ItemData itemData;
    ///<summary> 퀘스트 진행 정보 </summary>
    public QuestSlot questData;

    ///<summary> 불변 스텟 및 경험치 요구량 로드 </summary>
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
    ///<summary> 로드 시 사용할 빈 생성자 </summary>
    public SlotData() { }
    ///<summary> 새 슬롯 생성 시 사용할 생성자 </summary>
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
    ///<summary> 던전 진행 중 획득한 아이템 정보 저장(결과창 용) </summary>
    public void DropSave(DropType type, int idx, int amt = 1)
    {
        if (dungeonState == null)
            return;

        if (dungeonState.dropList.Count <= 0)
        {
            dungeonState.dropList.Add(new Triplet<DropType, int, int>(type, idx, amt));
        }
        else
        {
            int left = 0, right = dungeonState.dropList.Count - 1;

            int middle = (left + right) / 2;
            while (left <= middle && middle <= right)
            {
                middle = (left + right) / 2;

                int compare = Compare(type, idx, dungeonState.dropList[middle]);
                if (compare == 0)
                {
                    dungeonState.dropList[middle].third += amt;
                    return;
                }
                else if (compare < 0)
                    right = middle - 1;
                else
                    left = middle + 1;
            }

            if (middle < 0)
                dungeonState.dropList.Insert(0, new Triplet<DropType, int, int>(type, idx, amt));
            else if (middle >= dungeonState.dropList.Count)
                dungeonState.dropList.Add(new Triplet<DropType, int, int>(type, idx, amt));
            else
                dungeonState.dropList.Insert(middle, new Triplet<DropType, int, int>(type, idx, amt));
        }

        int Compare(DropType type, int idx, Triplet<DropType, int, int> o2)
        {
            int ret = type.CompareTo(o2.first);
            if (ret == 0)
                ret = idx.CompareTo(o2.second);

            return ret;
        }
    }
}
///<summary> 던전 진행 정보 저장용 </summary>
public class DungeonState
{
    ///<summary> 던전 맵 스크롤 </summary>
    public double mapScroll;

    ///<summary> 전투 방 번호, 이벤트 번호, 돌발퀘 번호 </summary>
    public int currRoomEvent;
    ///<summary> 던전 맵에서 현재 위치 </summary>
    public int[] currPos;
    ///<summary> 던전 정보 </summary>
    public Dungeon currDungeon;

    ///<summary> 현재 체력 </summary>
    public int currHP;
    ///<summary> 4 매드사이언티스트 골렘 체력 </summary>
    public int golemHP;
    ///<summary> 6 드루이드 부활 여부(255 세계수의 보호) </summary>
    public int druidRevive;
    ///<summary> 포션 사용 여부 </summary>
    public bool[] potionUse = new bool[2];

    ///<summary> 이벤트로 발생한 버프 </summary>
    public List<DungeonBuff> dungeonBuffs = new List<DungeonBuff>();
    ///<summary> 이벤트로 발생한 디버프 </summary>
    public List<DungeonBuff> dungeonDebuffs = new List<DungeonBuff>();

    ///<summary> 드롭된 아이템들 정보 </summary>
    public List<Triplet<DropType, int, int>> dropList = new List<Triplet<DropType, int, int>>();

    ///<summary> 로드를 위한 빈 생성자 </summary>
    public DungeonState() { }
    ///<summary> 던전 입장 시 사용하는 생성자 </summary>
    public DungeonState(int dungeonIdx)
    {
        currDungeon = new Dungeon();
        currDungeon.DungeonInstantiate(dungeonIdx);
        mapScroll = 0;

        currHP = -1; druidRevive = 0; golemHP = GameManager.slotData.slotClass == 4 ? 0 : -1;
        potionUse[0] = potionUse[1] = false;
        currPos = new int[2] { 0, 0 };
    }

    public Room GetCurrRoom() => currDungeon.GetRoom(currPos[0], currPos[1]);
}
///<summary> 아이템 획득 정보 저장용 </summary>
public class ItemData
{
    ///<summary> 기본 재화 
    ///<para> 1~3 : 스킬 재화 </para>
    ///<para> 4~12 : 아이템 특수 재화(상중하 우선, 무기,방어구,장신구) </para>
    ///<para> 13~15 : 아이템 공통 재화 </para>
    ///</summary>
    public int[] basicMaterials;
    ///<summary> 장비 레시피 획득 정보 </summary>
    public int[] equipRecipes;
    ///<summary> 보유 스킬북 정보 </summary>
    public Skillbook[] skillbooks;

    ///<summary> 현재 슬롯 직업의 시작 스킬 인덱스 </summary>
    public int skillStartIdx;
    ///<summary> 스킬 학습 여부 </summary>
    public bool[] skillLearned;

    ///<summary> 획득한 무기 </summary>
    public List<Equipment> weapons;
    ///<summary> 획득한 방어구 </summary>
    public List<Equipment> armors;
    ///<summary> 획득한 장신구 </summary>
    public List<Equipment> accessories;
    List<Equipment> GetEquipmentList(EquipPart part)
    {
        switch (part)
        {
            case EquipPart.Weapon:
                return weapons;
            case EquipPart.Top:
            case EquipPart.Pants:
            case EquipPart.Gloves:
            case EquipPart.Shoes:
                return armors;
        }
        return accessories;
    }
    ///<summary> 장착 중인 장비
    ///<para> 1 무기, 2 상의, 3 하의, 4 장갑, 5 신발, 6 반지, 7 목걸이 </para>
    ///</summary>
    public Equipment[] equipmentSlots = new Equipment[8];

    #region Smith
    ///<summary> 해당 장비 제작 가능 여부 반환 </summary>
    public bool CanSmith(EquipBluePrint ebp)
    {
        for (int i = 0; i < ebp.requireResources.Count; i++)
            if (basicMaterials[ebp.requireResources[i].Key] < ebp.requireResources[i].Value)
                return false;
        return equipRecipes[ebp.idx] > 0;
    }
    ///<summary> 해당 장비 옵션 변환 가능 여부 반환 </summary>
    public bool CanSwitchCommonStat(EquipPart part, int idx) => GetEquipmentList(part)[idx].CanSwitchCommonStat();
    ///<summary> 해당 장비 융합 가능 여부 반환 </summary>
    public bool CanFusion(EquipPart part, int idx)
    {
        int stuff = -1;
        List<Equipment> eList = GetEquipmentList(part);
        Equipment tmp = eList[idx];

        for (int i = 0; i < eList.Count; i++)
            if (i != idx && eList[i].ebp.idx == tmp.ebp.idx && eList[i].star == tmp.star)
            {
                stuff = i;
                break;
            }

        return tmp.star < 3 && stuff >= 0;
    }

    ///<summary> 장비 제작 </summary>
    public void Smith(EquipBluePrint ebp)
    {
        for (int i = 0; i < ebp.requireResources.Count; i++)
            basicMaterials[ebp.requireResources[i].Key] -= ebp.requireResources[i].Value;
        EquipDrop(ebp);
    }
    ///<summary> 장비 분해 </summary>
    public void Disassemble(EquipPart part, int idx)
    {
        List<Equipment> eList = GetEquipmentList(part);
        GetResource(eList[idx]);
        eList.RemoveAt(idx);

        //분해 시 획득 재료 = 제작 시 소모 재료의 20% * 2^(장비 star)
        void GetResource(Equipment e)
        {
            for (int i = 0; i < e.ebp.requireResources.Count; i++)
                basicMaterials[e.ebp.requireResources[i].Key] += Mathf.RoundToInt(Mathf.Pow(2, e.star) * 0.2f * e.ebp.requireResources[i].Value);
        }
    }
    ///<summary> 장비 옵션 변환 </summary>
    public void SwitchCommonStat(EquipPart part, int idx)
    {
        List<Equipment> eList = GetEquipmentList(part);

        eList[idx].SwitchCommonStat();
    }
    ///<summary> 장비 융합 </summary>
    public void Fusion(EquipPart part, int idx)
    {
        int stuff = -1;
        List<Equipment> eList = GetEquipmentList(part);
        Equipment selectEquip = eList[idx];

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
            eList.Sort((a, b) => a.CompareTo(b));
        }
        else
            Debug.Log("there is no stuff");
    }
    #endregion Smith

    ///<summary> 새로운 장비 드롭 </summary>
    public void EquipDrop(EquipBluePrint ebp)
    {
        Equipment tmp = new Equipment(ebp);
        List<Equipment> eList = GetEquipmentList(ebp.part);

        eList.Add(tmp);
        eList.Sort((a, b) => a.CompareTo(b));
    }
    ///<summary> 장비 장착 </summary>
    public void Equip(EquipPart part, int idx)
    {
        bool sort = false;
        List<Equipment> eList = GetEquipmentList(part);

        //이미 장착한 장비 있음 -> 장착 해제
        if (equipmentSlots[(int)part] != null)
        {
            sort = true;
            eList.Add(equipmentSlots[(int)part]);
            equipmentSlots[(int)part] = null;
        }

        //장비 장착
        equipmentSlots[(int)part] = eList[idx];
        eList.RemoveAt(idx);

        if (sort) eList.Sort((a, b) => a.CompareTo(b));
    }
    ///<summary> 장비 해제 </summary>
    public void UnEquip(EquipPart part)
    {
        if (equipmentSlots[(int)part] != null)
        {
            List<Equipment> eList = GetEquipmentList(part);
            eList.Add(equipmentSlots[(int)part]);
            eList.Sort((a, b) => a.CompareTo(b));

            equipmentSlots[(int)part - 1] = null;
        }
    }

    #region Skill
    ///<summary> 스킬 학습 여부 반환 </summary>
    public bool IsLearned(int idx)
    {
        //선행 스킬 없으면 0 표기 -> 무조건 참 반환
        if (idx < skillStartIdx)
            return true;
        return skillLearned[idx - skillStartIdx];
    }
    ///<summary> 스킬 학습 </summary>
    public void SkillLearn(int idx)
    {
        skillLearned[idx - skillStartIdx] = true;
    }
    ///<summary> 스킬북 분해 </summary>
    public void DisassembleSkillbook(int idx)
    {
        skillbooks[idx - skillStartIdx].count--;
    }
    ///<summary> 스킬북 보유 여부 반환 </summary>
    public bool HasSkillBook(int idx) => skillbooks[idx - skillStartIdx].count > 0;
    #endregion Skill

    ///<summary> 로드용 빈 생성자 </summary>
    public ItemData() { }
    public ItemData(int currClass)
    {
        basicMaterials = new int[16];
        equipRecipes = new int[ItemManager.EQUIP_COUNT + 1];

        Skill[] s = SkillManager.GetSkillData(currClass);
        skillbooks = new Skillbook[s.Length];

        skillStartIdx = s[0].idx;
        skillLearned = new bool[s.Length];
        for (int i = 0; i < s.Length; i++)
        {
            skillbooks[i] = new Skillbook();
            skillbooks[i].idx = s[i].idx;
            skillbooks[i].lvl = s[i].reqLvl;
            skillbooks[i].type = s[i].useType;
            skillbooks[i].count = 0;
        }

        skillLearned[0] = skillLearned[1] = true;

        weapons = new List<Equipment>();
        armors = new List<Equipment>();
        accessories = new List<Equipment>();
    }
}