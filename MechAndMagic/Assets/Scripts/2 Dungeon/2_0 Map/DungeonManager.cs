using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Linq;

public class DungeonManager : MonoBehaviour
{
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
        MakeImage();
        QuestShow();

        GameManager.sound.PlayBGM(BGM.Battle1);
    }

    #region DungeonMaking
    private void MakeImage()
    {
        Dungeon dungeon = GameManager.slotData.dungeonState.currDungeon;

        scrollContent.GetComponent<RectTransform>().sizeDelta = new Vector2(1063, Mathf.Max(1920, dungeon.floorCount * 300));
        //각 방의 위치 이미지 생성
        for (int i = 0; i < dungeon.floorCount; i++)
        {
            roomImages.Add(new List<RoomImage>());
            for (int j = 0; j < dungeon.roomCount[i]; j++)
            {
                RoomImage r = Instantiate(roomPrefab).GetComponent<RoomImage>();
                r.transform.SetParent(scrollContent.transform);
                r.Init(dungeon.GetRoom(i, j), this);
                r.SetPosition(new Vector3(0, 100, 0) + Vector3.right * 1080f * (j + 1) / (dungeon.roomCount[i] + 1) + Vector3.down * (dungeon.floorCount - dungeon.GetRoom(i,j).floor) * 300);
                roomImages[i].Add(r);
            }
        }

        //방 사이의 연결 이미지 생성
        for (int i = 0; i < dungeon.floorCount - 1; i++)
        {
            for (int j = 0; j < dungeon.roomCount[i]; j++)
            {
                for (int k = 0; k < dungeon.GetRoom(i,j).next.Count; k++)
                {
                    roomConnectMgr.AddConnect(roomImages[i][j].rect, roomImages[i + 1][dungeon.GetRoom(i,j).next[k]].rect);
                }
            }
        }

        scroll.verticalNormalizedPosition = (float)GameManager.slotData.dungeonState.scroll;
    }
    #endregion

    #region DungeonProcess
    public void Btn_RoomSelect(params int[] pos)
    {
        if (!GameManager.CanMove(pos))
        {
            Debug.Log("Can't move there");
            return;
        }

        GameManager.DungeonMove(pos, scroll.verticalNormalizedPosition);
        QuestManager.QuestUpdate(QuestType.Room, 0, 1);

        RoomType type = GameManager.slotData.dungeonState.GetCurrRoom().type;
        if (type == RoomType.Monster || type == RoomType.Boss)
        {
            GameManager.SwitchSceneData(SceneKind.Battle);
            SceneManager.LoadScene("2_1 Battle");
        }
        else if (type == RoomType.Quest)
        {
            GameManager.OutbreakDetermine(pos);
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
        int dungeonIdx = GameManager.slotData.dungeonIdx;
        GameManager.RemoveDungeonData();
        GameManager.SetNewDungeon(dungeonIdx);
        SceneManager.LoadScene("2_0 Dungeon");
    }
    #endregion

    #region Quest
    void QuestShow()
    {
        KeyValuePair<QuestBlueprint, int>[] currQuest = QuestManager.GetCurrQuest();

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
