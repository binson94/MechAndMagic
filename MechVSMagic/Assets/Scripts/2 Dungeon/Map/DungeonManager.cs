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
        PlayerPrefs.SetString(string.Concat("DungeonData", GameManager.slotNumber), dungeonData);
    }

    private void LoadState()
    {
        if (PlayerPrefs.HasKey(string.Concat("DungeonData", GameManager.slotNumber)))
        {
            state = JsonMapper.ToObject<DungeonState>(PlayerPrefs.GetString(string.Concat("DungeonData", GameManager.slotNumber)));
        }
        else
        {
            state.dungeonIdx = PlayerPrefs.GetInt(string.Concat("Dungeon", GameManager.slotNumber), 1);
            state.currDungeon.DungeonInstantiate(new DungeonBluePrint(state.dungeonIdx));
            state.currPos = new int[2] { 0, 0 };
        }
    }

    private void MakeImage()
    {
        scrollContent.GetComponent<RectTransform>().sizeDelta = new Vector2(1063, Mathf.Max(1920, state.currDungeon.floorCount * 300));
        //각 방의 위치 이미지 생성
        for (int i = 0; i < state.currDungeon.floorCount; i++)
        {
            roomImages.Add(new List<RoomImage>());
            for (int j = 0; j < state.currDungeon.roomCount[i]; j++)
            {
                RoomImage r = Instantiate(roomPrefab).GetComponent<RoomImage>();
                r.transform.parent = scrollContent.transform;
                r.Init(state.currDungeon.GetRoom(i, j), this);
                r.SetPosition(new Vector3(0, 100, 0) + Vector3.right * 1080f * (j + 1) / (state.currDungeon.roomCount[i] + 1) + Vector3.down * (state.currDungeon.floorCount - state.currDungeon.GetRoom(i,j).floor) * 300);
                roomImages[i].Add(r);
            }
        }

        //방 사이의 연결 이미지 생성
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

        scroll.verticalNormalizedPosition = PlayerPrefs.GetFloat(string.Concat("DungeonScroll", GameManager.slotNumber), 0f);
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
        PlayerPrefs.SetFloat(string.Concat("DungeonScroll", GameManager.slotNumber), scroll.verticalNormalizedPosition);
        SaveState();

        RoomType type = state.GetCurrRoom().type;
        PlayerPrefs.SetInt(string.Concat("Room", GameManager.slotNumber), state.GetCurrRoom().roomEventIdx);

        if (type == RoomType.Monster || type == RoomType.Boss)
            SceneManager.LoadScene("2_1 Battle");
        else if (type == RoomType.Quest)
            SceneManager.LoadScene("2_3 Outbreak");
        else
            SceneManager.LoadScene("2_2 Event");
    }

    public void Debug_NewDungeon()
    {
        PlayerPrefs.DeleteKey(string.Concat("DungeonData", GameManager.slotNumber));
        SceneManager.LoadScene("2_0 Dungeon");
    }
    #endregion
}
