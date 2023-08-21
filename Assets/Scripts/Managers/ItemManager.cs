using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using LitJson;


public class ItemManager
{
    static SetOptionManager setManager;

    public const int EQUIP_COUNT = 521;

    ///<summary> 모든 장비 정보 </summary>
    static EquipBluePrint[] bluePrints = new EquipBluePrint[EQUIP_COUNT];
    ///<summary> 스킬 학습 시 소모하는 재화 정보 </summary>
    static JsonData skillLearnJson;

    public static void Debug_Recipe()
    {
        var list = from token in bluePrints
                    where IsPreferClass(token.useClass)
                    select token.idx;

        foreach(int idx in list)
            GameManager.Instance.slotData.itemData.RecipeDrop(idx);
    }

    public static void LoadData()
    {
        for (int i = 0; i < EQUIP_COUNT; i++)
            bluePrints[i] = new EquipBluePrint(i);

        setManager = new SetOptionManager();

        skillLearnJson = JsonMapper.ToObject(Resources.Load<TextAsset>("Jsons/Items/Skillbook").text);
    }
    public static void LoadSetData() => setManager.SetComfirm(GameManager.Instance.slotData.itemData);
    #region ItemDrop
    ///<summary> 아이템 드롭 </summary>
    public static void ItemDrop(int category, float prob, bool isQuest = false)
    {
        if(category <= 15)
        {
            int count = 0;
            while (prob >= 1f)
            {
                count++;
                prob -= 1;
            }
            if (Random.Range(0, 1f) < prob)
                count++;

            if(count <= 0) return;

            GameManager.Instance.slotData.itemData.basicMaterials[category] += count;
            GameManager.Instance.DropSave(DropType.Material, category, count, isQuest);
        }
        else if(category == 150)
        {
            GameManager.Instance.GetExp((int)prob);
            if(isQuest) GameManager.Instance.questExp = (int)prob;
        }
        else
        {
            while (prob >= 1f)
            {
                AddItem();
                prob -= 1;
            }

            if (Random.Range(0, 1f) < prob)
                AddItem();
        }

        GameManager.Instance.SaveSlotData();

        void AddItem()
        {
            //스킬북
            if (category <= 23)
                SkillbookDrop(category, isQuest);
            //장비
            else if (category <= 86)
                EquipmentDrop(category, isQuest);
            //제작법
            else if (category <= 149)
                RecipeDrop(category, isQuest);
        }
    }
    static void EquipmentDrop(int category, bool isQuest)
    {
        int classIdx = GameManager.SlotClass;
        int region = GameManager.Instance.slotData.region;

        var possibleList = from token in bluePrints
                           where (token.category == category) && IsPreferClass(token.useClass)
                           select token;

        if (possibleList.Count() <= 0)
            return;

        EquipBluePrint ebp = possibleList.Skip(Random.Range(0, possibleList.Count())).Take(1).First();

        GameManager.Instance.DropSave(DropType.Equip, ebp.idx, 1, isQuest);
        GameManager.Instance.slotData.itemData.EquipDrop(ebp);
    }
    ///<summary> 스킬북 드롭 </summary>
    ///<param name="category"> 19 9lv, 20 7lv, 21 5lv, 22 3lv, 23 1lv </param>
    static void SkillbookDrop(int category, bool isQuest)
    {
        int lvl = 47 - 2 * category;
        Skill[] s = SkillManager.GetSkillData(GameManager.SlotClass);

        List<int> possibleList;
        if (lvl == 1)
            possibleList = (from token in s
                            where lvl == token.reqLvl && token.category != 1024 && token.useType == 1
                            select token.idx).ToList();
        else
            possibleList = (from token in s
                            where lvl == token.reqLvl && token.category != 1024
                            select token.idx).ToList();

        if (possibleList.Count() <= 0)
            return;

        int skillbookIdx = possibleList[Random.Range(0, possibleList.Count)];
        GameManager.Instance.slotData.itemData.SkillBookDrop(skillbookIdx);
        GameManager.Instance.DropSave(DropType.Skillbook, skillbookIdx, 1, isQuest);
        GameManager.Instance.SaveSlotData();
    }
    static void RecipeDrop(int category, bool isQuest)
    {
        category -= 63;
        int classIdx = GameManager.SlotClass;
        int region = GameManager.Instance.slotData.region;
        var possibleList = (from token in bluePrints
                            where (token.category == category) && IsPreferClass(token.useClass)
                            select token);

        if (possibleList.Count() <= 0)
            return;

        int recipeIdx = possibleList.Skip(Random.Range(0, possibleList.Count())).Take(1).First().idx;

        if (GameManager.Instance.slotData.itemData.RecipeDrop(recipeIdx))
            GameManager.Instance.DropSave(DropType.Recipe, bluePrints[recipeIdx - bluePrints[0].idx].idx, 1, isQuest);

        GameManager.Instance.SaveSlotData();
    }
    ///<summary> 광고를 통한 제작법 획득 </summary>
    public static List<int>[] GetAdRecipe(int lvl)
    {
        List<int>[] lists = new List<int>[] {new List<int>(), new List<int>(), new List<int>(), new List<int>(), new List<int>()};
        
        if(lvl % 2 == 0)
        {    for(int i = 0;i < bluePrints.Length;i++)
                if(bluePrints[i].reqlvl == lvl - 1  && IsPreferClass(bluePrints[i].useClass) && bluePrints[i].rarity != Rarity.Common && !GameManager.Instance.slotData.itemData.equipRecipes.Contains(bluePrints[i].idx))
                    lists[bluePrints[i].rarity - Rarity.Common].Add(bluePrints[i].idx);
        }
        else
            for(int i = 0;i <bluePrints.Length;i++)
                if (bluePrints[i].reqlvl == lvl && IsPreferClass(bluePrints[i].useClass) && !GameManager.Instance.slotData.itemData.equipRecipes.Contains(bluePrints[i].idx))
                    lists[bluePrints[i].rarity - Rarity.Common].Add(bluePrints[i].idx);

        return lists;
    }
    ///<summary> 광고를 통한 스킬북 획득 </summary>
    public static int AdSkillbookDrop(int lvl)
    {
        Skill[] skills = SkillManager.GetSkillData(GameManager.SlotClass);

        List<int> possibleList;
        if (lvl == 1)
            possibleList = (from token in skills
                            where lvl == token.reqLvl && token.category != 1024 && token.useType == 1
                            select token.idx).ToList();
        else
            possibleList = (from token in skills
                            where lvl == token.reqLvl && token.category != 1024
                            select token.idx).ToList();
        
        int skillbookIdx = possibleList[Random.Range(0, possibleList.Count)];
        GameManager.Instance.slotData.itemData.SkillBookDrop(skillbookIdx);
        GameManager.Instance.SaveSlotData();

        return skillbookIdx;
    }
    ///<summary> 장비 클래스 적합 판단 </summary>
    static bool IsPreferClass(int classIdx) => classIdx == 0 || classIdx == GameManager.SlotClass || classIdx == GameManager.Instance.slotData.region;
    #endregion ItemDrop

