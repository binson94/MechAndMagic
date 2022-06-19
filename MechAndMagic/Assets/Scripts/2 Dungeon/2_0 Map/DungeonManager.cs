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

        bgmSlider.value = (float)SoundManager.instance.option.bgm;
        sfxSlider.value = (float)SoundManager.instance.option.sfx;
        txtSpdSlider.value = SoundManager.instance.option.txtSpd / 2f;
        Btn_CloseOption();
        SoundManager.instance.PlayBGM(BGM.Battle1);

        void LoadPlayerInfo()
        {
            playerIcon.sprite = playerSprites[GameManager.instance.slotData.slotClass - 1];
            playerIcon.transform.position = roomImages[GameManager.instance.slotData.dungeonData.currPos[0]][GameManager.instance.slotData.dungeonData.currPos[1]].transform.position + new Vector3(0, 100, 0);
            playerIcon.transform.SetParent(scrollContent.transform);

            int hpValue = GameManager.instance.slotData.dungeonData.currHP > 0 ? GameManager.instance.slotData.dungeonData.currHP : GameManager.instance.slotData.itemStats[(int)Obj.HP];
            hpTxt.text = hpValue.ToString();
            hpBar.value = hpValue / (float)GameManager.instance.slotData.itemStats[(int)Obj.HP];
            lvlTxt.text = GameManager.instance.slotData.lvl.ToString();
        }
    }

    #region DungeonMaking
    private void MakeImage()
    {
        Dungeon dungeon = GameManager.instance.slotData.dungeonData.currDungeon;

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

        scroll.verticalNormalizedPosition = (float)GameManager.instance.slotData.dungeonData.mapScroll;
    }
    #endregion

    #region DungeonProcess
    public void Btn_RoomSelect(params int[] pos)
    {
        if (!GameManager.instance.CanMove(pos))
        {
            Debug.Log("Can't move there");
            return;
        }

        GameManager.instance.DungeonMove(pos, scroll.verticalNormalizedPosition);
        QuestManager.QuestUpdate(QuestType.Room, 0, 1);

        RoomType type = GameManager.instance.slotData.dungeonData.GetCurrRoom().type;
        if (type == RoomType.Monster || type == RoomType.Boss)
        {
            GameManager.instance.SwitchSceneData(SceneKind.Battle);
            SceneManager.LoadScene("2_1 Battle");
        }
        else if (type == RoomType.Quest)
        {
            GameManager.instance.OutbreakDetermine(pos);
            GameManager.instance.SwitchSceneData(SceneKind.Outbreak);
            SceneManager.LoadScene("2_3 Outbreak");
        }
        else
        {
            GameManager.instance.SwitchSceneData(SceneKind.Event);
            SceneManager.LoadScene("2_2 Event");
        }
    }

    public void Debug_NewDungeon()
    {
        int dungeonIdx = GameManager.instance.slotData.dungeonIdx;
        GameManager.instance.RemoveDungeonData();
        GameManager.instance.SetNewDungeon(dungeonIdx);
        SceneManager.LoadScene("2_0 Dungeon");
    }
    #endregion

    #region Quest
    void QuestShow()
    {
        KeyValuePair<QuestBlueprint, int>[] currQuest = QuestManager.GetProceedingQuestData();

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
    public void Slider_BGM() => SoundManager.instance.BGMSet(bgmSlider.value);
    public void Slider_SFX() => SoundManager.instance.SFXSet(sfxSlider.value);
    public void Slider_TxtSpd()
    {
        txtSpdSlider.value = Mathf.RoundToInt(txtSpdSlider.value * 2) / 2f;
        SoundManager.instance.TxtSet(txtSpdSlider.value);
    }
    #endregion Option
}
