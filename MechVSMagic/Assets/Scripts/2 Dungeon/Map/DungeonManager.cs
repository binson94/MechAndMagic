using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

using System.Linq;
using LitJson;

[System.Serializable]
public class DungeonState
{
    public int dungeonIdx;
    public int[] currPos;
    public Dungeon currDungeon;

    public Room GetCurrRoom()
    {
        return currDungeon.GetRoom(currPos[0], currPos[1]);
    }
}

public class DungeonManager : MonoBehaviour
{
    DungeonState state;

    [SerializeField] RoomConnectManager roomConnectMgr;
    [SerializeField] GameObject roomPrefab;

    [SerializeField] ScrollRect scroll;
    [SerializeField] GameObject scrollContent;

    List<List<RoomImage>> roomImages = new List<List<RoomImage>>();

    private void Start()
    {
        state = new DungeonState
        {
            dungeonIdx = 0,
            currPos = new int[2],
            currDungeon = new Dungeon()
        };

        LoadState();
        MakeImage();
        SaveState();
    }

    #region DungeonMaking
    private void SaveState()
    {
        string dungeonData = JsonMapper.ToJson(state);
        PlayerPrefs.SetString(string.Concat("Dungeon", GameManager.instance.slotNumber), dungeonData);
    }

    private void LoadState()
    {
        if (PlayerPrefs.HasKey(string.Concat("Dungeon", GameManager.instance.slotNumber)))
        {
            state = JsonMapper.ToObject<DungeonState>(PlayerPrefs.GetString(string.Concat("Dungeon", GameManager.instance.slotNumber)));
        }
        else
        {
            state.dungeonIdx = 1;
            state.currDungeon.DungeonInstantiate(new DungeonBluePrint(1));
            state.currPos = new int[2] { 0, 0 };
        }
    }

    private void MakeImage()
    {
        for (int i = 0; i < state.currDungeon.floorCount; i++)
        {
            roomImages.Add(new List<RoomImage>());
            for (int j = 0; j < state.currDungeon.roomCount[i]; j++)
            {
                RoomImage r = Instantiate(roomPrefab).GetComponent<RoomImage>();
                r.transform.parent = scrollContent.transform;
                r.Init(state.currDungeon.GetRoom(i,j), this);
                r.SetPosition(new Vector3(0, 100, 0) + Vector3.right * 1080f * (j + 1) / (state.currDungeon.roomCount[i] + 1) + Vector3.up * state.currDungeon.GetRoom(i,j).floor * 300);
                roomImages[i].Add(r);
            }
        }

        for (int i = 0; i < state.currDungeon.floorCount - 1; i++)
        {
            for (int j = 0; j < state.currDungeon.roomCount[i]; j++)
            {
                for (int k = 0; k < state.currDungeon.GetRoom(i,j).next.Count; k++)
                {
                    roomConnectMgr.AddConnect(roomImages[i][j].rect, roomImages[i + 1][state.currDungeon.GetRoom(i,j).next[k]].rect);
                }

            }
        }
    }
    #endregion

    #region DungeonProcess
    public void Btn_RoomSelect(params int[] pos)
    {
        if (pos[0] != state.currPos[0] + 1 || !state.currDungeon.GetRoom(state.currPos[0], state.currPos[1]).next.Any(n => n == pos[1]))
        {
            Debug.Log("Can't move there");
            return;
        }

        state.currPos = (int[])pos.Clone();
        SaveState();

        RoomType type = state.GetCurrRoom().type;
        PlayerPrefs.SetInt(string.Concat("Room", GameManager.instance.slotNumber), state.GetCurrRoom().roomEventIdx);
        if (type == RoomType.Empty || (RoomType.Positive <= type && type <= RoomType.Quest))
            SceneManager.LoadScene("2_2 Event");
        else
            SceneManager.LoadScene("2_1 Battle");
    }

    public void Debug_NewDungeon()
    {
        PlayerPrefs.DeleteKey(string.Concat("PosX", GameManager.instance.slotNumber));
        PlayerPrefs.DeleteKey(string.Concat("PosY", GameManager.instance.slotNumber));
        PlayerPrefs.DeleteKey(string.Concat("Dungeon", GameManager.instance.slotNumber));
        SceneManager.LoadScene("2_0 Dungeon");
    }
    #endregion
}