    #region SmithEquipment
    ///<summary> 장비 제작 </summary>
    public static Equipment CreateEquipment(EquipBluePrint ebp)
    {
        Equipment e = GameManager.Instance.slotData.itemData.Create(ebp);
        GameManager.Instance.SaveSlotData();
        return e;
    }
    ///<summary> 장비 분해 </summary>
    public static void DisassembleEquipment(KeyValuePair<int, Equipment> equipInfo)
    {
        GameManager.Instance.slotData.itemData.Disassemble(equipInfo);
        GameManager.Instance.SaveSlotData();
    }
    ///<summary> 장비 옵션 변경 </summary>
    public static void SwitchCommonStat(Equipment e)
    {
        e.SwitchCommonStat();
        for(int i = 0;i < e.ebp.requireResources.Count;i++)
            GameManager.Instance.slotData.itemData.basicMaterials[e.ebp.requireResources[i].Key] -= Mathf.RoundToInt(0.4f * e.ebp.requireResources[i].Value);
        GameManager.Instance.SaveSlotData();
    }
    ///<summary> 조합 강화 재료로 쓰일 장비 반환 </summary>
    public static List<KeyValuePair<int, Equipment>> GetResourceEquipData(KeyValuePair<int, Equipment> equipInfo)
    {
        List<Equipment> baseList;
        List<KeyValuePair<int, Equipment>> resultList = new List<KeyValuePair<int, Equipment>>();
        if(equipInfo.Value.ebp.part <= EquipPart.Weapon)
            baseList = GameManager.Instance.slotData.itemData.weapons;
        else if(equipInfo.Value.ebp.part <= EquipPart.Shoes)
            baseList = GameManager.Instance.slotData.itemData.armors;
        else   
            baseList = GameManager.Instance.slotData.itemData.accessories;

        for(int i = 0;i < baseList.Count;i++)
            if ((i != equipInfo.Key) && (baseList[i].star == equipInfo.Value.star) && (baseList[i].ebp.idx == equipInfo.Value.ebp.idx))
                resultList.Add(new KeyValuePair<int, Equipment>(i, baseList[i]));

        return resultList;
    }
    public static void MergeEquipment(KeyValuePair<int, Equipment> equipInfo, KeyValuePair<int, Equipment> resourceInfo)
    {
        GameManager.Instance.slotData.itemData.Merge(equipInfo, resourceInfo);
        GameManager.Instance.SaveSlotData();
    }
    #endregion SmithEquipment

