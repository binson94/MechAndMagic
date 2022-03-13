using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LitJson;

public class GameManager : MonoBehaviour
{
    static GameManager instance = null;
    public static SoundManager sound = null;

    [Header("Play Data")]
    //0 : 기계 슬롯, 1 : 마법 슬롯
    public const int SLOTMAX = 4;

    public static int currSlot;
    public static SlotData slotData;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            sound = transform.GetChild(0).GetComponent<SoundManager>();
            Screen.SetResolution(1080, 1920, false);
            DontDestroyOnLoad(gameObject);
        }
        else
            Destroy(gameObject);
    }

    #region SlotCreate/Delete
    public static void SwitchSceneData(SceneKind kind)
    {
        slotData.nowScene = kind;
        SaveSlotData();
    }
    public static void LoadSlotData(int slot) => slotData = HexToObj<SlotData>(PlayerPrefs.GetString(string.Concat("Slot", currSlot = slot)));
    public static void SaveSlotData() => PlayerPrefs.SetString(string.Concat("Slot", currSlot), ObjToHex(slotData));
    public static void NewSlot(int slot, int slotClass)
    {
        currSlot = slot;
        slotData = new SlotData(slotClass);
        SaveSlotData();
    }
    public static void DeleteSlot(int slot) => PlayerPrefs.DeleteKey(string.Concat("Slot", slot));
    #endregion SlotCreate/Delete

    #region Dungeon
    public static void GetExp(int amt)
    {
        if (slotData.lvl < 10)
        {
            slotData.exp += amt;
            if (slotData.exp > SlotData.reqExp[slotData.lvl])
            {
                slotData.exp -= SlotData.reqExp[slotData.lvl];
                slotData.lvl++;
            }

            slotData.DropSave(DropType.EXP, 1, amt);
            SaveSlotData();
        }
    }

    public static void SetNewDungeon(int dungeonIdx)
    {
        slotData.dungeonIdx = dungeonIdx;
        slotData.dungeonState = new DungeonState(dungeonIdx);
        SaveSlotData();
    }
    public static bool CanMove(int[] newPos) => (newPos[0] == slotData.dungeonState.currPos[0] + 1 && slotData.dungeonState.GetCurrRoom().next.Contains(newPos[1]));
    public static void DungeonMove(int[] newPos, float newScroll)
    {
        slotData.dungeonState.currPos = newPos;
        slotData.dungeonState.scroll = newScroll;
        slotData.dungeonState.currRoomEvent = slotData.dungeonState.currDungeon.GetRoom(newPos[0], newPos[1]).roomEventIdx;
        SaveSlotData();
    }
    public static void OutbreakDetermine(int[] pos) => slotData.dungeonState.currDungeon.QuestDetermined(pos);
    public static void DropSave(DropType type, int idx)
    {
        slotData.DropSave(type, idx);
        SaveSlotData();
    }
    public static void RemoveDungeonData()
    {
        slotData.dungeonState = null;
        SaveSlotData();
    }
    #region Event
    public static void GetExpPer(float rate) => GetExp(Mathf.RoundToInt(SlotData.reqExp[slotData.lvl] * rate / 100f));
    public static void LossExpPer(float rate) => slotData.exp = Mathf.Max(0, slotData.exp - Mathf.RoundToInt(SlotData.reqExp[slotData.lvl] * rate / 100f));
    public static void GetHeal(float rate)
    {
        int heal = Mathf.RoundToInt(slotData.itemStats[(int)Obj.HP] * rate);
        slotData.dungeonState.currHP = Mathf.Min(slotData.dungeonState.currHP + heal, slotData.itemStats[(int)Obj.HP]);
        SaveSlotData();
    }
    public static void GetDamage(float rate)
    {
        int dmg = Mathf.RoundToInt(slotData.itemStats[(int)Obj.HP] * rate);
        slotData.dungeonState.currHP = Mathf.Max(slotData.dungeonState.currHP - dmg, 1);
    }
    public static void AddBuff(DungeonBuff b)
    {
        slotData.dungeonState.dungeonBuffs.Add(b);
        SaveSlotData();
    }
    public static void AddDebuff(DungeonBuff b)
    {
        slotData.dungeonState.dungeonDebuffs.Add(b);
        SaveSlotData();
    }
    public static void UpdateBuff()
    {
        List<DungeonBuff> list = slotData.dungeonState.dungeonBuffs;
        for (int i = 0; i < list.Count; i++)
        {
            list[i].count--;
            if(list[i].count <= 0)
                list.RemoveAt(i);
        }

        list = slotData.dungeonState.dungeonDebuffs;
        for (int i = 0; i < list.Count; i++)
        {
            list[i].count--;
            if(list[i].count <= 0)
                list.RemoveAt(i);
        }
        SaveSlotData();
    }
    #endregion Event
    #endregion Dungeon

    public static string ObjToHex<T>(T obj)
    {
        if (obj == null)
            return string.Empty;
        else
            return System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(JsonMapper.ToJson(obj)));
    }
    public static T HexToObj<T>(string s)
    {
        if (s == string.Empty || s == null)
            return default(T);
        else
            return JsonMapper.ToObject<T>(System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String(s)));
    }
}