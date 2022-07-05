using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Linq;

public class DungeonManager : MonoBehaviour
{
    [Header("Map")]
    ///<summary> 방 사이 연결 이미지 관리 클래스 </summary>
    [SerializeField] RoomConnectManager roomConnectMgr;
    ///<summary> 방 버튼 프리팹 </summary>
    [SerializeField] GameObject roomPrefab;

    ///<summary> 던전 지도 스크롤뷰 </summary>
    [SerializeField] ScrollRect scroll;
    ///<summary> 던전 방, 연결 이미지 부모 오브젝트 </summary>
    [SerializeField] RectTransform scrollParent;

    [Header("Image")]
    ///<summary> 던전 방 이미지 저장 리스트 </summary>
    List<List<RoomImage>> roomImages = new List<List<RoomImage>>();
    ///<summary> 방 버튼 프레임 스프라이트들 </summary>
    public Sprite[] roomBGSprites;

    ///<summary> 방 이미지 스프라이트
    ///<para> 0 빈 방, 1 몬스터, 2 긍정, 3 중립, 4 부정, 5 비공개, 6 보스 </para> </summary>
    [Tooltip("0 빈 방, 1 몬스터, 2 긍정, 3 중립, 4 부정, 5 비공개, 6 보스")]
    public Sprite[] roomSprites;
    ///<summary> 플레이어 위치 표시 이미지 </summary>
    [SerializeField] Image playerIcon;
    ///<summary> 플레이어 아이콘 스프라이트 </summary>
    [SerializeField] Sprite[] playerSprites;

    [Header("Player Info")]
    ///<summary> 플레이어 현재 체력 표시 슬라이더 </summary>
    [SerializeField] Slider hpBar;
    ///<summary> 플레이어 체력 텍스트 </summary>
    [SerializeField] Text hpTxt;
    ///<summary> 플레이어 레벨 표시 텍스트 </summary>
    [SerializeField] Text lvlTxt;

    [SerializeField] RepairPanel repairPanel;

    ///<summary> 각 퀘스트 정보 표시 UI Set </summary>
    [SerializeField] QuestPanel[] questPanels;

    [Header("Option")]
    [SerializeField] Slider bgmSlider;
    [SerializeField] Slider sfxSlider;
    [SerializeField] Slider txtSpdSlider;

    private void Start()
    {
        MakeRoomImage();
        LoadQuestData();
        LoadPlayerInfo();

        bgmSlider.value = (float)SoundManager.instance.option.bgm;
        sfxSlider.value = (float)SoundManager.instance.option.sfx;
        txtSpdSlider.value = SoundManager.instance.option.txtSpd / 2f;
        SoundManager.instance.PlayBGM(BGM.Battle1);

        void LoadPlayerInfo()
        {
            int[] currPos = GameManager.instance.slotData.dungeonData.currPos;

            //플레이어 표시 이미지 스프라이트 및 위치 설정
            playerIcon.sprite = playerSprites[GameManager.instance.slotData.slotClass - 1];
            playerIcon.transform.SetParent(scrollParent);
            playerIcon.rectTransform.anchoredPosition = roomImages[currPos[0]][currPos[1]].rectTransform.anchoredPosition + new Vector2(0, 100);

            //체력바와 레벨 설정
            int hpValue = GameManager.instance.slotData.dungeonData.currHP > 0 ? GameManager.instance.slotData.dungeonData.currHP : GameManager.instance.slotData.itemStats[(int)Obj.HP];
            hpTxt.text = hpValue.ToString();
            hpBar.value = hpValue / (float)GameManager.instance.slotData.itemStats[(int)Obj.HP];
            lvlTxt.text = GameManager.instance.slotData.lvl.ToString();
        }
    }

