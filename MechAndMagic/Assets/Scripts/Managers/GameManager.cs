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
    public static void LoadSlotData(int slot)
    {
        slotData = JsonMapper.ToObject<SlotData>(PlayerPrefs.GetString(string.Concat("SlotData", currSlot = slot)));
    }

    public static void SaveSlotData()
    {
        PlayerPrefs.SetString(string.Concat("SlotData", currSlot), JsonMapper.ToJson(slotData));
    }

    public static void NewSlot(int slot, int slotClass)
    {
        currSlot = slot;
        slotData = new SlotData(slotClass);
    }
    public static void DeleteSlot(int slot)
    {
        PlayerPrefs.DeleteKey(string.Concat("CharState", slot));
        PlayerPrefs.DeleteKey(string.Concat("Item", slot));
        PlayerPrefs.DeleteKey(string.Concat("QuestData", slot));
        PlayerPrefs.DeleteKey(string.Concat("DungeonData", slot));

        PlayerPrefs.DeleteKey(string.Concat("SlotData", slot));
    }
    #endregion SlotCreate/Delete

    public static void GetExp(int amt)
    {
        if(slotData.lvl < 10)
        {
            slotData.exp += amt;
            if(slotData.exp > SlotData.reqExp[slotData.lvl])
            {
                slotData.exp -= SlotData.reqExp[slotData.lvl];
                slotData.lvl++;
            }
            SaveSlotData();
        }
    }

    public static string ObjToHex<T>(T obj) => System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(JsonMapper.ToJson(obj)));
    public static T HexToObj<T>(string s) => JsonMapper.ToObject<T>(System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String(s)));
}
