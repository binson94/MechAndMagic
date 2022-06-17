using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LitJson;

public class GameManager : MonoBehaviour
{
    //singleton
    static GameObject container;
    static GameManager _instance = null;
    public static GameManager instance
    {
        get
        {
            if(_instance == null)
            {
                container = new GameObject();
                container.name = "GameManager";
                _instance = container.AddComponent(typeof(GameManager)) as GameManager;

                ItemManager.LoadData();
                SkillManager.LoadData();

                DontDestroyOnLoad(container);
            }

            return _instance;
        }
    }

    public const int SLOTMAX = 4;
    ///<summary> 현재 플레이 중인 슬롯 </summary>
    static int currSlot;
    ///<summary> 현재 플레이 중인 슬롯 데이터 관리 </summary>
    public static SlotData slotData;

    #region SlotManage
    ///<summary> 새로운 슬롯 생성 </summary>
    public static void CreateNewSlot(int slot, int slotClass)
    {
        currSlot = slot;
        slotData = new SlotData(slotClass);
        SaveSlotData();
    }
    ///<summary> 슬롯 삭제 </summary>
    public static void DeleteSlot(int slot) => PlayerPrefs.DeleteKey($"Slot{slot}");
    ///<summary> 슬롯 불러오기 </summary>
    public static void LoadSlotData(int slot) => slotData = HexToObj<SlotData>(PlayerPrefs.GetString(string.Concat("Slot", currSlot = slot)));
    ///<summary> 슬롯 데이터 저장 </summary>
    public static void SaveSlotData() => PlayerPrefs.SetString($"Slot{currSlot}", ObjToHex(slotData));
    ///<summary> 씬 전환 시, 로드 시 불러올 씬 변경 </summary>
    public static void SwitchSceneData(SceneKind kind)
    {
        slotData.nowScene = kind;
        SaveSlotData();
    } 
    #endregion SlotManage

    #region Dungeon
    ///<summary> 던전 입장 시 새로운 던전 정보 생성 </summary>
    public static void SetNewDungeon(int dungeonIdx)
    {
        slotData.dungeonIdx = dungeonIdx;
        slotData.dungeonState = new DungeonState(dungeonIdx);
        SaveSlotData();
    }
    ///<summary> 던전 정보 삭제(던전 종료, 중단) </summary>
    public static void RemoveDungeonData()
    {
        slotData.dungeonIdx = 0;
        slotData.dungeonState = null;
        SaveSlotData();
    }
    
    ///<summary> 던전에서 해당 위치로 이동 가능 여부 반환 </summary>
    public static bool CanMove(int[] newPos) => (newPos[0] == slotData.dungeonState.currPos[0] + 1 && slotData.dungeonState.GetCurrRoom().next.Contains(newPos[1]));
    ///<summary> 던전에서 해당 위치로 이동 </summary>
    public static void DungeonMove(int[] newPos, float newScroll)
    {
        slotData.dungeonState.currPos = newPos;
        slotData.dungeonState.mapScroll = newScroll;
        slotData.dungeonState.currRoomEvent = slotData.dungeonState.currDungeon.GetRoom(newPos[0], newPos[1]).roomEventIdx;
        SaveSlotData();
    }
    ///<summary> 돌발퀘 방 입장 시, 다른 돌발퀘 방 이벤트로 변경 </summary>
    public static void OutbreakDetermine(int[] pos) => slotData.dungeonState.currDungeon.QuestDetermined(pos);
    
    ///<summary> 경험치 획득 </summary>
    public static void GetExp(int amt)
    {
        if (slotData.lvl < 10)
        {
            slotData.exp += amt;
            while (slotData.lvl < 10 && slotData.exp > SlotData.reqExp[slotData.lvl])
            {
                slotData.exp -= SlotData.reqExp[slotData.lvl];
                slotData.lvl++;
            }

            slotData.DropSave(DropType.EXP, 1, amt);
            SaveSlotData();
        }
    }
    ///<summary> 아이템 드롭 정보 저장 </summary>
    public static void DropSave(DropType type, int idx)
    {
        slotData.DropSave(type, idx);
        SaveSlotData();
    }

    #region Event
    ///<summary> 긍정 이벤트 - 경험치 획득 </summary>
    public static void EventGetExp(float rate) => GetExp(Mathf.RoundToInt(SlotData.reqExp[slotData.lvl] * rate / 100f));
    ///<summary> 부정 이벤트 - 경험치 손실 </summary>
    public static void EventLoseExp(float rate) => slotData.exp = Mathf.Max(0, slotData.exp - Mathf.RoundToInt(SlotData.reqExp[slotData.lvl] * rate / 100f));
    ///<summary> 긍정 이벤트 - 회복 </summary>
    public static void EventGetHeal(float rate)
    {
        int heal = Mathf.RoundToInt(slotData.itemStats[(int)Obj.HP] * rate);
        slotData.dungeonState.currHP = Mathf.Min(slotData.dungeonState.currHP + heal, slotData.itemStats[(int)Obj.HP]);
        SaveSlotData();
    }
    ///<summary> 부정 이벤트 - 피해 </summary>
    public static void EventGetDamage(float rate)
    {
        int dmg = Mathf.RoundToInt(slotData.itemStats[(int)Obj.HP] * rate);
        slotData.dungeonState.currHP = Mathf.Max(slotData.dungeonState.currHP - dmg, 1);
    }
    ///<summary> 긍정 이벤트 - 버프 </summary>
    public static void EventAddBuff(DungeonBuff b)
    {
        slotData.dungeonState.dungeonBuffs.Add(b);
        SaveSlotData();
    }
    ///<summary> 부정 이벤트 - 디버프 </summary>
    public static void EventAddDebuff(DungeonBuff b)
    {
        slotData.dungeonState.dungeonDebuffs.Add(b);
        SaveSlotData();
    }
    ///<summary> 매 전투마다 던전 버프 지속시간 업데이트 </summary>
    public static void UpdateDungeonBuff()
    {
        List<DungeonBuff> list = slotData.dungeonState.dungeonBuffs;
        for (int i = 0; i < list.Count; i++)
        {
            list[i].count--;
            if(list[i].count <= 0)
                list.RemoveAt(i--);
        }

        list = slotData.dungeonState.dungeonDebuffs;
        for (int i = 0; i < list.Count; i++)
        {
            list[i].count--;
            if(list[i].count <= 0)
                list.RemoveAt(i--);
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