    #region SmithSkill
    ///<summary> 스킬 학습 시, 재료 idx, 현재 보유량, 요구량 반환 </summary>
    public static List<Triplet<int, int ,int>> GetRequireResources(Skillbook skillbook)
    {
        int lvl = SkillManager.GetSkill(GameManager.SlotClass, skillbook.idx).reqLvl;
        List<Triplet<int, int, int>> resourceList = new List<Triplet<int, int, int>>();
        int tmp;
        for (int i = 1; i <= 3; i++)
            if ((tmp = (int)skillLearnJson[lvl / 2][$"resource{i}"]) > 0)
                resourceList.Add(new Triplet<int, int, int>(i, GameManager.Instance.slotData.itemData.basicMaterials[i], tmp));
            
        return resourceList;
    }
    ///<summary> 스킬 학습 호출, 재화 소모 </summary>
    public static void SkillLearn(KeyValuePair<int, Skillbook> skillInfo)
    {
        int lvl = SkillManager.GetSkill(GameManager.SlotClass, skillInfo.Value.idx).reqLvl;

        GameManager.Instance.slotData.itemData.SkillLearn(skillInfo);
        for (int i = 1; i <= 3; i++)
            GameManager.Instance.slotData.itemData.basicMaterials[i] -= (int)skillLearnJson[lvl / 2][$"resource{i}"];
        
        GameManager.Instance.SaveSlotData();
    }
    ///<summary> 스킬북 분해 </summary>
    public static void DisassembleSkillBook(KeyValuePair<int, Skillbook> skillbook)
    {
        int lvl = SkillManager.GetSkill(GameManager.SlotClass, skillbook.Value.idx).reqLvl;
        GameManager.Instance.slotData.itemData.DisassembleSkillbook(skillbook.Key);
        for(int i = 1;i <= 3;i++)
            GameManager.Instance.slotData.itemData.basicMaterials[i] += Mathf.CeilToInt((int)skillLearnJson[lvl / 2][$"resource{i}"] / 10f);
        GameManager.Instance.SaveSlotData();
    }
    #endregion SmithSkill

