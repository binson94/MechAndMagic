using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using LitJson;
using System.Linq;

public class ScriptPanel : MonoBehaviour, ITownPanel
{
    [SerializeField] TownManager TM;

    #region UI
    [Header("Illust")]
    [SerializeField] Image blasterImage;
    [SerializeField] Image otherClassImage;
    ///<summary> 대화 중인 캐릭터와 npc 일러스트 </summary>
    [SerializeField] Image[] charIllusts;
    [SerializeField] Sprite[] playerSprites;
    [SerializeField] Sprite[] npcSprites;

    [Header("Dialog")]
    ///<summary> 대화 선택 버튼 부모 오브젝트, 선택 가능 상황에서만 활성화 </summary>
    [SerializeField] GameObject dialogSelectPanel;
    ///<summary> 대화 선택 버튼들 상태 표시 </summary>
    [SerializeField] DialogButton[] dialogSelectBtns;
    ///<summary> 대화 선택 버튼 퀘스트 스프라이트 </summary>
    [SerializeField] Sprite[] questSprites;
    ///<summary> 퀘스트 수락/거절 버튼 부모 오브젝트, 선택 가능 상황에서만 활성화 </summary>
    [SerializeField] GameObject questSelectBtns;

    ///<summary> 실제 대화 내용 표시 텍스트 </summary>
    [SerializeField] Text dialogTxt;
    ///<summary> 현재 대사 중인 캐릭터 이름 표시 텍스트 </summary>
    [SerializeField] Text dialogTalkerTxt;

    [SerializeField] GameObject rewardPanel;

    [SerializeField] GameObject[] jumpBtns;
    [SerializeField] Text[] jumpTxts;
    #endregion UI

    #region Reward
    ///<summary> 퀘스트 보상 경험치 표시 텍스트 </summary>
    [Header("Reward")]
    [SerializeField] Text expTxt;
    ///<summary> 퀘스트 보상 토큰 </summary>
    [SerializeField] DropToken dropTokenPrefab;
    ///<summary> 퀘스트 보상 토큰 부모 오브젝트 </summary>
    [SerializeField] RectTransform tokenParent;
    ///<summary> 퀘스트 보상 풀 부모 오브젝트 </summary>
    [SerializeField] RectTransform poolParent;
    ///<summary> 퀘스트 보상 풀 </summary>
    Queue<DropToken> tokenPool = new Queue<DropToken>();
    ///<summary> 퀘스트 보상 토큰 리스트 </summary>
    List<DropToken> tokenList = new List<DropToken>();
    ///<summary> 퀘스트 보상 팝업 매니저 </summary>
    [SerializeField] PopUpManager pm;
    #endregion Reward

    Color illustGrayColor = new Color(0.7f, 0.7f, 0.7f, 1);

    #region Dialog
    ///<summary> 현재 대화 진행 상태 </summary>
    DialogState state = DialogState.Start;
    ///<summary> 현재 선택한 npc의 idx </summary>
    int selectedNpcIdx = -1;
    ///<summary> 선택한 npc의 현재 가능한 대화 목록 </summary>
    List<KeyValuePair<DialogData, QuestState>> dialogList = new List<KeyValuePair<DialogData, QuestState>>();
    ///<summary> 현재 진행 중인 대화 데이터 </summary>
    DialogData currDialog = null;
    ///<summary> 진행 중인 대화 텍스트 </summary>
    JsonData dialogJson;
    ///<summary> 현재 대화 텍스트 위치 dialogJson[pos] </summary>
    int pos;
    ///<summary> 텍스트 표시 코루틴 </summary>
    Coroutine proceedDialog;
    #endregion Dialog

    void Start()
    {
        charIllusts[0] = GameManager.SlotClass == 3 ? blasterImage : otherClassImage;
        blasterImage.gameObject.SetActive(GameManager.SlotClass == 3);
        otherClassImage.gameObject.SetActive(GameManager.SlotClass != 3);
        
        charIllusts[0].sprite = playerSprites[GameManager.SlotClass];
    }
    //최초 상태로 되돌리기
    public void ResetAllState()
    {
        StopAllCoroutines();

        state = DialogState.Start;
        dialogTxt.text = string.Empty;

        foreach (GameObject g in jumpBtns)
            g.SetActive(false);

        currDialog = null;

        dialogList.Clear();
        questSelectBtns.SetActive(false);

        rewardPanel.SetActive(false);
    }

