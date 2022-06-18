using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using LitJson;
using System.Linq;

public class ScriptPanel : MonoBehaviour, ITownPanel
{
    #region Btn
    [SerializeField] GameObject npcSelectPanel;

    int selectedNpc = -1;
    [SerializeField] GameObject dialogSelectPanel;
    [SerializeField] DialogButton[] dialogSelectBtns;

    DialogData currDialog = null;
    [SerializeField] GameObject questSelectBtns;
    #endregion Btn

    [SerializeField] DialogState state = DialogState.Start;

    static string[] npcName = null;
    static NPC[] npcs;
    List<KeyValuePair<DialogData, QuestState>> dialogList = new List<KeyValuePair<DialogData, QuestState>>();

    #region Dialog
    [SerializeField] Text dialogTxt;
    JsonData dialogJson;
    int pos;
    Coroutine proceedDialog;
    #endregion Dialog

    //최초 상태(npc 선택 창)로 되돌리기, npc 목록 새로고침
    public void ResetAllState()
    {
        if (npcName == null)
            LoadNPCData();

        StopAllCoroutines();

        state = DialogState.Start;
        dialogTxt.text = string.Empty;

        selectedNpc = -1;
        currDialog = null;

        RefreshNPC();
        dialogList.Clear();
        npcSelectPanel.SetActive(true);
        dialogSelectPanel.SetActive(false);
        questSelectBtns.SetActive(false);

        //NPC 목록 새로고침
        void RefreshNPC()
        {

        }
    }
    void LoadNPCData()
    {
        npcName = new string[8];
        npcName[0] = "testNPC";

        npcs = new NPC[8];
        for(int i = 0;i < 1;i++)
            npcs[i] = new NPC(npcName[i]);

    }

    ///<summary> npc 선택 버튼 - 선택한 npc의 대화 목록 불러오기 </summary>
    public void Btn_SelectNPC(int idx)
    {
        selectedNpc = idx;
        npcSelectPanel.SetActive(false);

        LoadDialogList();
        dialogSelectPanel.SetActive(true);

    }
    ///<summary> 선택한 npc 대화 목록 불러오기 - 퀘스트 진행 상황에 따라 불러옴 </summary>
    void LoadDialogList()
    {
        dialogList.Clear();
        List<QuestProceed> proceedingQuestList = QuestManager.GetCurrQuest();
        List<int> clearedQuestList = QuestManager.GetClearedQuest();

        int i = 0;
        for (; i < npcs[selectedNpc].count && dialogList.Count < 4; i++)
        {
            DialogData dialog = npcs[selectedNpc].dialogs[i];

            //현재 3개 이상 퀘스트 수행 중일 시, 새로운 퀘스트 관련 대화 표시 안함
            if (proceedingQuestList.Count >= 3 && dialog.kind == 1 &&
                !proceedingQuestList.Any(x=>x.idx == dialog.linkedQuest))
                continue;

            //관련 퀘스트 수행 중 여부 알아냄
            QuestState qs = QuestState.NotReceive;
            foreach(QuestProceed qp in proceedingQuestList)
                if(qp.idx == dialog.linkedQuest)
                {
                    qs = qp.state;
                    break;
                }

            //1. 퀘스트 관련 대화가 아니거나 퀘스트 3개 이상 수행 중 아님
            //2. 선행 퀘스트 클리어함
            //3. 숨김 퀘스트 클리어 안함
            // 위 조건 모두 만족 시 대화에 추가
            if (IsAdd(i))
                dialogList.Add(new KeyValuePair<DialogData, QuestState>(npcs[selectedNpc].dialogs[i], qs));
        }

        i = 0;
        for (; i < dialogList.Count; i++)
        {
            dialogSelectBtns[i].Set(dialogList[i].Key.name);
            dialogSelectBtns[i].gameObject.SetActive(true);
        }
        for (; i < dialogSelectBtns.Length; i++)
            dialogSelectBtns[i].gameObject.SetActive(false);

        //선행 퀘스트, 관련 퀘스트, 레벨 조건 검사
        bool IsAdd(int idx)
        {
            int reqQuest = npcs[selectedNpc].dialogs[idx].reqQuest;
            int linkedQuest = npcs[selectedNpc].dialogs[idx].linkedQuest;
            //선행 퀘스트 클리어, 관련 퀘스트 클리어 안함, 레벨 넘김
            return (clearedQuestList.Contains(reqQuest) && 
                    (linkedQuest == 0 || !clearedQuestList.Contains(linkedQuest)) &&
                    GameManager.instance.slotData.lvl >= npcs[selectedNpc].dialogs[idx].lvl);
        }
    }

