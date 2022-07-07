using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using LitJson;


public class ItemManager : MonoBehaviour
{
    static SetOptionManager setManager;

    public const int EQUIP_COUNT = 521;

    ///<summary> 모든 장비 정보 </summary>
    static EquipBluePrint[] bluePrints = new EquipBluePrint[EQUIP_COUNT];

    public static void LoadData()
    {
        for (int i = 0; i < EQUIP_COUNT; i++)
            bluePrints[i] = new EquipBluePrint(i);

        setManager = new SetOptionManager();
    }

    #region ItemDrop
    ///<summary> 아이템 드롭 </summary>
    public static void ItemDrop(int category, float prob)
    {
        if(category == 150)
            GameManager.instance.GetExp((int)prob);
        else if (prob >= 1f)
            for (float i = 0; i < prob; i += 1f)
                AddItem();
        else if (Random.Range(0, 1f) < prob)
            AddItem();

        GameManager.instance.SaveSlotData();

        void AddItem()
        {
            //기본 재료
            if (category <= 15)
            {
                GameManager.instance.slotData.itemData.basicMaterials[category]++;
                GameManager.instance.DropSave(DropType.Material, category);
            }
            //스킬북
            else if (category <= 23)
                AddNewSkillBook(category);
            //장비
            else if (category <= 86)
                AddNewEquip(category);
            //제작법
            else if (category <= 149)
                AddNewEquipRecipe(category);
        }
    }
    static void AddNewEquip(int category)
    {
        int classIdx = GameManager.instance.slotData.slotClass;
        int region = GameManager.instance.slotData.region;

        var possibleList = (from token in bluePrints
                            where (token.category == category) && (token.useClass == classIdx || token.useClass == region || token.useClass == 0)
                            select token);

        if (possibleList.Count() <= 0)
            return;

        EquipBluePrint ebp = possibleList.Skip(Random.Range(0, possibleList.Count())).Take(1).First();

        GameManager.instance.DropSave(DropType.Equip, ebp.idx);
        GameManager.instance.slotData.itemData.EquipDrop(ebp);
    }
    ///<summary> 스킬북 드롭 </summary>
    ///<param name="category"> 19 9lv, 20 7lv, 21 5lv, 22 3lv, 23 1lv </param>
    static void AddNewSkillBook(int category)
    {
        int lvl = 47 - 2 * category;
        Skill[] s = SkillManager.GetSkillData(GameManager.instance.slotData.slotClass);

        List<int> possibleList;
        if (lvl == 1)
            possibleList = (from token in s
                            where lvl == token.reqLvl && token.useType == 1
                            select token.idx).ToList();
        else
            possibleList = (from token in s
                            where lvl == token.reqLvl
                            select token.idx).ToList();

        if (possibleList.Count() <= 0)
            return;

        int skillbookIdx = possibleList[Random.Range(0, possibleList.Count())];
        GameManager.instance.slotData.itemData.SkillBookDrop(skillbookIdx);
        GameManager.instance.DropSave(DropType.Skillbook, skillbookIdx);
        GameManager.instance.SaveSlotData();
    }
    static void AddNewEquipRecipe(int category)
    {
        category -= 63;
        int classIdx = GameManager.instance.slotData.slotClass;
        int region = GameManager.instance.slotData.region;
        var possibleList = (from token in bluePrints
                            where (token.category == category) && (token.useClass == classIdx || token.useClass == region || token.useClass == 0)
                            select token);

        if (possibleList.Count() <= 0)
            return;

        int recipeIdx = possibleList.Skip(Random.Range(0, possibleList.Count())).Take(1).First().idx;

        GameManager.instance.slotData.itemData.RecipeDrop(recipeIdx);
        GameManager.instance.DropSave(DropType.Recipe, bluePrints[recipeIdx].idx);

        GameManager.instance.SaveSlotData();
    }
    #endregion ItemDrop

    #region Smith
    ///<summary> 장비 제작 가능 여부 반환 </summary>
    public static bool CanSmith(int idx) => GameManager.instance.slotData.itemData.CanSmith(bluePrints[idx]);