    ///<summary> 선택한 npc의 대화 목록 불러오기 </summary>
    public void SelectNPC(int npcIdx)
    {
        selectedNpcIdx = npcIdx;
        charIllusts[1].sprite = npcSprites[selectedNpcIdx];
        charIllusts[1].color = illustGrayColor;

        LoadDialogList();
        dialogSelectPanel.SetActive(true);

        dialogTalkerTxt.text = string.Empty;
    }
    ///<summary> 선택한 npc 대화 목록 불러오기 - 퀘스트 진행 상황에 따라 불러옴 </summary>
    void LoadDialogList()
    {
        dialogList.Clear();
        List<QuestProceed> proceedingQuestList = QuestManager.GetCurrQuest();
        List<int> clearedQuestList = QuestManager.GetClearedQuest();

        int i = 0;
        NPC npc = TM.GetNPCData(selectedNpcIdx);
        for (; i < npc.count && dialogList.Count < 4; i++)
        {
            DialogData dialog = npc.dialogs[i];

            //현재 3개 이상 퀘스트 수행 중일 시, 새로운 퀘스트 관련 대화 표시 안함
            if (proceedingQuestList.Count >= 3 && dialog.kind == 1 &&
                !proceedingQuestList.Any(x => x.idx == dialog.linkedQuest))
                continue;

            //관련 퀘스트 수행 중 여부 알아냄
            QuestState qs = QuestState.NotReceive;
            foreach (QuestProceed qp in proceedingQuestList)
                if (qp.idx == dialog.linkedQuest)
                {
                    qs = qp.state;
                    break;
                }

            //1. 퀘스트 관련 대화가 아니거나 퀘스트 3개 이상 수행 중 아님
            //2. 선행 퀘스트 클리어함
            //3. 숨김 퀘스트 클리어 안함
            // 위 조건 모두 만족 시 대화에 추가
            if (IsAdd(i))
                dialogList.Add(new KeyValuePair<DialogData, QuestState>(npc.dialogs[i], qs));
        }

        i = 0;
        for (; i < dialogList.Count; i++)
        {
            dialogSelectBtns[i].Set(dialogList[i], questSprites[(int)dialogList[i].Value]);
            dialogSelectBtns[i].gameObject.SetActive(true);
        }
        for (; i < dialogSelectBtns.Length; i++)
            dialogSelectBtns[i].gameObject.SetActive(false);

        //선행 퀘스트, 관련 퀘스트, 레벨 조건 검사
        bool IsAdd(int idx)
        {
            int reqQuest = npc.dialogs[idx].reqQuest;
            int linkedQuest = npc.dialogs[idx].linkedQuest;
            //선행 퀘스트 클리어, 관련 퀘스트 클리어 안함, 레벨 넘김
            return (clearedQuestList.Contains(reqQuest) &&
                    (linkedQuest == 0 || !clearedQuestList.Contains(linkedQuest)) &&
                    GameManager.SlotLvl >= npc.dialogs[idx].lvl);
        }
    }

    //npc의 대화 목록 선택 버튼 - 선택한 대화 시작
    public void Btn_SelectDialog(int idx)
    {
        if (state != DialogState.Start)
            return;

        currDialog = dialogList[idx].Key;
        string path;
        if (currDialog.kind == 1 && dialogList[idx].Value == QuestState.Proceeding)
            path = "Jsons/Scripts/dialog_QuestProceed";
        else
        {
            path = $"Jsons/Scripts/dialog{currDialog.idx}";
            if (currDialog.kind == 1)
                path += dialogList[idx].Value == QuestState.NotReceive ? "R" : "C";
        }

        //선택한 대화 불러오기
        dialogJson = JsonMapper.ToObject(Resources.Load<TextAsset>(path).text);
        pos = 0;

        dialogSelectPanel.SetActive(false);
        state = DialogState.Next;
        NextToken();
    }