    ///<summary> 방 버튼 이미지 생성 </summary>
    private void MakeRoomImage()
    {
        //현재 던전 정보 불러오기
        Dungeon dungeon = GameManager.instance.slotData.dungeonData.currDungeon;

        //층 수에 따라 scrollView 크기 조절
        scrollParent.sizeDelta = new Vector2(1063, Mathf.Max(1920, dungeon.floorCount * 400));

        //각 방의 이미지 생성
        for (int i = 0; i < dungeon.floorCount; i++)
        {
            roomImages.Add(new List<RoomImage>());
            for (int j = 0; j < dungeon.roomCount[i]; j++)
            {
                RoomImage token = Instantiate(roomPrefab).GetComponent<RoomImage>();
                token.transform.SetParent(scrollParent);
                {
                    RectTransform newRect = token.transform as RectTransform;
                    RectTransform prefabRect = roomPrefab.GetComponent<RectTransform>();
                    newRect.anchoredPosition = prefabRect.anchoredPosition;
                    newRect.anchorMax = prefabRect.anchorMax;
                    newRect.anchorMin = prefabRect.anchorMin;
                    newRect.localRotation = prefabRect.localRotation;
                    newRect.localScale = prefabRect.localScale; ;
                    newRect.pivot = prefabRect.pivot;
                    newRect.sizeDelta = prefabRect.sizeDelta;
                }
                token.Init(dungeon.GetRoom(i, j), this);

                //좌표 설정 - 기준점은 왼쪽 아래
                token.SetPosition(new Vector3(0, 150, 0) + Vector3.right * 1080 * (j + 1) / (dungeon.roomCount[i] + 1) + Vector3.up * i * 400);
                roomImages[i].Add(token);
            }
        }

        //방 사이의 연결 이미지 생성
        //마지막 층은 다음으로 이어지는 링크 없으므로 floorCount - 1까지
        for (int i = 0; i < dungeon.floorCount - 1; i++)
            for (int j = 0; j < dungeon.roomCount[i]; j++)
            {
                Room currRoom = dungeon.GetRoom(i, j);
                for (int k = 0; k < currRoom.next.Count; k++)
                    roomConnectMgr.AddConnect(roomImages[i][j].rectTransform, roomImages[i + 1][currRoom.next[k]].rectTransform);
            }

        //스크롤 정도 불러오기
        scroll.verticalNormalizedPosition = (float)GameManager.instance.slotData.dungeonData.mapScroll;
    }
    ///<summary> 현재 수행 중인 퀘스트 정보 로드하여 표시 </summary>
    void LoadQuestData()
    {
        KeyValuePair<QuestBlueprint, int>[] currQuest = QuestManager.GetProceedingQuestData();

        for (int i = 0; i < 4; i++)
            if (currQuest[i].Key != null)
                questPanels[i].SetQuestProceed(currQuest[i]);
            else
                questPanels[i].gameObject.SetActive(false);
    }
   
    ///<summary> 방 버튼 클릭 시 호출 
    ///<para> 이동 가능 시 이동 </para> </summary>
    public void Btn_RoomSelect(params int[] pos)
    {
        //이동 불가능한 경우 예외 처리
        if (!GameManager.instance.CanMove(pos)) return;

        //이동한 위치 및 현재 스크롤 상태 저장
        GameManager.instance.DungeonMove(pos, scroll.verticalNormalizedPosition);
        //방 입장 퀘스트 업데이트
        QuestManager.QuestUpdate(QuestType.Room, 0, 1);

        //방 정보에 맞는 씬 불러오기
        RoomType type = GameManager.instance.slotData.dungeonData.GetCurrRoom().type;

        //몬스터 및 보스 - 전투 씬 로드
        if (type == RoomType.Monster || type == RoomType.Boss)
        {
            GameManager.instance.SwitchSceneData(SceneKind.Battle);
            SceneManager.LoadScene((int)SceneKind.Battle);
        }
        //돌발 퀘스트 - 다른 돌발퀘스트 방 이벤트로 변경, 퀘스트 씬 로드
        else if (type == RoomType.Quest)
        {
            GameManager.instance.OutbreakDetermine(pos);
            GameManager.instance.SwitchSceneData(SceneKind.Outbreak);
            SceneManager.LoadScene((int)SceneKind.Outbreak);
        }
        //그 외(빈 방, 이벤트) - 이벤트 씬 로드
        else
        {
            GameManager.instance.SwitchSceneData(SceneKind.Event);
            SceneManager.LoadScene((int)SceneKind.Event);
        }
    }
    
    public void Btn_OpenRepair()
    {
        repairPanel.ResetAllState();
        repairPanel.gameObject.SetActive(true);
    }

    public void Slider_BGM() => SoundManager.instance.BGMSet(bgmSlider.value);
    public void Slider_SFX() => SoundManager.instance.SFXSet(sfxSlider.value);
    public void Slider_TxtSpd()
    {
        txtSpdSlider.value = Mathf.RoundToInt(txtSpdSlider.value * 2) / 2f;
        SoundManager.instance.TxtSet(txtSpdSlider.value);
    }
}