    #region Equip
    ///<summary> 장비 장착 </summary>
    public static void Equip(EquipPart part, int orderIdx)
    {
        GameManager.Instance.slotData.itemData.Equip(part, orderIdx);
        ItemStatUpdate();
        setManager.SetComfirm(GameManager.Instance.slotData.itemData);
        GameManager.Instance.SaveSlotData();
    }
    ///<summary> 장비 장착 해제 </summary>
    public static void UnEquip(EquipPart part)
    {
        GameManager.Instance.slotData.itemData.UnEquip(part);
        ItemStatUpdate();
        setManager.SetComfirm(GameManager.Instance.slotData.itemData);
        GameManager.Instance.SaveSlotData();
    }
    ///<summary> 장비 장착, 해제 시 스텟 재계산 </summary>
    static void ItemStatUpdate()
    {
        int[] addPivots = new int[13];
        foreach (Equipment e in GameManager.Instance.slotData.itemData.equipmentSlots)
            if (e != null)
            {
                addPivots[(int)e.mainStat] += e.mainStatValue;
                addPivots[(int)e.subStat] += e.subStatValue;

                for (int i = 0; i < e.commonStatValue.Count; i++)
                    addPivots[(int)e.commonStatValue[i].Key] += e.commonStatValue[i].Value;
            }

        for (int i = 1; i <= 12; i++)
        {
            int statValue;
            if(GameManager.Instance.slotData.slotClass == 4 && i != 4 && i != 7)
                statValue = Mathf.RoundToInt(0.8f * (GameManager.BaseStats[i] + addPivots[i]));
            else
                statValue = GameManager.BaseStats[i] + addPivots[i];

            GameManager.Instance.slotData.itemStats[i] = statValue;
        }
        GameManager.Instance.slotData.itemStats[1] = GameManager.Instance.slotData.itemStats[2];
        GameManager.Instance.slotData.itemStats[3] = GameManager.Instance.slotData.itemStats[4];
        GameManager.Instance.SaveSlotData();
    }
    ///<summary> 장비 장착 시 변화하는 스텟 반환 </summary>
    public static int[] GetStatDelta(Equipment newE)
    {
        int[] addPivots = new int[13];

        addPivots[(int)newE.mainStat] += newE.mainStatValue;
        addPivots[(int)newE.subStat] += newE.subStatValue;
        for (int i = 0; i < newE.commonStatValue.Count; i++)
            addPivots[(int)newE.commonStatValue[i].Key] += newE.commonStatValue[i].Value;

        Equipment currE = GameManager.Instance.slotData.itemData.equipmentSlots[(int)newE.ebp.part];
        if (currE != null)
        {
            addPivots[(int)currE.mainStat] -= currE.mainStatValue;
            addPivots[(int)currE.subStat] -= currE.subStatValue;
            for (int i = 0; i < currE.commonStatValue.Count; i++)
                addPivots[(int)currE.commonStatValue[i].Key] -= currE.commonStatValue[i].Value;
        }

        int[] ret = new int[10];

        for (int i = 2, j = 0; i < 13; i++)
        {
            if (i == 3) i++;
            if (GameManager.Instance.slotData.slotClass == 4 && i != 4)
                ret[j++] = Mathf.RoundToInt(0.8f * addPivots[i]);
            else
                ret[j++] = addPivots[i];
        }

        return ret;
    }
    ///<summary> 포션 장착 </summary>
    ///<param name="potionIdx"> 1 활력, 2 정화, 3 회복, 4 재활용 </param>
    public static void EquipPotion(int potionIdx)
    {
        if(potionIdx <= 0) return;

        //1번 슬롯 빔 -> 1번 슬롯에 채움
        if(GameManager.Instance.slotData.potionSlot[0] == 0)
            GameManager.Instance.slotData.potionSlot[0] = potionIdx;
        //2번 슬롯 빔 -> 2번 슬롯에 채움
        else if(GameManager.Instance.slotData.potionSlot[1] == 0)
            GameManager.Instance.slotData.potionSlot[1] = potionIdx;
        //슬롯 가득참 -> 1번 슬롯 밀어냄
        else
        {
            GameManager.Instance.slotData.potionSlot[0] = GameManager.Instance.slotData.potionSlot[1];
            GameManager.Instance.slotData.potionSlot[1] = potionIdx;
        }  

        GameManager.Instance.SaveSlotData();
    }
    #endregion Equip