    #region Dialog
    public void Btn_ScriptSFX() => SoundManager.Instance.PlaySFX((int)SFXList.Talk);
    ///<summary> 대화 진행 버튼 - 다음 대사 로드 </summary>
    public void Btn_NextDialog()
    {
        if (state == DialogState.Proceed)
            SkipDialog();
        else if (state == DialogState.Next)
            NextToken();
    }
    ///<summary> 퀘스트 관련 대화에서 퀘스트 수락 / 거절 버튼 </summary>
    public void Btn_AcceptQuest(int isAccept)
    {
        questSelectBtns.SetActive(false);
        //퀘스트 수락
        if (isAccept == 1)
        {
            QuestManager.AcceptQuest(false, currDialog.linkedQuest);
            pos++;
        }
        //퀘스트 거절 - 대화 위치 변경
        else
            pos = int.Parse(dialogJson[pos]["script"].ToString());

        state = DialogState.Next;
        NextToken();
    }
    ///<summary> 선택 분기 대화에서 선택 버튼 </summary>
    public void Btn_SelectJump(int idx)
    {
        foreach (GameObject g in jumpBtns)
            g.SetActive(false);

        pos++;
        pos = int.Parse(dialogJson[pos]["script"].ToString().Split('^')[idx]);
        NextToken();
    }
    ///<summary> 다음 대사 로드 </summary>
    void NextToken()
    {
        if (state != DialogState.Next)
            return;

        //대화 끝
        if (pos == dialogJson.Count)
        {
            state = DialogState.End;
            EndDialog();
        }
        else
        {
            switch ((DialogToken)(int)dialogJson[pos]["code"])
            {
                //대사 출력
                case DialogToken.NPC:
                    charIllusts[0].color = illustGrayColor; charIllusts[1].color = Color.white;
                    dialogTalkerTxt.text = TM.GetNPCData(selectedNpcIdx).name;
                    state = DialogState.Proceed;
                    proceedDialog = StartCoroutine(ProceedDialog());
                    break;
                case DialogToken.Player:
                    charIllusts[0].color = Color.white; charIllusts[1].color = illustGrayColor;
                    dialogTalkerTxt.text = GameManager.Instance.slotData.className;
                    state = DialogState.Proceed;
                    proceedDialog = StartCoroutine(ProceedDialog());
                    break;
                case DialogToken.Narration:
                    charIllusts[0].color = illustGrayColor; charIllusts[1].color = illustGrayColor;
                    dialogTalkerTxt.text = string.Empty;
                    state = DialogState.Proceed;
                    proceedDialog = StartCoroutine(ProceedDialog());
                    break;
                //퀘스트 버튼 보이기, QuestAccept state로 전환
                case DialogToken.Quest:
                    charIllusts[0].color = illustGrayColor; charIllusts[1].color = illustGrayColor;
                    questSelectBtns.SetActive(true);
                    state = DialogState.QuestAccept;
                    break;
                //퀘스트 클리어
                case DialogToken.QuestClear:
                    state = DialogState.End;
                    ClearQuest(currDialog.linkedQuest);
                    break;
                //선택지 구문
                case DialogToken.Select:
                    charIllusts[0].color = illustGrayColor; charIllusts[1].color = illustGrayColor;
                    string[] btnTxts = dialogJson[pos]["script"].ToString().Split('^');
                    for (int i = 0; i < btnTxts.Length; i++)
                    {
                        jumpTxts[i].text = btnTxts[i];
                        jumpBtns[i].SetActive(true);
                    }
                    break;
                //대화 종료
                case DialogToken.EndDialog:
                    state = DialogState.End;
                    EndDialog();
                    break;
                //스토리 보이기, 대화 종료
                case DialogToken.Story:
                    PlayStory(int.Parse(dialogJson[pos]["script"].ToString()));
                    break;
                //에러 - 대화 종료
                default:
                    state = DialogState.End;
                    EndDialog();
                    break;
            }
        }

    }

    #region DialogTokens
    #region NormalDialog
    IEnumerator ProceedDialog()
    {
        float time = 0.1f - 0.035f * SoundManager.Instance.GetTxtSpd();
        if (state != DialogState.Proceed)
            yield break;

        dialogTxt.text = string.Empty;
        string str = dialogJson[pos]["script"].ToString();
        for (int i = 0; i < str.Length; i++)
        {
            if (str[i] == '<')
            {
                int count = 0;
                while (count < 2)
                {
                    dialogTxt.text += str[i];
                    if (str[i] == '>')
                        count++;
                    i++;
                }

                if (i >= str.Length)
                    break;

                yield return new WaitForSeconds(time);
            }
            dialogTxt.text += str[i];
            yield return new WaitForSeconds(time);
        }

        state = DialogState.Next;
        pos++;
    }
    void SkipDialog()
    {
        StopCoroutine(proceedDialog);
        dialogTxt.text = dialogJson[pos++]["script"].ToString();
        state = DialogState.Next;
    }
    #endregion NormalDialog

