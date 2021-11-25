using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LitJson;

public class GameManager : MonoBehaviour
{
    public static GameManager instance = null;

    public static SoundManager sound = null;

    [Header("Play Data")]
    //0 : 기계 슬롯, 1 : 마법 슬롯
    public const int SLOTMAX = 2;

    public static int currSlot;
    static int[] slotClass = new int[SLOTMAX];

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            sound = transform.GetChild(0).GetComponent<SoundManager>();
            Screen.SetResolution(1080, 1920, true);
            DontDestroyOnLoad(gameObject);
            LoadSlotData();
        }
        else
            Destroy(gameObject);

    }

    static void LoadSlotData()
    {
        if(PlayerPrefs.HasKey("SlotData"))
        {
            slotClass = JsonMapper.ToObject<int[]>(PlayerPrefs.GetString("SlotData"));
        }
    }
    public static void SaveSlotData()
    {
        PlayerPrefs.SetString("SlotData", JsonMapper.ToJson(slotClass));
    }
    public static int GetCurrClass()
    {
        return slotClass[currSlot];
    }
}