    #region Show
    ///<summary> 현재 보유 중인 장비 중 태그에 맞는 장비 리스트 반환 </summary>
    public static List<KeyValuePair<int, Equipment>> GetEquipData(SmithCategory category, Rarity rarity, int lvl)
    {
        List<KeyValuePair<int, Equipment>> returnList = new List<KeyValuePair<int, Equipment>>();
        List<Equipment>[] baseLists;
        switch (category)
        {
            case SmithCategory.Weapon:
                baseLists = new List<Equipment>[] { GameManager.Instance.slotData.itemData.weapons };
                break;
            case SmithCategory.Armor:
                baseLists = new List<Equipment>[] { GameManager.Instance.slotData.itemData.armors };
                break;
            case SmithCategory.Accessory:
                baseLists = new List<Equipment>[] { GameManager.Instance.slotData.itemData.accessories };
                break;
            case SmithCategory.EquipTotal:
                baseLists = new List<Equipment>[] { GameManager.Instance.slotData.itemData.weapons, GameManager.Instance.slotData.itemData.armors, GameManager.Instance.slotData.itemData.accessories };
                break;
            default:
                return null;
        }

        foreach (List<Equipment> list in baseLists)
            for (int i = 0; i < list.Count; i++)
                if ((rarity == Rarity.None || list[i].ebp.rarity == rarity) && (lvl == 0 || list[i].ebp.reqlvl == lvl))
                    returnList.Add(new KeyValuePair<int, Equipment>(i, list[i]));
        if (category == SmithCategory.EquipTotal) returnList.Sort((a, b) => a.Value.CompareTo(b.Value));
        return returnList;
    }
    ///<summary> 현재 보유 중인 스킬북 중 태그에 맞는 리스트 반환 </summary>
    ///<param name="skillType"> -1 전체, 0 액티브, 1 패시브 </param>
    public static List<KeyValuePair<int, Skillbook>> GetSkillbookData(int skillType, int lvl)
    {
        List<KeyValuePair<int, Skillbook>> categorizedList = new List<KeyValuePair<int, Skillbook>>();
        int i = 0;
        foreach (Skillbook sb in GameManager.Instance.slotData.itemData.skillbooks)
        {
            Skill s = SkillManager.GetSkill(GameManager.SlotClass, sb.idx);
            if (sb.count > 0 && (skillType == -1 || s.useType == skillType) && (lvl == 0 || s.reqLvl == lvl))
                categorizedList.Add(new KeyValuePair<int, Skillbook>(i, sb));
            i++;
        }

        return categorizedList;
    }
    ///<summary> 현재 보유 중인 장비 레시피 중 태그에 맞는 리스트 반환 </summary>
    public static List<KeyValuePair<int, EquipBluePrint>> GetRecipeData(SmithCategory category, Rarity rarity, int lvl)
    {
        int region = GameManager.Instance.slotData.region;
        List<KeyValuePair<int, EquipBluePrint>> ebps = new List<KeyValuePair<int, EquipBluePrint>>();

        foreach (int equipIdx in GameManager.Instance.slotData.itemData.equipRecipes)
        {
            EquipBluePrint ebp = bluePrints[equipIdx - bluePrints[0].idx];
            SmithCategory ebpCategory = ebp.part <= EquipPart.Weapon ? SmithCategory.WeaponRecipe : (ebp.part <= EquipPart.Shoes ? SmithCategory.ArmorRecipe : SmithCategory.AccessoryRecipe);
            if ((category == SmithCategory.RecipeTotal || category == ebpCategory) &&
            (ebp.useClass == 0 || ebp.useClass == GameManager.SlotClass || ebp.useClass == region) &&
            (rarity == Rarity.None || ebp.rarity == rarity) &&
            (lvl == 0 || ebp.reqlvl == lvl))
                ebps.Add(new KeyValuePair<int, EquipBluePrint>(0, ebp));
        }

        return ebps;
    }