    ///<summary> 장비 제작 </summary>
    public static void SmithEquipment(int idx)
    {
        GameManager.instance.slotData.itemData.Smith(bluePrints[idx]);
        GameManager.instance.SaveSlotData();
    }
    ///<summary> 장비 분해 </summary>
    public static void DisassembleEquipment(EquipPart part, int idx)
    {
        GameManager.instance.slotData.itemData.Disassemble(part, idx);
        GameManager.instance.SaveSlotData();
    }
    ///<summary> 장비 옵션 변경 </summary>
    public static void SwitchEquipOption(EquipPart part, int idx)
    {
        GameManager.instance.slotData.itemData.SwitchCommonStat(part, idx);
        GameManager.instance.SaveSlotData();
    }
    ///<summary> 장비 융합 </summary>
    public static void FusionEquipment(EquipPart part, int idx)
    {
        GameManager.instance.slotData.itemData.Fusion(part, idx);
        GameManager.instance.SaveSlotData();
    }

    ///<summary> 스킬 학습 </summary>
    public static void SkillLearn(int idx)
    {
        GameManager.instance.slotData.itemData.SkillLearn(idx);
        GameManager.instance.SaveSlotData();
    }
    ///<summary> 스킬북 분해 </summary>
    public static void DisassembleSkillBook(int idx)
    {
        GameManager.instance.slotData.itemData.DisassembleSkillbook(idx);
        GameManager.instance.SaveSlotData();
    }
    #endregion

    #region Equip
    ///<summary> 장비 장착 </summary>
    public static void Equip(EquipPart part, int idx)
    {
        GameManager.instance.slotData.itemData.Equip(part, idx);
        ItemStatUpdate();
        setManager.SetComfirm(GameManager.instance.slotData.itemData);
        GameManager.instance.SaveSlotData();
    }
    ///<summary> 장비 장착 해제 </summary>
    public static void UnEquip(EquipPart part)
    {
        GameManager.instance.slotData.itemData.UnEquip(part);
        ItemStatUpdate();
        setManager.SetComfirm(GameManager.instance.slotData.itemData);
        GameManager.instance.SaveSlotData();
    }
    ///<summary> 장비 장착, 해제 시 스텟 재계산 </summary>
    static void ItemStatUpdate()
    {
        int[] addPivots = new int[13];
        foreach (Equipment e in GameManager.instance.slotData.itemData.equipmentSlots)
            if (e != null)
            {
                addPivots[(int)e.mainStat] += e.mainStatValue;
                addPivots[(int)e.subStat] += e.subStatValue;

                for (int i = 0; i < e.commonStatValue.Count; i++)
                    addPivots[(int)e.commonStatValue[i].Key] += e.commonStatValue[i].Value;
            }

        for (int i = 1; i < 13; i++)
        {
            GameManager.instance.slotData.itemStats[i] = GameManager.baseStats[i] + addPivots[i];
        }
        GameManager.instance.slotData.itemStats[1] = GameManager.instance.slotData.itemStats[2];
        GameManager.instance.slotData.itemStats[3] = GameManager.instance.slotData.itemStats[4];
        GameManager.instance.SaveSlotData();
    }
    ///<summary> 장비 장착 시 변화하는 스텟 반환 </summary>
    public static int[] GetStatDelta(Equipment newE)
    {
        int[] addPivots = new int[13];

        addPivots[(int)newE.mainStat] += newE.mainStatValue;
        addPivots[(int)newE.subStat] += newE.subStatValue;
        for (int i = 0; i < newE.commonStatValue.Count; i++)
            addPivots[(int)newE.commonStatValue[i].Key] += newE.commonStatValue[i].Value;

        Equipment currE = GameManager.instance.slotData.itemData.equipmentSlots[(int)newE.ebp.part];
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
        if(GameManager.instance.slotData.potionSlot[0] == 0)
            GameManager.instance.slotData.potionSlot[0] = potionIdx;
        //2번 슬롯 빔 -> 2번 슬롯에 채움
        else if(GameManager.instance.slotData.potionSlot[1] == 0)
            GameManager.instance.slotData.potionSlot[1] = potionIdx;
        //슬롯 가득참 -> 1번 슬롯 밀어냄
        else
        {
            GameManager.instance.slotData.potionSlot[0] = GameManager.instance.slotData.potionSlot[1];
            GameManager.instance.slotData.potionSlot[1] = potionIdx;
        }  

        GameManager.instance.SaveSlotData();
    }
    #endregion

