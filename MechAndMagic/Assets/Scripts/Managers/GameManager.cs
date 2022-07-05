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
                
                _instance = container.AddComponent<GameManager>();

                ItemManager.LoadData();
                SkillManager.LoadData();
                QuestManager.LoadData();
                
                DontDestroyOnLoad(container);
            }

            return _instance;
        }
    }

    public const int SLOTMAX = 4;
    ///<summary> 현재 플레이 중인 슬롯 </summary>
    int currSlot;
    ///<summary> 현재 플레이 중인 슬롯 데이터 관리 </summary>
    public SlotData slotData;

    #region SlotManage
    ///<summary> 새로운 슬롯 생성 </summary>
    public void CreateNewSlot(int slot, int slotClass)
    {
        currSlot = slot;
        slotData = new SlotData(slotClass);
        SaveSlotData();
    }
    ///<summary> 슬롯 삭제 </summary>
    public void DeleteSlot(int slot) => PlayerPrefs.DeleteKey($"Slot{slot}");
    ///<summary> 슬롯 불러오기 </summary>
    public void LoadSlotData(int slot) => slotData = HexToObj<SlotData>(PlayerPrefs.GetString($"Slot{currSlot = slot}"));
    ///<summary> 슬롯 데이터 저장 </summary>
    public void SaveSlotData() => PlayerPrefs.SetString($"Slot{currSlot}", ObjToHex(slotData));
    ///<summary> 씬 전환 시 호출, 로드 시 불러올 씬 변경 </summary>
    public void SwitchSceneData(SceneKind kind)
    {
        slotData.nowScene = kind;
        SaveSlotData();
    } 
    #endregion SlotManage

    #region Dungeon
    ///<summary> 던전 입장 시 새로운 던전 정보 생성 </summary>
    public void SetNewDungeon(int dungeonIdx)
    {
        slotData.dungeonIdx = dungeonIdx;
        slotData.dungeonData = new DungeonData(dungeonIdx);
        SaveSlotData();
    }
    ///<summary> 던전 정보 삭제(던전 종료, 중단) </summary>
    public void RemoveDungeonData()
    {
        slotData.dungeonIdx = 0;
        slotData.dungeonData = null;
        SaveSlotData();
    }
    
    ///<summary> 던전에서 해당 위치로 이동 가능 여부 반환 </summary>
    public bool CanMove(int[] newPos) => (newPos[0] == slotData.dungeonData.currPos[0] + 1 && slotData.dungeonData.GetCurrRoom().next.Contains(newPos[1]));
    ///<summary> 던전에서 해당 위치로 이동 </summary>
    public void DungeonMove(int[] newPos, float newScroll)
    {
        slotData.dungeonData.currPos = newPos;
        slotData.dungeonData.mapScroll = newScroll;
        slotData.dungeonData.currRoomEvent = slotData.dungeonData.currDungeon.GetRoom(newPos[0], newPos[1]).roomEventIdx;
        SaveSlotData();
    }
    ///<summary> 돌발퀘 방 입장 시, 다른 돌발퀘 방 이벤트로 변경 </summary>
    public void OutbreakDetermine(int[] pos) => slotData.dungeonData.currDungeon.QuestDetermined(pos);
    
    ///<summary> 경험치 획득 </summary>
    public void GetExp(int amt)
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
    public void DropSave(DropType type, int idx)
    {
        slotData.DropSave(type, idx);
        SaveSlotData();
    }

    #region Event
    ///<summary> 긍정 이벤트 - 경험치 획득 </summary>
    public void EventGetExp(float rate) => GetExp(Mathf.RoundToInt(SlotData.reqExp[slotData.lvl] * rate / 100f));
    ///<summary> 부정 이벤트 - 경험치 손실 </summary>
    public void EventLoseExp(float rate) => slotData.exp = Mathf.Max(0, slotData.exp - Mathf.RoundToInt(SlotData.reqExp[slotData.lvl] * rate / 100f));
    ///<summary> 긍정 이벤트 - 회복 </summary>
    public void EventGetHeal(float rate)
    {
        int heal = Mathf.RoundToInt(slotData.itemStats[(int)Obj.HP] * rate);
        slotData.dungeonData.currHP = Mathf.Min(slotData.dungeonData.currHP + heal, slotData.itemStats[(int)Obj.HP]);
        SaveSlotData();
    }
    ///<summary> 부정 이벤트 - 피해 </summary>
    public void EventGetDamage(float rate)
    {
        int dmg = Mathf.RoundToInt(slotData.itemStats[(int)Obj.HP] * rate);
        slotData.dungeonData.currHP = Mathf.Max(slotData.dungeonData.currHP - dmg, 1);
    }
    ///<summary> 긍정 이벤트 - 버프 </summary>
    public void EventAddBuff(DungeonBuff b)
    {
        slotData.dungeonData.dungeonBuffs.Add(b);
        SaveSlotData();
    }
    ///<summary> 부정 이벤트 - 디버프 </summary>
    public void EventAddDebuff(DungeonBuff b)
    {
        slotData.dungeonData.dungeonDebuffs.Add(b);
        SaveSlotData();
    }
    ///<summary> 매 전투마다 던전 버프 지속시간 업데이트 </summary>
    public void UpdateDungeonBuff()
    {
        List<DungeonBuff> list = slotData.dungeonData.dungeonBuffs;
        for (int i = 0; i < list.Count; i++)
        {
            list[i].count--;
            if(list[i].count <= 0)
                list.RemoveAt(i--);
        }

        list = slotData.dungeonData.dungeonDebuffs;
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

    public static T GetToken<T>(Queue<T> pool, RectTransform parent, GameObject prefab) where T : MonoBehaviour
    {
        T token;

        if(pool.Count > 0)
        {
            token = pool.Dequeue();
            token.transform.SetParent(parent);
        }
        else
        {
            token = Instantiate(prefab).GetComponent<T>();
            token.transform.SetParent(parent);

            //해상도에 맞게 사이즈 조절
            RectTransform newRect = token.transform as RectTransform;
            RectTransform prefabRect = prefab.GetComponent<RectTransform>();
            newRect.anchoredPosition = prefabRect.anchoredPosition;
            newRect.anchorMax = prefabRect.anchorMax;
            newRect.anchorMin = prefabRect.anchorMin;
            newRect.localRotation = prefabRect.localRotation;
            newRect.localScale = prefabRect.localScale; ;
            newRect.pivot = prefabRect.pivot;
            newRect.sizeDelta = prefabRect.sizeDelta;
        }

        return token;
    }
}