    ///<summary> 현재 장착 중인 장비 반환 </summary>
    public static Equipment GetEquipment(EquipPart p) => GameManager.Instance.slotData.itemData.equipmentSlots[(int)p];
    public static EquipBluePrint GetEBP(int equipIdx) => bluePrints[equipIdx - bluePrints[0].idx];
    ///<summary> 자원 이름 
    ///<para> 1 ~ 3 : 스킬 학습 재화(상중하) </para>
    ///<para> 4 ~ 6 : 무기 재화(상중하) </para>
    ///<para> 7 ~ 9 : 방어구 재화(상중하) </para>
    ///<para> 10 ~ 12 : 악세서리 재화(상중하) </para>
    ///<para> 13~15 : 아이템 공통 재화 </para> </summary>
    public static string GetResourceName(int resourceIdx)
    {
        int pivot = (resourceIdx - 1) % 3;
        string resourceName = pivot == 0 ? "상급 " : (pivot == 1 ? "중급 " : "하급 ");
        if(resourceIdx <= 3)
            if(GameManager.Instance.slotData.region < 11)
                resourceName += "용지";
            else
                resourceName += "양피지";
        else if(resourceIdx <= 6)
            switch(GameManager.SlotClass)
            {
                case 1:
                    resourceName += "동력원";
                    break;
                case 2:
                    resourceName += "합금";
                    break;
                case 3:
                    resourceName += "화약";
                    break;
                case 4:
                    resourceName += "톱니바퀴";
                    break;
                case 5:
                    resourceName += "원소 정수";
                    break;
                case 6:
                    resourceName += "신비한 덩굴";
                    break;
                case 7:
                    resourceName += "비전석";
                    break;
                case 8:
                    resourceName += "사악한 날";
                    break;
            }
        else if(resourceIdx <= 9)
            if(GameManager.Instance.slotData.region < 11)
                resourceName += "관절 볼트";
            else
                resourceName += "세계수 줄기";
        else if(resourceIdx <= 12)
            resourceName += "보석";
        else if(GameManager.Instance.slotData.region < 11)
            resourceName += "강철";
        else
            resourceName += "나무";
        
        return resourceName;
    }
    
    ///<summary> 세트 정보 반환 </summary>
    public static KeyValuePair<string, float[]> GetSetData(int set) => setManager.GetSetData(set);
    ///<summary> 선택한 세트 발동 정보 반환
    ///<para> Key - 세트 이름, Value - 세트 옵션(현재 발동 여부, 부위 수, 설명)의 리스트 </para> </summary>
    public static Pair<string, List<Triplet<bool, int, string>>> GetSetInfo(int setIdx) => setManager.GetSetInfo(setIdx);
    ///<summary> 현재 발동 중인 세트 정보 반환
    ///<para> 1. 세트 이름 + 세트 옵션을 위한 부위 수, 3. 세트 옵션 설명 </para> </summary> 
    public static List<Pair<string, string>> GetSetInfo() => setManager.GetSetInfo();
    #endregion Show
}

public class SetOptionManager
{
    ///<summary> 세트 효과 발동하는 수 이상 장착한 세트 저장(세트 인덱스, 장착 부위 수) </summary>
    Dictionary<int, int> currSetInfos = new Dictionary<int, int>();
    SetOption[] setOptions;
    public SetOptionManager()
    {
        TextAsset jsonTxt = Resources.Load<TextAsset>("Jsons/Items/SetOption");
        JsonData json = JsonMapper.ToObject(jsonTxt.text);

        setOptions = new SetOption[json.Count];

        for (int i = 0; i < setOptions.Length; i++)
        {
            setOptions[i] = new SetOption();

            setOptions[i].name = json[i]["name"].ToString();
            setOptions[i].setIdx = (int)json[i]["set"];
            setOptions[i].count = (int)json[i]["count"];

            setOptions[i].rate = new float[setOptions[i].count];
            setOptions[i].reqPart = new int[setOptions[i].count];
            setOptions[i].scripts = new string[setOptions[i].count];
            for (int j = 0; j < setOptions[i].count; j++)
            {
                setOptions[i].scripts[j] = json[i]["script"][j].ToString();
                setOptions[i].reqPart[j] = (int)json[i]["reqPart"][j];
                setOptions[i].rate[j] = float.Parse(json[i]["rate"][j].ToString());
            }
        }
    }
    ///<summary> 현재 장착 중인 장비 세트 업데이트 </summary>
    public void SetComfirm(ItemData itemData)
    {
        bool hasGolemSkill = currSetInfos.Any(x=>x.Key == 11 && x.Value >= 4);

        currSetInfos.Clear();
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

        //token.Key : set idx, token.Value : set 장비 수
        foreach (KeyValuePair<int, int> token in count)
            if ((token.Key != 8 && token.Value >= 2) || token.Value >= 3)
                currSetInfos.Add(token.Key, token.Value);

        //궁극의 피조물 세트, 패시브 동시 장착 해제
        if (hasGolemSkill && !currSetInfos.Any(x => x.Key == 11 && x.Value >= 4))
        {
            List<int> madSkills = new List<int>();
            madSkills.Add(140); madSkills.Add(141); madSkills.Add(142); madSkills.Add(143);
            madSkills.Add(162); madSkills.Add(163);

            for (int i = 0; i < 4; i++)
                if(madSkills.Contains(GameManager.Instance.slotData.passiveSkills[i]))
                    GameManager.Instance.slotData.passiveSkills[i] = 0;      
        }
    }
    ///<summary> 해당 세트 발동 정보 반환
    ///<para> 1. 세트 이름, 2 세트 옵션 수치(미발동시 0) </para> </summary>
    public KeyValuePair<string, float[]> GetSetData(int setIdx)
    {
        float[] tmp = new float[setOptions[setIdx - 1].count];

        if (currSetInfos.ContainsKey(setIdx))
            for (int i = 0; i < setOptions[setIdx - 1].count; i++)
                tmp[i] = currSetInfos[setIdx] >= setOptions[setIdx - 1].reqPart[i] ? setOptions[setIdx - 1].rate[i] : 0;

        return new KeyValuePair<string, float[]>(setOptions[setIdx - 1].name, tmp);
    }