    #region Show
    ///<summary> 현재 보유 중인 장비 중 태그에 맞는 장비 리스트 반환 </summary>
    public static List<Equipment> GetEquipData(ItemCategory category, Rarity rarity, int lvl)
    {
        List<Equipment> tmp;
        switch (category)
        {
            case ItemCategory.Weapon:
                tmp = GameManager.instance.slotData.itemData.weapons;
                break;
            case ItemCategory.Armor:
                tmp = GameManager.instance.slotData.itemData.armors;
                break;
            case ItemCategory.Accessory:
                tmp = GameManager.instance.slotData.itemData.accessories;
                break;
            default:
                return null;
        }
        return (from x in tmp
                where (rarity == Rarity.None || x.ebp.rarity == rarity) && (lvl == 0 || x.ebp.reqlvl == lvl)
                select x).ToList();
    }
    ///<summary> 현재 보유 중인 스킬북 중 태그에 맞는 리스트 반환 </summary>
    ///<param name="skillType"> -1 전체, 0 액티브, 1 패시브 </param>
    public static List<Skillbook> GetSkillbookData(int skillType, int lvl)
    {
        List<Skillbook> categorizedList = new List<Skillbook>();
        foreach (Skillbook sb in GameManager.instance.slotData.itemData.skillbooks)
        {
            Skill s = SkillManager.GetSkill(GameManager.instance.slotData.slotClass, sb.idx);
            if (sb.count > 0 && (skillType == -1 || s.useType == skillType) && (lvl == 0 || s.reqLvl == lvl))
                categorizedList.Add(sb);
        }

        return categorizedList;
    }
    ///<summary> 현재 보유 중인 장비 레시피 중 태그에 맞는 리스트 반환 </summary>
    public static List<EquipBluePrint> GetRecipeData(Rarity rarity, int lvl)
    {
        int region = GameManager.instance.slotData.region;
        List<EquipBluePrint> ebps = new List<EquipBluePrint>();

        foreach (int equipIdx in GameManager.instance.slotData.itemData.equipRecipes)
        {
            if ((bluePrints[equipIdx].useClass == 0 || bluePrints[equipIdx].useClass == GameManager.instance.slotData.slotClass || bluePrints[equipIdx].useClass == region) &&
            (rarity == Rarity.None || bluePrints[equipIdx].rarity == rarity) &&
            (lvl == 0 || bluePrints[equipIdx].reqlvl == lvl))
                ebps.Add(bluePrints[equipIdx]);
        }

        return ebps;
    }
    ///<summary> 기본 재료 반환 </summary>
    public static int[] GetResourceData(ItemCategory category) => GameManager.instance.slotData.itemData.basicMaterials;

    ///<summary> 현재 장착 중인 장비 반환 </summary>
    public static Equipment GetEquipment(EquipPart p) => GameManager.instance.slotData.itemData.equipmentSlots[(int)p];
    ///<summary> 세트 정보 반환 </summary>
    public static KeyValuePair<string, float[]> GetSetData(int set) => setManager.GetSetData(set);
    ///<summary> 현재 발동 중인 모든 세트효과 반환 </summary>
    public static List<KeyValuePair<string, int>> GetSetList() => setManager.GetSetList();
    #endregion
}

public class SetOptionManager
{
    Dictionary<int, int> setList = new Dictionary<int, int>();
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
    ///<summary> 현재 장착 중인 장비 세트 업데이트 </summary>
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

        //token.Key : set idx, token.Value : set 장비 수
        foreach (KeyValuePair<int, int> token in count)
            if (token.Value >= 2)
                setList.Add(token.Key, token.Value);
    }
    ///<summary> 해당 세트 장착 정보 반환 </summary>
    public KeyValuePair<string, float[]> GetSetData(int set)
    {
        set--;
        float[] tmp = new float[options[set].count];

        if (setList.ContainsKey(set))
            for (int i = 0; i < options[set].count; i++)
                tmp[i] = setList[set] >= options[set].reqPart[i] ? options[set].rate[i] : 0;

        return new KeyValuePair<string, float[]>(options[set].name, tmp);
    }
    public List<KeyValuePair<string, int>> GetSetList()
    {
        List<KeyValuePair<string, int>> list = new List<KeyValuePair<string, int>>();
        foreach(KeyValuePair<int, int> token in setList)

            for (int optionIdx = 0; optionIdx < options[token.Key].count; optionIdx++)
                if(token.Value <= options[token.Key].reqPart[optionIdx])
                    list.Add(new KeyValuePair<string, int>(options[token.Key].name, options[token.Key].reqPart[optionIdx]));

        return list;
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
