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

    [Header("Image")]
    List<List<RoomImage>> roomImages = new List<List<RoomImage>>();
    public Sprite[] roomBGSprites;
    public Sprite[] roomSprites;
    [SerializeField] Image playerIcon;
    [SerializeField] Sprite[] playerSprites;

    [Header("Player Info")]
    [SerializeField] Slider hpBar;
    [SerializeField] Text hpTxt;
    [SerializeField] Text lvlTxt;

    [Header("Quest Show")]
    [SerializeField] GameObject expandPanel;
    [SerializeField] QuestPanel[] questPanels;
    [SerializeField] GameObject questExpandBtn;
    bool questOpen = false;

    [Header("Option")]
    [SerializeField] GameObject optionPanel;
    [SerializeField] Slider bgmSlider;
    [SerializeField] Slider sfxSlider;
    [SerializeField] Slider txtSpdSlider;

    private void Start()
    {
        MakeImage();
        QuestShow();
        LoadPlayerInfo();

        bgmSlider.value = (float)GameManager.sound.option.bgm;
        sfxSlider.value = (float)GameManager.sound.option.sfx;
        txtSpdSlider.value = GameManager.sound.option.txtSpd / 2f;
        Btn_CloseOption();
        GameManager.sound.PlayBGM(BGM.Battle1);

        void LoadPlayerInfo()
        {
            playerIcon.sprite = playerSprites[GameManager.slotData.slotClass - 1];
            playerIcon.transform.position = roomImages[GameManager.slotData.dungeonState.currPos[0]][GameManager.slotData.dungeonState.currPos[1]].transform.position + new Vector3(0, 100, 0);
            playerIcon.transform.SetParent(scrollContent.transform);

            int hpValue = GameManager.slotData.dungeonState.currHP > 0 ? GameManager.slotData.dungeonState.currHP : GameManager.slotData.itemStats[(int)Obj.HP];
            hpTxt.text = hpValue.ToString();
            hpBar.value = hpValue / (float)GameManager.slotData.itemStats[(int)Obj.HP];
            lvlTxt.text = GameManager.slotData.lvl.ToString();
        }
    }

    #region DungeonMaking
    private void MakeImage()
    {
        Dungeon dungeon = GameManager.slotData.dungeonState.currDungeon;

        scrollContent.GetComponent<RectTransform>().sizeDelta = new Vector2(1063, Mathf.Max(1920, dungeon.floorCount * 400));
        //각 방의 위치 이미지 생성
        for (int i = 0; i < dungeon.floorCount; i++)
        {
            roomImages.Add(new List<RoomImage>());
            for (int j = 0; j < dungeon.roomCount[i]; j++)
            {
                RoomImage r = Instantiate(roomPrefab).GetComponent<RoomImage>();
                r.transform.SetParent(scrollContent.transform);
                r.Init(dungeon.GetRoom(i, j), this);
                r.SetPosition(new Vector3(0, 100, 0) + Vector3.right * 1080f * (j + 1) / (dungeon.roomCount[i] + 1) + Vector3.down * (dungeon.floorCount - dungeon.GetRoom(i,j).floor) * 400);
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
    public void Btn_QuestToggle()
    {
        questOpen = !questOpen;
        expandPanel.SetActive(questOpen);
    }
    #endregion

   #region Option
    public void Btn_OpenOption() => optionPanel.SetActive(true);
    public void Btn_CloseOption()
    {
        optionPanel.SetActive(false);
    }
    public void Slider_BGM() => GameManager.sound.BGMSet(bgmSlider.value);
    public void Slider_SFX() => GameManager.sound.SFXSet(sfxSlider.value);
    public void Slider_TxtSpd()
    {
        PlayerPrefs.SetInt("TxtSpd", Mathf.RoundToInt(txtSpdSlider.value * 2));
        txtSpdSlider.value = Mathf.RoundToInt(txtSpdSlider.value * 2) / 2f;
    }
    #endregion Option
}
