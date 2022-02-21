using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

using System.Linq;
using LitJson;

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

    [Header("Dungeon")]
    [SerializeField] RoomConnectManager roomConnectMgr;
    [SerializeField] GameObject roomPrefab;

    [SerializeField] ScrollRect scroll;
    [SerializeField] GameObject scrollContent;

    List<List<RoomImage>> roomImages = new List<List<RoomImage>>();

    [Header("Quest Show")]
    [SerializeField] GameObject expandPanel;
    [SerializeField] QuestPanel[] questPanels;
    [SerializeField] GameObject questExpandBtn;

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

        QuestShow();

        GameManager.sound.PlayBGM(BGM.Battle1);
    }

    #region DungeonMaking
    private void SaveState()
    {
        string dungeonData = JsonMapper.ToJson(state);
        PlayerPrefs.SetString(string.Concat("DungeonData", GameManager.currSlot), dungeonData);
    }

    private void LoadState()
    {
        if (PlayerPrefs.HasKey(string.Concat("DungeonData", GameManager.currSlot)))
        {
            state = JsonMapper.ToObject<DungeonState>(PlayerPrefs.GetString(string.Concat("DungeonData", GameManager.currSlot)));
        }
        else
        {
            state.dungeonIdx = GameManager.slotData.dungeonIdx;
            state.currDungeon.DungeonInstantiate(new DungeonBluePrint(state.dungeonIdx));
            state.currPos = new int[2] { 0, 0 };

            PlayerPrefs.DeleteKey(string.Concat("CharState", GameManager.currSlot));
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
                r.transform.SetParent(scrollContent.transform);
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

        scroll.verticalNormalizedPosition = (float)GameManager.slotData.dungeonScroll;
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
        GameManager.slotData.dungeonScroll = scroll.verticalNormalizedPosition;
        GameManager.slotData.dungeonRoom = state.GetCurrRoom().roomEventIdx;
        GameManager.SaveSlotData();

        SaveState();

        RoomType type = state.GetCurrRoom().type;

        if (type == RoomType.Monster || type == RoomType.Boss)
        {
            GameManager.SwitchSceneData(SceneKind.Battle);
            SceneManager.LoadScene("2_1 Battle");
        }
        else if (type == RoomType.Quest)
        {
            GameManager.SwitchSceneData(SceneKind.Outbreak);
            SceneManager.LoadScene("2_3 Outbreak");
        }
        else
        {
            GameManager.SwitchSceneData(SceneKind.Event);
            SceneManager.LoadScene("2_2 Event");
        }
    }

    public void Debug_NewDungeon()
    {
        PlayerPrefs.DeleteKey(string.Concat("DungeonData", GameManager.currSlot));
        SceneManager.LoadScene("2_0 Dungeon");
    }
    #endregion

    #region Quest
    void QuestShow()
    {
        KeyValuePair<QuestData, int[]>[] currQuest = QuestManager.GetCurrQuest();

        for (int i = 0; i < 4; i++)
            if (currQuest[i].Key != null)
                questPanels[i].SetQuestProceed(currQuest[i]);
            else
                questPanels[i].gameObject.SetActive(false);
    }
    public void Btn_QuestExpand()
    {
        expandPanel.SetActive(true);
        questExpandBtn.SetActive(false);
    }
    public void Btn_QuestReduction()
    {
        expandPanel.SetActive(false);
        questExpandBtn.SetActive(true);
    }
    #endregion
}