    ///<summary> 선택한 세트 발동 정보 반환
    ///<para> Key - 세트 이름, Value - 세트 옵션(현재 발동 여부, 부위 수, 설명)의 리스트 </para> </summary>
    public Pair<string, List<Triplet<bool, int, string>>> GetSetInfo(int setIdx)
    {
        Pair<string, List<Triplet<bool, int, string>>> setOptionInfos
            = new Pair<string, List<Triplet<bool, int, string>>>(setOptions[setIdx - 1].name, new List<Triplet<bool, int, string>>());
        if(!currSetInfos.ContainsKey(setIdx))
            for(int i = 0;i <setOptions[setIdx - 1].count;i++)
                setOptionInfos.Value.Add(new Triplet<bool, int, string>(false, setOptions[setIdx - 1].reqPart[i], setOptions[setIdx - 1].scripts[i]));
        else
            for(int i = 0;i < setOptions[setIdx - 1].count;i++)
                setOptionInfos.Value.Add(new Triplet<bool, int, string>(currSetInfos[setIdx] >= setOptions[setIdx - 1].reqPart[i], setOptions[setIdx - 1].reqPart[i], setOptions[setIdx - 1].scripts[i]));

        return setOptionInfos;
    }
    
    ///<summary> 현재 발동 중인 세트 정보 반환
    ///<para> 1. 세트 이름 + 세트 옵션을 위한 부위 수, 2. 세트 옵션 설명 </para> </summary> 
    public List<Pair<string, string>> GetSetInfo()
    {
        List<Pair<string, string>> list = new List<Pair<string, string>>();
        foreach(KeyValuePair<int, int> token in currSetInfos)
            for (int optionIdx = 0; optionIdx < setOptions[token.Key - 1].count; optionIdx++)
                if(token.Value >= setOptions[token.Key - 1].reqPart[optionIdx])
                    list.Add(new Pair<string, string>($"{setOptions[token.Key - 1].name} {setOptions[token.Key - 1].reqPart[optionIdx]}세트", setOptions[token.Key - 1].scripts[optionIdx]));

        return list;
    }
    class SetOption
    {
        ///<summary> 세트 이름 </summary>
        public string name;
        ///<summary> 세트 인덱스 </summary>
        public int setIdx;
        ///<summary> 세트 옵션 수 </summary>
        public int count;
        ///<summary> 세트 옵션 당 필요 부위 수 </summary>
        public int[] reqPart;
        ///<summary> 세트 옵션 당 설명 </summary>
        public string[] scripts;
        ///<summary> 세트 효과 정보 </summary>
        public float[] rate;
    }
}