    //npc의 대화 목록 선택 버튼 - 선택한 대화 시작
    public void Btn_SelectDialog(int idx)
    {
        if (state != DialogState.Start)
            return;

        currDialog = dialogList[idx].Key;
        string path = string.Concat("Jsons/Scripts/", npcName[selectedNpc], "/", "dialog", currDialog.idx);
        if (currDialog.kind == 1)
            path = string.Concat(path, dialogList[idx].Value == QuestState.NotReceive ? "R" :
                                        dialogList[idx].Value == QuestState.Proceeding ? "P" : "C");


        //선택한 대화 불러오기
        dialogJson = JsonMapper.ToObject(Resources.Load<TextAsset>(path).text);
        pos = 0;

        dialogSelectPanel.SetActive(false);
        state = DialogState.Next;
        NextToken();
    }
    //NPC 선택창으로 돌아가기
    public void Btn_BackToNPCSelect() => ResetAllState();

    #region Dialog
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
                    state = DialogState.Proceed;
                    proceedDialog = StartCoroutine(ProceedDialog());
                    break;
                case DialogToken.Player:
                    state = DialogState.Proceed;
                    proceedDialog = StartCoroutine(ProceedDialog());
                    break;
                case DialogToken.Narration:
                    state = DialogState.Proceed;
                    proceedDialog = StartCoroutine(ProceedDialog());
                    break;
                //퀘스트 버튼 보이기, QuestAccept state로 전환
                case DialogToken.Quest:
                    questSelectBtns.SetActive(true);
                    state = DialogState.QuestAccept;
                    break;
                //퀘스트 클리어
                case DialogToken.QuestClear:
                    ClearQuest(currDialog.linkedQuest);
                    pos++;
                    NextToken();
                    break;
                //대화 종료
                case DialogToken.EndDialog:
                    state = DialogState.End;
                    EndDialog();
                    break;
                //스토리 보이기, 대화 종료
                case DialogToken.Story:
                    PlayStory(int.Parse(dialogJson[pos]["script"].ToString()));
                    EndDialog();
                    break;
                //에러 - 대화 종료
                default:
                    state = DialogState.End;
                    EndDialog();
                    break;
            }
        }

    }

    #region Normal Dialog
    IEnumerator ProceedDialog()
    {
        float time = 0.05f + 0.05f * SoundManager.instance.GetTxtSpd();
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
    #endregion Normal Dialog

    #region Special Event
    void PlaySFX(int idx) => SoundManager.instance.PlaySFX(idx);

    void PlayStory(int idx)
    {
        Debug.Log(string.Concat("스토리 재생 ", idx));
    }

    void NewQuest(int idx) => QuestManager.AcceptQuest(false, idx);
    void ClearQuest(int idx) => QuestManager.ClearQuest(idx);
    #endregion Special Event
    void EndDialog()
    {
        state = DialogState.Start;
        dialogTxt.text = string.Empty;
        currDialog = null;
        LoadDialogList();

        npcSelectPanel.SetActive(false);
        dialogSelectPanel.SetActive(true);
        questSelectBtns.SetActive(false);
    }
    #endregion Dialog

    enum DialogToken
    {
        NPC, Player, Narration, Quest, QuestClear, EndDialog, Story
    }
    enum DialogState
    {
        Start, Proceed, QuestAccept, Next, End
    }

    class DialogData
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
    class NPC
    {
        public int count;
        public DialogData[] dialogs;

        public NPC(string name)
        {
            JsonData json = JsonMapper.ToObject(Resources.Load<TextAsset>(string.Concat("Jsons/Scripts/", name, "/", name)).text);
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
}