    void PlaySFX(int idx) => SoundManager.Instance.PlaySFX(idx);
    void PlayStory(int storyIdx)
    {
        GameManager.Instance.slotData.storyIdx = storyIdx;
        
        if(currDialog.linkedQuest > 0)
            QuestManager.ClearQuest(currDialog.linkedQuest);

        GameManager.Instance.SwitchSceneData(SceneKind.Story);
        GameManager.Instance.LoadScene(SceneKind.Story);
    }
    void NewQuest(int questIdx) => QuestManager.AcceptQuest(false, questIdx);
    void ClearQuest(int questIdx)
    {
        dialogTxt.text = string.Empty;
        dialogTalkerTxt.text = string.Empty;
        charIllusts[0].color = illustGrayColor; charIllusts[1].color = illustGrayColor;

        QuestManager.ClearQuest(questIdx);
        LoadReward();
        rewardPanel.SetActive(true);
    } 
    void EndDialog()
    {
        if(currDialog.idx == 21)
            TM.Btn_SelectPanel(0);

        state = DialogState.Start;
        dialogTxt.text = string.Empty;
        dialogTalkerTxt.text = string.Empty;
        currDialog = null;
        LoadDialogList();

        charIllusts[0].color = illustGrayColor; charIllusts[1].color = illustGrayColor;
        dialogSelectPanel.SetActive(true);
        questSelectBtns.SetActive(false);
    }
    #endregion DialogTokens
    #endregion Dialog

    void LoadReward()
    {
        ResetDropInfo();
        List<Triplet<DropType, int, int>> drops = GameManager.Instance.questDrops;

        DropToken token;
        List<Triplet<DropType, int, int>> idxs = new List<Triplet<DropType, int, int>>();

        for(int i = 0;i < drops.Count;)
        {
            token = GameManager.GetToken(tokenPool, tokenParent, dropTokenPrefab);

            for (int j = 0; j < 5 && i < drops.Count; i++, j++)
                idxs.Add(drops[i]);

            token.Initialize(pm, idxs);
            tokenList.Add(token);
            token.gameObject.SetActive(true);
            idxs.Clear();
        }

        if (GameManager.Instance.questExp > 0)
            expTxt.text = $"경험치 +{GameManager.Instance.questExp}";
        else
            expTxt.text = string.Empty;


        void ResetDropInfo()
        {
            for(int i = 0;i < tokenList.Count;i++)
            {
                tokenList[i].gameObject.SetActive(false);
                tokenList[i].transform.SetParent(poolParent);
                tokenPool.Enqueue(tokenList[i]);
            }

            tokenList.Clear();
        }
    }
    public void Btn_CloseReward()
    {
        rewardPanel.SetActive(false);
        pos++;
        state = DialogState.Next;
        NextToken();
    }

    enum DialogToken
    {
        NPC, Player, Narration, Quest, QuestClear, Select, Select_Jump, EndDialog, Story
    }
    enum DialogState
    {
        Start, Proceed, QuestAccept, Next, End
    }
}
public class NPC
{
    public string name;
    public int count;
    public DialogData[] dialogs;

    public NPC(string name)
    {
        JsonData json = JsonMapper.ToObject(Resources.Load<TextAsset>($"Jsons/Scripts/{name}").text);

        this.name = json[0]["npcName"].ToString();

        count = json.Count;

        dialogs = new DialogData[count];

        for (int i = 0; i < count; i++)
        {
            dialogs[i] = new DialogData();
            dialogs[i].name = json[i]["name"].ToString();
            dialogs[i].idx = (int)json[i]["idx"];
            dialogs[i].kind = (int)json[i]["kind"];
            dialogs[i].chapter = (int)json[i]["chapter"];
            dialogs[i].lvl = (int)json[i]["lvl"];

            dialogs[i].reqQuest = (int)json[i]["reqQuest"];
            dialogs[i].linkedQuest = (int)json[i]["linkedQuest"];
        }
    }
}

public class DialogData
{
    ///<summary> 대화 이름 </summary>
    public string name;
    ///<summary> 대화 인덱스 </summary>
    public int idx;
    ///<summary> 퀘스트 받는 대화 표시
    ///<para> 0 : 그냥 대화, 1 : 퀘스트 수락 대화 </para>
    ///</summary>
    public int kind;
    ///<summary> 대화 표시 요구 챕터 </summary>
    public int chapter;
    ///<summary> 대화 표시 요구 레벨 </summary>
    public int lvl;
    ///<summary> 대화 표시 요구 퀘스트 </summary>
    public int reqQuest;
    ///<summary> 대화 시 수락하는 퀘스트 </summary>
    public int linkedQuest;
}
